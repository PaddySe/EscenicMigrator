using EPiServer;
using EPiServer.Core;
using EPiServer.DataAccess;
using EPiServer.Security;
using EPiServer.Web;
using EPiServer.Web.Hosting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Xml.Linq;

namespace EscenicMigrator
{
	public partial class EscenicImport : Page
	{
		#region Properties

		private static UnifiedDirectory UploadDirectory { get; set; }
		private static string FilePath { get; set; }
		private static PageData ImportContainer { get; set; }

		#endregion

		static EscenicImport()
		{
			UploadDirectory = FilesystemHelper.GetUnifiedDirectory("~/upload/EscenicImport/");

			// Where should all pages be created?
			ImportContainer = DataFactory.Instance.GetPage(new PageReference(226198));
		}

		protected override void OnLoad(EventArgs e)
		{
			FilePath = txtSourceDirectory.Text;

			base.OnLoad(e);
		}

		protected void btnParseFiles_OnClick(object sender, EventArgs e)
		{
			if (string.IsNullOrWhiteSpace(FilePath))
			{
				return;
			}

			var media = ReadMedia().ToList();
			var articles = ReadArticles().ToList();

			FixReferences(articles, media);
			FixRelations(articles);

			litMedia.Text = media.Count().ToString(CultureInfo.InvariantCulture);
			rptMedia.DataSource = media.OrderBy(m => m.Name);
			rptMedia.DataBind();

			litArticles.Text = articles.Count().ToString(CultureInfo.InvariantCulture);
			rptArticles.DataSource = articles.OrderBy(article => article.Fields["TITLE"]);
			rptArticles.DataBind();

			litSections.Text = articles.SelectMany(article => article.Sections).DistinctBy(section => section.Name).Count().ToString(CultureInfo.InvariantCulture);
			rptSections.DataSource = articles.SelectMany(article => article.Sections).DistinctBy(section => section.Name).OrderBy(section => section.Name);
			rptSections.DataBind();

			var pageTypes = articles
				.Select(article => article.Type)
				.Distinct()
				.OrderBy(type => type)
				.Aggregate(new StringBuilder(), (builder, type) => builder.AppendFormat("{0}<br/>", type), builder => builder.ToString());
			litLog.Text = pageTypes + litLog.Text;
		}

		protected void btnImport_OnClick(object sender, EventArgs e)
		{
			if (string.IsNullOrWhiteSpace(FilePath))
			{
				return;
			}

			var media = ReadMedia().ToList();
			var articles = ReadArticles().ToList();

			FixReferences(articles, media);
			FixRelations(articles);

			var articlesToMigrate = new List<Article>();
			articlesToMigrate.AddRange(articles.Where(article => article.Type == "imageGallery"));
			articlesToMigrate.AddRange(articles.Where(article => article.Type == "standard"));
			articlesToMigrate.AddRange(articles.Where(article => article.Type == "articleList"));
			articlesToMigrate.AddRange(articles.Where(article => article.Type == "articleCollection"));
			articlesToMigrate.AddRange(articles.Where(article => article.Type == "multiPage"));
			var partitioner = Partitioner.Create(articlesToMigrate, true);
			partitioner.AsParallel().ForAll(article => article.Migrate());
		}

		protected void OnServerValidate(object source, ServerValidateEventArgs args)
		{
			if (!Directory.Exists(txtSourceDirectory.Text))
			{
				args.IsValid = false;
				valDirectories.ErrorMessage = "Import directory does not exist.";
			}
			else if (!FilesystemHelper.VirtualDirectoryExists(txtUploadPath.Text))
			{
				args.IsValid = false;
				valDirectories.ErrorMessage = "Upload directory does not exist.";
			}
		}

