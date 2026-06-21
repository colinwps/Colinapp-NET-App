namespace Colinapp.Application.Common;

/// <summary>
/// 当前登录用户上下文。由 Api 层基于 HttpContext 实现，供各层读取当前用户信息，
/// 用于审计字段填充、数据权限过滤、租户隔离等。
/// </summary>
public interface ICurrentUser
{
    /// <summary>当前用户 Id，未登录为 null</summary>
    long? UserId { get; }

    /// <summary>当前用户账号</summary>
    string? UserName { get; }

    /// <summary>当前租户 Id（多租户预留）</summary>
    long? TenantId { get; }

    /// <summary>是否已认证</summary>
    bool IsAuthenticated { get; }

    /// <summary>是否超级管理员</summary>
    bool IsAdmin { get; }
}
