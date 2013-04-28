using System;
using System.Xml.Linq;

namespace EscenicMigrator
{
	public static class XAttributeExtensions
	{
		public static string SafeGetValue(this XAttribute attribute)
		{
			return (attribute != null) ? attribute.Value : string.Empty;
		}

		public static bool SafeGetBool(this XAttribute attribute)
		{
			bool value;
			bool.TryParse(attribute.SafeGetValue(), out value);
			return value;
		}

		public static DateTime SafeGetDate(this XAttribute attribute)
		{
			DateTime value;
			DateTime.TryParse(attribute.SafeGetValue(), out value);
			return value;
		}

		public static int SafeGetInt(this XAttribute attribute)
		{
			int value;
			int.TryParse(attribute.SafeGetValue(), out value);
			return value;
		}
	}
}