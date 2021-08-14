using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;

namespace Library.Platform.Caching
{
    public static class DistributedCacheExtensions
    {
        public static T Get<T>(this IDistributedCache cache, string key)
        {
            if (cache == null) throw new ArgumentNullException(nameof(cache));
            if (key == null) throw new ArgumentNullException(nameof(key));

            var bytes = cache.Get(key);
            if (bytes == default || bytes.Length == 0) return default;

            var json = Encoding.UTF8.GetString(bytes);
            return JsonSerializer.Deserialize<T>(json);
        }

        public static async Task<T> GetAsync<T>(this IDistributedCache cache, string key, CancellationToken token = default)
        {
            if (cache == null) throw new ArgumentNullException(nameof(cache));
            if (key == null) throw new ArgumentNullException(nameof(key));

            var bytes = await cache.GetAsync(key, token);
            if (bytes == default || bytes.Length == 0) return default;

            var json = Encoding.UTF8.GetString(bytes);
            return JsonSerializer.Deserialize<T>(json);
        }

        public static void Set<T>(this IDistributedCache cache, string key, T value, DistributedCacheEntryOptions options = default)
        {
            if (cache == null) throw new ArgumentNullException(nameof(cache));
            if (key == null) throw new ArgumentNullException(nameof(key));

            var bytes = new byte[0];
            if ((object) value != default)
            {
                var json = JsonSerializer.Serialize(value);
                bytes = Encoding.UTF8.GetBytes(json);
            }

            cache.Set(key, bytes, options);
        }

        public static Task SetAsync<T>(this IDistributedCache cache, string key, T value, DistributedCacheEntryOptions options = default, CancellationToken token = default)
        {
            if (cache == null) throw new ArgumentNullException(nameof(cache));
            if (key == null) throw new ArgumentNullException(nameof(key));

            var bytes = new byte[0];
            if ((object)value != default)
            {
                var json = JsonSerializer.Serialize(value);
                bytes = Encoding.UTF8.GetBytes(json);
            }

            return cache.SetAsync(key, bytes, options, token);
        }
    }
}