		private void FixReferences(List<Article> articles, List<Media> mediae)
		{
			articles.ForEach(article =>
			{
				foreach (var reference in article.References)
				{
					var media = mediae.FirstOrDefault(m => m.Id == reference.Id && reference.Type == "image");
					if (media == null)
					{
						litLog.Text += string.Format("Article {0} is missing reference {1}.<br/>", article.Id, reference.Id);
						continue;
					}

					reference.Media = media;
				}
			});
		}

		private void FixRelations(List<Article> articles)
		{
			articles.Where(article => article.Relations.Any())
					.ToList()
					.ForEach(article => article.Relations
											   .Where(relation => relation.Type.StartsWith("BODY"))
											   .ToList()
											   .ForEach(relation =>
											   {
												   var relatedArticle = articles.FirstOrDefault(a => a.Id == relation.Id);
												   if (relatedArticle == null)
												   {
													   litLog.Text += string.Format("Article {0} is missing relation {1}.<br/>", article.Id, relation.Id);
												   }
												   else
												   {
													   relation.Article = relatedArticle;
												   }
											   }));
		}

		#region Articles

		private static IEnumerable<Article> ReadArticles()
		{
			var articleFiles = Directory.EnumerateFiles(FilePath, "article*.xml", SearchOption.TopDirectoryOnly);
			var articles = articleFiles.SelectMany(ReadArticle);
			return articles;
		}

		private static IEnumerable<Article> ReadArticle(string articleFilePath)
		{
			var document = XDocument.Load(articleFilePath);
			var articles = (from element in document.Element("io").Elements("article")
							let author = element.Element("author")
							let fields = element.Elements("field")
							let sections = element.Elements("section")
							let references = element.Elements("reference")
							let relations = element.Elements("relation")
							select new Article
							{
								Path = articleFilePath,
								Id = element.Attribute("id").SafeGetInt(),
								Type = element.Attribute("type").SafeGetValue(),
								State = element.Attribute("state").SafeGetValue(),
								PublishDate = element.Attribute("publishdate").SafeGetDate(),
								Fields = fields.ToDictionary(field => field.Attribute("name").SafeGetValue(), field => field.SafeGetValue()),
								Sections = new SectionList(sections.Select(section => new Section
								{
									Id = section.Attribute("id").SafeGetInt(),
									Name = section.Attribute("name").SafeGetValue(),
									HomeSection = section.Attribute("homeSection").SafeGetBool()
								})),
								References = new ReferenceList(references.Select(reference => new Reference
								{
									Id = reference.Attribute("id").SafeGetInt(),
									Type = reference.Attribute("type").SafeGetValue(),
									Priority = reference.Attribute("priority").SafeGetInt(),
									Element = reference.Attribute("element").SafeGetValue(),
									Align = reference.Attribute("align").SafeGetValue()
								})),
								Relations = new RelationList(relations.Select(relation => new Relation
								{
									Id = relation.Attribute("id").SafeGetInt(),
									Priority = relation.Attribute("priority").SafeGetInt(),
									Type = relation.Attribute("type").SafeGetValue()
								})),
								Author = (author == null) ? null : new Author
								{
									Id = author.Attribute("id").SafeGetInt(),
									FirstName = author.Attribute("firstname").SafeGetValue(),
									LastName = author.Attribute("surname").SafeGetValue()
								}
							});
			return articles;
		}

		#endregion

		#region Media

		private static IEnumerable<Media> ReadMedia()
		{
			var files = Directory.EnumerateFiles(FilePath, "image*.xml", SearchOption.TopDirectoryOnly);
			return files.SelectMany(ImportMediaFile);
		}

		private static IEnumerable<Media> ImportMediaFile(string mediaFilePath)
		{
			var document = XDocument.Load(mediaFilePath);
			var objects = (from element in document.Element("io").Elements("multimediaGroup")
						   let multimedia = element.Element("multimedia")
						   select new Media
						   {
							   AltText = element.Attribute("alttext").SafeGetValue(),
							   Copyright = element.Attribute("copyright").SafeGetValue(),
							   Description = element.Element("description").SafeGetValue(),
							   FileName = (multimedia != null) ? multimedia.Attribute("filename").SafeGetValue() : string.Empty,
							   Id = element.Attribute("id").SafeGetInt(),
							   Name = element.Attribute("name").SafeGetValue(),
							   Type = element.Attribute("type").SafeGetValue()
						   });
			return objects;
		}

