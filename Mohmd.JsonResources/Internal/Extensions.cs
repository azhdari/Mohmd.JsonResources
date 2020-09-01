using System;
using System.Collections.Generic;
using System.Linq;

namespace Mohmd.JsonResources.Internal
{
    public static class Extensions
    {
        public static IEnumerable<T> Break<T>(this IEnumerable<T> query, Action breaker)
        {
            breaker();
            return query;
        }

        public static IEnumerable<T> Break<T>(this IEnumerable<T> query, Action<List<T>> breaker)
        {
            var list = query.ToList();
            breaker(list);

            return list;
        }

        public static IEnumerable<T> Break<T>(this IEnumerable<T> query, Func<List<T>, IEnumerable<T>> breaker)
        {
            var list = query.ToList();
            return breaker(list);
        }
    }
}
