using System;
using System.ComponentModel;

namespace EscenicMigrator
{
	public static class TConverter
	{
		public static T ChangeType<T>(object value)
		{
			return (T)ChangeType(typeof(T), value);
		}

		public static object ChangeType(Type type, object value)
		{
			var tc = TypeDescriptor.GetConverter(type);
			return tc.ConvertFrom(value);
		}

		public static void RegisterConverter<T, TC>() where TC : TypeConverter
		{
			TypeDescriptor.AddAttributes(typeof(T), new TypeConverterAttribute(typeof(TC)));
		}
	}
}