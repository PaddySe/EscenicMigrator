using System;

namespace EscenicMigrator
{
	/// <summary>
	/// Helper methods to make it easier to throw exceptions.
	/// </summary>
	public static class ThrowHelper
	{
		/* Taken from http://code.google.com/p/morelinq/ */

		/// <summary>
		/// Throws if null.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="argument">The argument.</param>
		/// <param name="name">The name.</param>
		public static void ThrowIfNull<T>(this T argument, string name) where T : class
		{
			if (argument == null)
			{
				throw new ArgumentNullException(name);
			}
		}

		/// <summary>
		/// Throws if negative.
		/// </summary>
		/// <param name="argument">The argument.</param>
		/// <param name="name">The name.</param>
		public static void ThrowIfNegative(this int argument, string name)
		{
			if (argument < 0)
			{
				throw new ArgumentOutOfRangeException(name);
			}
		}

		/// <summary>
		/// Throws if non positive.
		/// </summary>
		/// <param name="argument">The argument.</param>
		/// <param name="name">The name.</param>
		public static void ThrowIfNonPositive(this int argument, string name)
		{
			if (argument <= 0)
			{
				throw new ArgumentOutOfRangeException(name);
			}
		}
	}
}