using Colinapp.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace Colinapp.Application.Permissions;

/// <summary>
/// 权限查询实现。按 用户 → 角色 → 菜单 链路聚合权限标识。
/// 作用域内缓存结果，避免单次请求重复查询。
/// </summary>
public class PermissionService(IAppDbContext db) : IPermissionService
{
    private IReadOnlySet<string>? _cachedPermissions;
    private long? _cachedForUserId;

    public async Task<IReadOnlySet<string>> GetUserPermissionsAsync(long userId, CancellationToken ct = default)
    {
        if (_cachedPermissions is not null && _cachedForUserId == userId)
            return _cachedPermissions;

        var roleIds = await GetUserRoleIdsAsync(userId, ct);

        var menuIds = await db.RoleMenus
            .Where(rm => roleIds.Contains(rm.RoleId))
            .Select(rm => rm.MenuId)
            .Distinct()
            .ToListAsync(ct);

        var permissions = await db.Menus
            .Where(m => menuIds.Contains(m.Id) && m.Enabled && m.Permission != null && m.Permission != "")
            .Select(m => m.Permission!)
            .Distinct()
            .ToListAsync(ct);

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