		#endregion

		#region Classes

		#region Class: EscenicObjectBase

		public abstract class EscenicObjectBase
		{
			public int Id { get; set; }
		}

		#endregion

		#region Class: Article

		public class Article : EscenicObjectBase
		{
			public string Path { get; set; }
			public Author Author { get; set; }
			public string Type { get; set; }
			public string State { get; set; }
			public DateTime PublishDate { get; set; }
			public Dictionary<string, string> Fields { get; set; }
			public SectionList Sections { get; set; }
			public ReferenceList References { get; set; }
			public RelationList Relations { get; set; }

			private string GetFieldValue(string key)
			{
				return Fields.ContainsKey(key) ? Fields[key] : null;
			}

			public PageData Migrate()
			{
				PageData newPage;

				switch (Type.ToLower())
				{
					case "standard":
						newPage = MigrateStandard();
						break;

					case "imagegallery":
						newPage = MigrateImageGallery();
						break;

					default:
						goto case "standard";
				}

				if (newPage != null)
				{
					var pageName = GetFieldValue("TITLE");
					if (!string.IsNullOrWhiteSpace(GetFieldValue("SUBTITLE")))
					{
						pageName = GetFieldValue("SUBTITLE") + " " + pageName;
					}

					newPage.PageName = pageName;
					newPage.URLSegment = UrlSegment.CreateUrlSegment(newPage);
					newPage["PageExternalURL"] = string.Format("article{0}", Id);
					newPage.StartPublish = PublishDate;
					newPage["PageCreated"] = PublishDate;
					var keywords = GetFieldValue("KEYWORDS");
					newPage["MetaKeywords"] = (!string.IsNullOrWhiteSpace(keywords) && keywords.Length > 255) ? keywords.Substring(0, 254) : keywords;

					var newPageReference = DataFactory.Instance.Save(newPage, SaveAction.Publish, AccessLevel.NoAccess);
					DataLogger.Add(this, newPageReference);
				}

				return newPage;
			}

			/// <summary>
			/// Migrate properties for the "standard" Escenic page type.
			/// </summary>
			/// <returns></returns>
			private PageData MigrateStandard()
			{
				var newPage = PageTypeResolver.GetDefaultPageData(this);
				// Add content from various Escenic fields to the properties of the EPiServer page.

				newPage["MainBody"] = ConstructHtmlForStandardSection();
				var topImage = References.FirstOrDefault(reference => reference.Element == "TOPWIDE");
				if (topImage != null)
				{
					newPage["TopImage"] = topImage.Media.GetUploadedFile().VirtualPath;
					if (!string.IsNullOrWhiteSpace(topImage.Media.Description))
					{
						var topImageText = topImage.Media.Description;
						if (topImageText.Length > 245)
						{
							topImageText = topImageText.Substring(0, 245);
						}

						if (!string.IsNullOrWhiteSpace(topImage.Media.Copyright))
						{
							var copyright = string.Format(" &copy; {0}", topImage.Media.Copyright);
							var maxLength = 245 - copyright.Length;
							topImageText = topImageText.StripHtml(maxLength, false) + copyright;
						}

						newPage["TopImageText"] = topImageText;
					}
				}

				Relations.Sort();
				var relations = Relations
					.Where(relation => relation.Type.StartsWith("BODY"))
					.Where(relation => relation.Article != null)
					.Take(5)
					.ToList();
				if (relations.Any())
				{
					var counter = 0;
					foreach (var relation in relations.Where(r => r.Article.Fields.ContainsKey("BODY1")))
					{
						counter++;
						newPage["BoxInfoHeader" + counter] = relation.Article.Fields["TITLE"];
						newPage["BoxInfo" + counter] = relation.Article.Fields["BODY1"];
					}
				}

				if (Fields.ContainsKey("BYLINE"))
				{
					newPage["Writer"] = GetFieldValue("BYLINE");
				}
				else if (Author != null)
				{
					newPage["Writer"] = Author.ToString();
				}

				var puffImage = References.FirstOrDefault(reference => reference.Element == "FPTITLE");
				if (puffImage != null)
				{
					newPage["ListImage"] = puffImage.Media.GetUploadedFile().VirtualPath;
				}

				return newPage;
			}

