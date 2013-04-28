using System.Xml.Linq;

namespace EscenicMigrator
{
	public static class XElementExtensions
	{
		public static string SafeGetValue(this XElement element)
		{
			return (element != null) ? element.Value : string.Empty;
		}
	}
}