using Colinapp.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Colinapp.Application.Permissions;

/// <summary>
/// 权限查询实现。按 用户 → 角色 → 菜单 链路聚合权限标识。
/// 两级缓存：请求内字段缓存 + 跨请求 ICacheService（内存/Redis）。
/// 失效：用户角色变更（按用户键）、角色/菜单变更（按 perm: 前缀）由各服务触发。
/// </summary>
public class PermissionService(IAppDbContext db, ICacheService cache) : IPermissionService
{
    private IReadOnlySet<string>? _cachedPermissions;
    private long? _cachedForUserId;

    public async Task<IReadOnlySet<string>> GetUserPermissionsAsync(long userId, CancellationToken ct = default)
    {
        if (_cachedPermissions is not null && _cachedForUserId == userId)
            return _cachedPermissions;

        var permissions = await cache.GetOrSetAsync(CacheKeys.UserPermissions(userId), async () =>
        {
            var roleIds = await GetUserRoleIdsAsync(userId, ct);

            var menuIds = await db.RoleMenus
                .Where(rm => roleIds.Contains(rm.RoleId))
                .Select(rm => rm.MenuId)
                .Distinct()
                .ToListAsync(ct);

            return await db.Menus
                .Where(m => menuIds.Contains(m.Id) && m.Enabled && m.Permission != null && m.Permission != "")
                .Select(m => m.Permission!)
                .Distinct()
                .ToListAsync(ct);
        }, ct: ct) ?? [];

        _cachedForUserId = userId;
        _cachedPermissions = permissions.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return _cachedPermissions;
    }

    public async Task<IReadOnlyList<long>> GetUserRoleIdsAsync(long userId, CancellationToken ct = default)
    {
        return await (
            from ur in db.UserRoles
            join r in db.Roles on ur.RoleId equals r.Id
            where ur.UserId == userId && r.Enabled
            select r.Id).Distinct().ToListAsync(ct);
    }
}