			private PageData MigrateImageGallery()
			{
				var newPage = MigrateStandard();

				References
					.Where(reference => reference.Element == "LEADTEXT")
					.OrderBy(reference => reference.Priority)
					.Select((reference, index) => new { Reference = reference, Index = index })
					.ToList()
					.ForEach(item => item.Reference.Media.GetUploadedPageFolderFile(newPage, string.Format("{0} ", item.Index)));

				newPage["MainBody"] = string.Format("[ADD DYNAMIC CONTENT CONTROL HERE]{0}.", newPage["MainBody"]);

				return newPage;
			}

			/// <summary>
			/// Constructs the HTML for standard section.
			/// </summary>
			/// <returns></returns>
			private string ConstructHtmlForStandardSection()
			{
				/*
				 * Format for article:
				 * BODY1-text (BODY1-image)
				 * [FULLWIDTH1-image]
				 * <br/>
				 * Repeat.
				 */

				var builder = new StringBuilder();

				for (var i = 0; i < 10; i++)
				{
					builder.Append(FormatPart("BODY" + i, "BODY" + i, "FULLWIDTH" + i));
				}

				return builder.ToString();
			}

			private string FormatPart(string bodyContentName, string bodyImageName, string bodyImageFullwidthName)
			{
				var builder = new StringBuilder();

				var smallImage = References.FirstOrDefault(reference => reference.Element == bodyImageName);
				if (smallImage != null)
				{
					var smallUploadedImage = smallImage.Media.GetUploadedFile();
					builder.Append("<p style=\"float: right; margin-left: 2px; margin-right: 2px; width: 200px;\">");
					builder.AppendFormat("<img src=\"{0}\" alt=\"{1}\" style=\"width: 200px;\" />", smallUploadedImage.VirtualPath, smallUploadedImage.Summary.Subject);
					if (!string.IsNullOrWhiteSpace(smallUploadedImage.Summary.Comments))
					{
						builder.Append("<br/>");
						builder.AppendFormat("<span class=\"imagetext\">{0}", smallUploadedImage.Summary.Comments);
						if (!string.IsNullOrWhiteSpace(smallUploadedImage.Summary.Author))
						{
							builder.AppendFormat(" &copy; {0}", smallUploadedImage.Summary.Author);
						}

						builder.Append("</span>");
					}

					builder.AppendLine("</p>");
				}

				if (Fields.Any(field => field.Key == bodyContentName))
				{
					builder.AppendLine(Fields[bodyContentName]);
				}

				var fullwidthImage = References.FirstOrDefault(reference => reference.Element == bodyImageFullwidthName);
				if (fullwidthImage != null)
				{
					if (builder.Length > 0)
					{
						builder.AppendLine("<p>&nbsp;</p>");
					}

					var bigUploadedImage = fullwidthImage.Media.GetUploadedFile();
					builder.Append("<p>");
					builder.AppendFormat("<img src=\"{0}\" alt=\"{1}\" style=\"width: 468px;\" />", bigUploadedImage.VirtualPath, bigUploadedImage.Summary.Subject);
					if (!string.IsNullOrWhiteSpace(bigUploadedImage.Summary.Comments))
					{
						builder.Append("<br/>");
						builder.AppendFormat("<span class=\"imagetext\">{0}", bigUploadedImage.Summary.Comments);
						if (!string.IsNullOrWhiteSpace(bigUploadedImage.Summary.Author))
						{
							builder.AppendFormat(" &copy; {0}", bigUploadedImage.Summary.Author);
						}

						builder.Append("</span>");
					}

					builder.AppendLine("</p>");
				}

				return builder.ToString();
			}
		}

