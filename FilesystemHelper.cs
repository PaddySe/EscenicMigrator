using System.IO;
using System.Web;
using System.Web.Hosting;
using EPiServer.Web;
using EPiServer.Web.Hosting;

namespace EscenicMigrator
{
	public static class FilesystemHelper
	{
		public static UnifiedDirectory GetUnifiedDirectory(string virtualPath)
		{
			if (!VirtualDirectoryExists(virtualPath))
			{
				UnifiedDirectory.CreateDirectory(virtualPath);
			}

			var dir = HostingEnvironment.VirtualPathProvider.GetDirectory(virtualPath) as UnifiedDirectory;
			return dir;
		}

		public static UnifiedFile SaveUploadedData(byte[] data, string virtualPath, string filename)
		{
			return SaveUploadedData(data, GetUnifiedDirectory(virtualPath), filename);
		}

		public static UnifiedFile SaveUploadedData(byte[] data, UnifiedDirectory directory, string filename)
		{
			UnifiedFile file = null;
			if (data != null && data.GetLength(0) > 0)
			{
				file = directory.CreateFile(filename);
				using (var output = file.Open(FileMode.Create, FileAccess.Write))
				{
					output.Write(data, 0, data.GetLength(0));
				}
			}

			return file;
		}

		public static UnifiedFile SaveUploadedFile(HttpPostedFile postedFile, string virtualPath, string fileName)
		{
			return SaveUploadedFile(postedFile, GetUnifiedDirectory(virtualPath), fileName);
		}

		public static UnifiedFile SaveUploadedFile(HttpPostedFile postedFile, UnifiedDirectory directory, string fileName)
		{
			var file = directory.CreateFile(fileName);
			var input = postedFile.InputStream;
			using (var output = file.Open(FileMode.Create, FileAccess.Write))
			{
				input.CopyTo(output);
			}

			return file;
		}

		public static string UniqueifyFilename(string filename, string virtualPath)
		{
			var newFilename = filename;
			var path = VirtualPathUtilityEx.Combine(virtualPath, newFilename);
			if (HostingEnvironment.VirtualPathProvider.FileExists(path))
			{
				var counter = 0;
				do
				{
					newFilename = string.Format("{0}{1}{2}", Path.GetFileNameWithoutExtension(filename), ++counter, Path.GetExtension(filename));
					path = VirtualPathUtilityEx.Combine(virtualPath, newFilename);
				} while (HostingEnvironment.VirtualPathProvider.FileExists(path));
			}

			return newFilename;
		}

		public static string UniqueifyFilename(string filename, UnifiedDirectory directory)
		{
			return UniqueifyFilename(filename, directory.VirtualPath);
		}

		public static bool VirtualDirectoryExists(string virtualPath)
		{
			return HostingEnvironment.VirtualPathProvider.DirectoryExists(virtualPath);
		}
	}
}