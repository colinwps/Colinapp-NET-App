namespace Colinapp.Application.Permissions;

/// <summary>
/// 权限查询服务：根据用户聚合其角色拥有的功能权限标识，供鉴权与前端菜单使用。
/// </summary>
public interface IPermissionService
{
    /// <summary>获取用户拥有的全部权限标识（来自启用角色绑定的启用菜单/按钮）。</summary>
    Task<IReadOnlySet<string>> GetUserPermissionsAsync(long userId, CancellationToken ct = default);

    /// <summary>获取用户拥有的角色 Id 列表（仅启用角色）。</summary>
    Task<IReadOnlyList<long>> GetUserRoleIdsAsync(long userId, CancellationToken ct = default);
}