		#endregion

		#region Class: Author

		public class Author : EscenicObjectBase
		{
			public string FirstName { get; set; }
			public string LastName { get; set; }

			public override string ToString()
			{
				return string.Format("{0} {1}", FirstName, LastName);
			}
		}

		#endregion

		#region Class: Media

		public class Media : EscenicObjectBase
		{
			public string AltText { get; set; }
			public string Copyright { get; set; }
			public string Description { get; set; }
			public string FileName { get; set; }
			public string Name { get; set; }
			public string Type { get; set; }

			private UnifiedFile _uploadedFile;

			public UnifiedFile GetUploadedPageFolderFile(PageData page, string prefix = "")
			{
				var pageDirectory = page.GetPageDirectory(true);
				return GetFile(pageDirectory, prefix);
			}

			public UnifiedFile GetUploadedFile()
			{
				return _uploadedFile ?? (_uploadedFile = GetFile(UploadDirectory, string.Empty));
			}

			private UnifiedFile GetFile(UnifiedDirectory directory, string prefix)
			{
				var fileToImport = Path.Combine(FilePath, FileName);
				var fileData = File.ReadAllBytes(fileToImport);

				var filename = FilesystemHelper.UniqueifyFilename(prefix + FileName, directory);
				var uploadedFile = FilesystemHelper.SaveUploadedData(fileData, directory, filename);
				if (uploadedFile != null)
				{
					uploadedFile.Summary.Author = Copyright;
					uploadedFile.Summary.Comments = Description;
					uploadedFile.Summary.Title = Name;
					uploadedFile.Summary.Subject = AltText;
					uploadedFile.Summary.SaveChanges();
				}

				return uploadedFile;
			}

			public bool FileExists()
			{
				var filePath = Path.Combine(FilePath, FileName);
				return File.Exists(filePath);
			}
		}


		#endregion

		#region Class: Reference

		public class Reference : EscenicObjectBase
		{
			public string Type { get; set; }
			public int Priority { get; set; }
			public string Element { get; set; }
			public string Align { get; set; }
			public Media Media { get; set; }
		}

		#endregion

		#region Class: ReferenceList

		public class ReferenceList : List<Reference>
		{
			public ReferenceList(IEnumerable<Reference> references)
				: base(references)
			{ }

			public string Files
			{
				get
				{
					return this
						.Where(reference => reference.Media != null)
						.OrderBy(reference => reference.Priority)
						.Aggregate(new StringBuilder(), (builder, reference) => builder.AppendFormat("{0} {1}", reference.Media.FileName, reference.Media.FileExists() ? string.Empty : "<span style=\"color: #f00;\">[MISSING]</span>").Append("<br/>"), builder => builder.ToString());
				}
			}

			public override string ToString()
			{
				return this
					.OrderBy(reference => reference.Priority)
					.Aggregate(new StringBuilder(), (builder, reference) => builder.AppendFormat("{0}<br/>", reference.Id), builder => builder.ToString());
			}
		}

		#endregion

		#region Class: Relation

		public class Relation : EscenicObjectBase, IComparable<Relation>
		{
			public int Priority { get; set; }
			public string Type { get; set; }
			public Article Article { get; set; }

