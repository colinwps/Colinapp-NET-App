using System.Text.Json;
using Colinapp.Application.Common;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace Colinapp.Infrastructure.Caching;

/// <summary>
/// Redis 缓存实现。值以 JSON 存储；前缀失效用 SCAN。
/// 所有操作对 Redis 故障降级：读失败回退工厂、写失败忽略，不影响主流程。
/// </summary>
public class RedisCacheService(IConnectionMultiplexer mux, IOptions<CacheOptions> options) : ICacheService
{
    private readonly CacheOptions _options = options.Value;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private IDatabase Db => mux.GetDatabase();
    private string K(string key) => _options.InstanceName + key;

    public async Task<T?> GetOrSetAsync<T>(
        string key, Func<Task<T?>> factory, TimeSpan? ttl = null, CancellationToken ct = default)
    {
        var redisKey = K(key);
        // 未连接时直接走工厂，避免阻塞到超时（misconfig/宕机时快速降级）
        if (mux.IsConnected)
        {
            try
            {
                var cached = await Db.StringGetAsync(redisKey);
                if (cached.HasValue)
                    return JsonSerializer.Deserialize<T>(cached.ToString(), Json);
            }
            catch { /* Redis 故障：降级到工厂 */ }
        }

        var value = await factory();
        if (value is not null && mux.IsConnected)
        {
            try
            {
                var payload = JsonSerializer.Serialize(value, Json);
                await Db.StringSetAsync(redisKey, payload, ttl ?? TimeSpan.FromSeconds(_options.DefaultTtlSeconds));
            }
            catch { /* 写失败忽略 */ }
        }
        return value;
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        if (!mux.IsConnected) return;
        try { await Db.KeyDeleteAsync(K(key)); }
        catch { /* 忽略 */ }
    }

    public async Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        if (!mux.IsConnected) return;
        var pattern = K(prefix) + "*";
        try
        {
            foreach (var endpoint in mux.GetEndPoints())
            {
                var server = mux.GetServer(endpoint);
                if (!server.IsConnected || server.IsReplica) continue;
                foreach (var key in server.Keys(pattern: pattern, pageSize: 250))
                    await Db.KeyDeleteAsync(key);
            }
        }
        catch { /* 忽略 */ }
    }
}
