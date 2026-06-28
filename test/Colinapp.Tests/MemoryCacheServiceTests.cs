using Colinapp.Application.Common;
using Colinapp.Tests.Infrastructure;
using Xunit;

namespace Colinapp.Tests;

/// <summary>
/// 内存缓存实现的核心契约：命中复用、null 不缓存、按键失效、按前缀批量失效。
/// </summary>
public class MemoryCacheServiceTests
{
    [Fact]
    public async Task GetOrSet_caches_value_factory_runs_once()
    {
        var cache = TestCache.Memory();
        var calls = 0;

        Task<string?> Factory() { calls++; return Task.FromResult<string?>("v"); }

        var first = await cache.GetOrSetAsync("k", Factory);
        var second = await cache.GetOrSetAsync("k", Factory);

        Assert.Equal("v", first);
        Assert.Equal("v", second);
        Assert.Equal(1, calls); // 第二次命中缓存，工厂只执行一次
    }

    [Fact]
    public async Task GetOrSet_does_not_cache_null()
    {
        var cache = TestCache.Memory();
        var calls = 0;

        Task<string?> Factory() { calls++; return Task.FromResult<string?>(null); }

        await cache.GetOrSetAsync("k", Factory);
        await cache.GetOrSetAsync("k", Factory);

        Assert.Equal(2, calls); // null 不写缓存，每次都走工厂
    }

    [Fact]
    public async Task Remove_evicts_single_key()
    {
        var cache = TestCache.Memory();
        var calls = 0;
        Task<string?> Factory() { calls++; return Task.FromResult<string?>("v"); }

        await cache.GetOrSetAsync("k", Factory);
        await cache.RemoveAsync("k");
        await cache.GetOrSetAsync("k", Factory);

        Assert.Equal(2, calls); // 失效后重新计算
    }

    [Fact]
    public async Task RemoveByPrefix_evicts_matching_only()
    {
        var cache = TestCache.Memory();

        await cache.GetOrSetAsync("perm:user:1", () => Task.FromResult<string?>("a"));
        await cache.GetOrSetAsync("perm:user:2", () => Task.FromResult<string?>("b"));
        await cache.GetOrSetAsync("dict:data:sex", () => Task.FromResult<string?>("c"));

        await cache.RemoveByPrefixAsync(CacheKeys.PermissionPrefix);

        var permCalls = 0;
        var dictCalls = 0;
        await cache.GetOrSetAsync("perm:user:1", () => { permCalls++; return Task.FromResult<string?>("a"); });
        await cache.GetOrSetAsync("dict:data:sex", () => { dictCalls++; return Task.FromResult<string?>("c"); });

        Assert.Equal(1, permCalls); // perm: 前缀被清，重算
        Assert.Equal(0, dictCalls); // dict: 未受影响，仍命中
    }
}