			public int CompareTo(Relation other)
			{
				if (Type.Contains("BODY") && other.Type.Contains("BODY"))
				{
					var thisNumberString = Type.Replace("BODY", "");
					var otherNumberString = other.Type.Replace("BODY", "");
					int thisNumber, otherNumber;
					if (int.TryParse(thisNumberString, out thisNumber) && int.TryParse(otherNumberString, out otherNumber))
					{
						return (thisNumber == otherNumber) ? Priority.CompareTo(other.Priority) : thisNumber.CompareTo(otherNumber);
					}
				}

				return string.Compare(Type, other.Type, StringComparison.Ordinal);
			}
		}

		#endregion

		#region Class: RelationList

		public class RelationList : List<Relation>
		{
			public RelationList(IEnumerable<Relation> relations)
				: base(relations)
			{ }

			public override string ToString()
			{
				Sort();
				return this
					.Where(relation => !relation.Type.Contains("related") && relation.Article != null)
					.Aggregate(new StringBuilder(), (builder, relation) => builder.AppendFormat("{0}&nbsp;({1})<br/>", relation.Id, relation.Type), builder => builder.ToString());
			}
		}

		#endregion

		#region Class: Section

		public class Section : EscenicObjectBase
		{
			public string Name { get; set; }
			public bool HomeSection { get; set; }
		}

		#endregion

		#region Class: SectionList

		public class SectionList : List<Section>
		{
			public SectionList(IEnumerable<Section> sections)
				: base(sections)
			{ }

			public override string ToString()
			{
				return this.Aggregate(new StringBuilder(), (builder, section) => builder.Append(section.Name).Append("<br/>"), builder => builder.ToString());
			}
		}

		#endregion

		#endregion

		#region Page Type Resolver

		public static class PageTypeResolver
		{
			private const int ParentPageTypeId = 102;

			// Maps an Escenic category by name to an EPiServer PageType ID.
			private static readonly Dictionary<string, int> ArticleTypeMappings = new Dictionary<string, int>
			{
				{ "standard", 103 },
				{ "imageGallery", 103 },
				{ "default", 103 }
			};

			public static PageData GetDefaultPageData(Article article)
			{
				var pageTypeId = ArticleTypeMappings.Where(m => m.Key == article.Type).Select(m => m.Value).FirstOrDefault();
				if (pageTypeId == default(int))
				{
					pageTypeId = ArticleTypeMappings.Where(m => m.Key == "default").Select(m => m.Value).FirstOrDefault();
					if (pageTypeId == default(int))
					{
						throw new Exception(string.Format("Page type for {0} not defined, and no default page type found.", article.Type));
					}
				}

				var parentPage = DataFactory.Instance.GetChildren(ImportContainer.PageLink).FirstOrDefault(child => child.PageName == article.Type);
				if (parentPage == null)
				{
					parentPage = DataFactory.Instance.GetDefaultPageData(ImportContainer.PageLink, ParentPageTypeId);
					parentPage.PageName = article.Type;
					DataFactory.Instance.Save(parentPage, SaveAction.Publish, AccessLevel.NoAccess);
				}

				var page = DataFactory.Instance.GetDefaultPageData(parentPage.PageLink, pageTypeId);

				// Set default data?
				page.PageName = article.Fields["TITLE"];
				page.Property["PageCreated"].Value = article.PublishDate;
				page.StartPublish = article.PublishDate;

				return page;
			}
		}

		#endregion

		#region Data Logging

		public static class DataLogger
		{
			public static void Add(Article article, PageReference pageRef)
			{
				using (var connection = new SqlConnection(""))
				{
					// CREATE TABLE EscenicImport (ArticleId INT NOT NULL, PageId INT NOT NULL);
					using (var command = new SqlCommand("INSERT INTO EscenicImport (ArticleId, PageId) VALUES (@articleId, @pageId)", connection))
					{
						command.Parameters.Add("@articleId", SqlDbType.Int).Value = article.Id;
						command.Parameters.Add("@pageId", SqlDbType.Int).Value = pageRef.ID;
						connection.Open();
						command.ExecuteNonQuery();
					}
				}
			}
		}

		#endregion
	}
}
