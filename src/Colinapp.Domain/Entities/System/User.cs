namespace Colinapp.Domain.Entities.System;

/// <summary>
/// 系统用户（人员）。阶段一仅用于登录认证，后续阶段扩展部门、职位、角色等关联。
/// </summary>
public class User : EntityBase
{
    /// <summary>登录账号（唯一）</summary>
    public string UserName { get; set; } = string.Empty;

    /// <summary>密码哈希（BCrypt）</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>姓名 / 昵称</summary>
    public string NickName { get; set; } = string.Empty;

    /// <summary>手机号</summary>
    public string? Phone { get; set; }

    /// <summary>邮箱</summary>
    public string? Email { get; set; }

    /// <summary>所属部门 Id（阶段二启用关联）</summary>
    public long? DeptId { get; set; }

    /// <summary>状态：true=启用，false=停用</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>是否超级管理员（拥有全部权限，跳过权限校验）</summary>
    public bool IsAdmin { get; set; }

    /// <summary>最后登录时间</summary>
    public DateTime? LastLoginTime { get; set; }
}
