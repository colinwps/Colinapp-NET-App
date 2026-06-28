namespace Colinapp.Infrastructure.Caching;

/// <summary>
/// 缓存配置（绑定 appsettings 的 "Cache" 节）。默认内存缓存，开箱即用，无需 Redis。
/// </summary>
public class CacheOptions
{
    public const string SectionName = "Cache";

    /// <summary>缓存提供方：Memory（默认）| Redis</summary>
    public string Provider { get; set; } = "Memory";

    /// <summary>Redis 连接串（Provider=Redis 时必填），如 localhost:6379</summary>
    public string? RedisConnection { get; set; }

    /// <summary>Redis 键前缀，隔离多实例/多环境。</summary>
    public string InstanceName { get; set; } = "colinapp:";

    /// <summary>默认过期时间（秒）。</summary>
    public int DefaultTtlSeconds { get; set; } = 600;

    public bool UseRedis => string.Equals(Provider, "Redis", StringComparison.OrdinalIgnoreCase);
}
