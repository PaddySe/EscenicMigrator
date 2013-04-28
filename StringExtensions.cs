using System.Text.RegularExpressions;
using System.Web;

namespace EscenicMigrator
{
	public static class StringExtensions
	{
		public static string StripHtml(this string text, int maxLength = 0, bool encodeHtml = true)
		{
			var filtered = text;
			if (!string.IsNullOrEmpty(filtered))
			{
				filtered = Regex.Replace(HttpUtility.HtmlDecode(filtered), @"(?></?\w+)(?>(?:[^>'""]+|'[^']*'|""[^""]*"")*)>", string.Empty).Trim();
				if (maxLength > 0)
				{
					if (encodeHtml)
					{
						if (filtered.Length > maxLength)
						{
							filtered = HttpUtility.HtmlEncode(filtered.Remove(maxLength - 2).TrimEnd()) + "&hellip;";
						}
						else
						{
							filtered = HttpUtility.HtmlEncode(filtered);
						}
					}
					else
					{
						if (filtered.Length > maxLength)
						{
							filtered = filtered.Remove(maxLength - 3).TrimEnd() + "&hellip;";
						}
					}
				}
			}

			return filtered;
		}

	}
}