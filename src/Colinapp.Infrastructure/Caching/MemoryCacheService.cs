using System.Collections.Concurrent;
using Colinapp.Application.Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Colinapp.Infrastructure.Caching;

/// <summary>
/// 进程内内存缓存实现（默认）。自行跟踪键以支持按前缀批量失效。
/// 单实例部署足够；多实例需切换 Redis 以共享缓存与失效。
/// </summary>
public class MemoryCacheService(IMemoryCache cache, IOptions<CacheOptions> options) : ICacheService
{
    private readonly CacheOptions _options = options.Value;
    private readonly ConcurrentDictionary<string, byte> _keys = new();

    public async Task<T?> GetOrSetAsync<T>(
        string key, Func<Task<T?>> factory, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        if (cache.TryGetValue(key, out T? cached))
            return cached;

        var value = await factory();
        if (value is not null)
        {
            cache.Set(key, value, ttl ?? TimeSpan.FromSeconds(_options.DefaultTtlSeconds));
            _keys.TryAdd(key, 0);
        }
        return value;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        cache.Remove(key);
        _keys.TryRemove(key, out _);
        return Task.CompletedTask;
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        foreach (var key in _keys.Keys.Where(k => k.StartsWith(prefix, StringComparison.Ordinal)).ToList())
        {
            cache.Remove(key);
            _keys.TryRemove(key, out _);
        }
        return Task.CompletedTask;
    }
}
