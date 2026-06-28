using Colinapp.Application.Common;
using Colinapp.Infrastructure.Caching;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace Colinapp.Tests.Infrastructure;

/// <summary>测试用缓存工厂。</summary>
public static class TestCache
{
    /// <summary>真实的进程内内存缓存实现（自跟踪键、支持前缀失效）。</summary>
    public static ICacheService Memory()
    {
        var memoryCache = new MemoryCache(new MemoryCacheOptions());
        var options = Options.Create(new CacheOptions());
        return new MemoryCacheService(memoryCache, options);
    }

    /// <summary>不缓存任何东西的直通实现，用于隔离被测逻辑、排除缓存干扰。</summary>
    public static ICacheService NoOp() => new NoOpCacheService();

    private sealed class NoOpCacheService : ICacheService
    {
        public Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? ttl = null, CancellationToken ct = default)
            => factory();

        public Task RemoveAsync(string key, CancellationToken ct = default) => Task.CompletedTask;
        public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default) => Task.CompletedTask;
    }
}
