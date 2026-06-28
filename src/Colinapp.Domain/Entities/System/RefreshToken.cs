namespace Colinapp.Domain.Entities.System;

/// <summary>
/// 刷新令牌。独立持久化以支持令牌轮换与主动撤销（登出 / 改密）。
/// 不继承 EntityBase：令牌行无需软删除与租户过滤，到期后可物理清理。
/// 所有时间均为 UTC，仅用于内部有效期比较，不对外展示。
/// </summary>
public class RefreshToken
{
    public long Id { get; set; }

    /// <summary>所属用户 Id</summary>
    public long UserId { get; set; }

    /// <summary>不透明令牌串（唯一）</summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>过期时间（UTC）</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>创建时间（UTC）</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>撤销时间（UTC），null 表示未撤销</summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>轮换后替换它的新令牌串，便于追踪令牌链</summary>
    public string? ReplacedByToken { get; set; }

    /// <summary>是否仍可用：未撤销且未过期</summary>
    public bool IsActive(DateTime utcNow) => RevokedAt is null && ExpiresAt > utcNow;
}
