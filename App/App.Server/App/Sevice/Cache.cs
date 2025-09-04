using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

public class Cache(IDistributedCache cache, Configuration configuration, CommandContext context)
{
    private async Task<string> Key(string key)
    {
        var result = key;
        if (configuration.IsCache && configuration.IsCacheShared == false)
        {
            if (context.CacheId == null)
            {
                // First client request
                context.CacheId = Guid.NewGuid().ToString();
                await cache.SetStringAsync($"{nameof(Cache)}/{context.CacheId}", context.CacheId);
            }
            else
            {
                var cacheId = await cache.GetStringAsync($"{nameof(Cache)}/{context.CacheId}");
                if (cacheId != context.CacheId)
                {
                    // Subsequent client request went to different server instance
                    context.CacheId = Guid.NewGuid().ToString();
                    await cache.SetStringAsync($"{nameof(Cache)}/{context.CacheId}", context.CacheId);
                }
            }
            result = $"{nameof(Cache)}/CacheId/{context.CacheId}/{key}";
        }
        return result;
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        if (configuration.IsCache == false)
        {
            return null;
        }
        T? result = null;
        key = await Key(key);
        var json = await cache.GetStringAsync(key);
        if (json != null)
        {
            result = JsonSerializer.Deserialize<T>(json, UtilServer.JsonOptions());
        }
        return result;
    }

    public async Task SetAsync<T>(string key, T? value) where T : class
    {
        if (configuration.IsCache == false)
        {
            return;
        }
        if (value != null)
        {
            UtilServer.Assert(typeof(T) == value.GetType());
            var json = JsonSerializer.Serialize<T>(value, UtilServer.JsonOptions());
            key = await Key(key);
            var options = new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromMinutes(15) }; // Keep alive 15 min from last access.
            await cache.SetStringAsync(key, json, new() {   });
        }
    }

    public async Task RemoveAsync(string key)
    {
        key = await Key(key);
        await cache.RemoveAsync(key);
    }
}
