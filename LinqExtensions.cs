﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace EscenicMigrator
{
	public static class LinqExtensions
	{
		#region DistinctBy

		/* Taken from http://code.google.com/p/morelinq/ */

		/// <summary>
		/// Returns all distinct elements of the given source, where "distinctness"
		/// is determined via a projection and the default eqaulity comparer for the projected type.
		/// </summary>
		/// <remarks>
		/// This operator uses deferred execution and streams the results, although
		/// a set of already-seen keys is retained. If a key is seen multiple times,
		/// only the first element with that key is returned.
		/// </remarks>
		/// <typeparam name="TSource">Type of the source sequence</typeparam>
		/// <typeparam name="TKey">Type of the projected element</typeparam>
		/// <param name="source">Source sequence</param>
		/// <param name="keySelector">Projection for determining "distinctness"</param>
		/// <returns>A sequence consisting of distinct elements from the source sequence,
		/// comparing them by the specified key projection.</returns>

		public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
		{
			return source.DistinctBy(keySelector, null);
		}

		/// <summary>
		/// Returns all distinct elements of the given source, where "distinctness"
		/// is determined via a projection and the specified comparer for the projected type.
		/// </summary>
		/// <remarks>
		/// This operator uses deferred execution and streams the results, although
		/// a set of already-seen keys is retained. If a key is seen multiple times,
		/// only the first element with that key is returned.
		/// </remarks>
		/// <typeparam name="TSource">Type of the source sequence</typeparam>
		/// <typeparam name="TKey">Type of the projected element</typeparam>
		/// <param name="source">Source sequence</param>
		/// <param name="keySelector">Projection for determining "distinctness"</param>
		/// <param name="comparer">The equality comparer to use to determine whether or not keys are equal.
		/// If null, the default equality comparer for <c>TSource</c> is used.</param>
		/// <returns>A sequence consisting of distinct elements from the source sequence,
		/// comparing them by the specified key projection.</returns>
		public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
		{
			source.ThrowIfNull("source");
			keySelector.ThrowIfNull("keySelector");
			return DistinctByImpl(source, keySelector, comparer);
		}

		private static IEnumerable<TSource> DistinctByImpl<TSource, TKey>(IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
		{
			var knownKeys = new HashSet<TKey>(comparer);
			return source.Where(element => knownKeys.Add(keySelector(element)));
		}

		#endregion
	}
}