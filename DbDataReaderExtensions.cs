using System;
using System.Data.Common;

namespace EscenicMigrator
{
	public static class DbDataReaderExtensions
	{
		public static T SafeGetValue<T>(this DbDataReader reader, string columnName)
		{
			return SafeGetValue(reader, columnName, default(T));
		}

		public static T SafeGetValue<T>(this DbDataReader reader, string columnName, T defaultValue)
		{
			if (reader == null)
			{
				return default(T);
			}

			var data = reader[columnName];
			if (data == null || data == DBNull.Value)
			{
				return default(T);
			}

			if (data is T)
			{
				return (T)data;
			}

			return TConverter.ChangeType<T>(data);
		}
	}
}