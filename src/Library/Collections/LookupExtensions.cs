using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Collections
{
    public static class LookupExtensions
    {
        public static bool TryGet<TKey, TSource>(this ILookup<TKey, TSource> lookup, TKey key, out IEnumerable<TSource> sources)
        {
            if (lookup == null) throw new ArgumentNullException(nameof(lookup));

            sources = lookup[key];
            return sources != null && sources.Any();
        }

        public static bool TryGetFirst<TKey, TSource>(this ILookup<TKey, TSource> lookup, TKey key, out TSource source) where TSource : class
        {
            if (lookup == null) throw new ArgumentNullException(nameof(lookup));

            source = lookup[key].FirstOrDefault();
            return source != null;
        }
    }
}
