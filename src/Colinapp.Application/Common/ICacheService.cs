namespace Colinapp.Application.Common;

/// <summary>
/// 统一缓存抽象。按配置在内存缓存与 Redis 间切换（见 Cache:Provider）。
/// 实现需对缓存故障降级（读写失败时回退到工厂/直接放行），不影响主流程。
/// </summary>
public interface ICacheService
{
    /// <summary>读缓存；未命中则执行工厂、写入并返回。工厂返回 null 时不写缓存。</summary>
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? ttl = null, CancellationToken ct = default);

    /// <summary>移除指定键。</summary>
    Task RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>移除所有以指定前缀开头的键（用于批量失效，如某类权限/字典）。</summary>
    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
}

/// <summary>缓存键约定。前缀用于批量失效。</summary>
public static class CacheKeys
{
    public const string PermissionPrefix = "perm:";
    public const string DictPrefix = "dict:";
    public const string ConfigPrefix = "config:";

    /// <summary>某用户聚合权限标识集合。</summary>
    public static string UserPermissions(long userId) => $"{PermissionPrefix}user:{userId}";

    /// <summary>某字典类型的启用数据项。</summary>
    public static string DictData(string type) => $"{DictPrefix}data:{type}";

    /// <summary>某参数键的值。</summary>
    public static string Config(string key) => $"{ConfigPrefix}{key}";
}
