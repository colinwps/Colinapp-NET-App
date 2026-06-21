namespace Colinapp.Domain.Entities.System;

/// <summary>
/// 部门（组织机构）。树形结构，通过 ParentId 链接，Ancestors 缓存祖级路径便于按「本部门及下级」查询。
/// </summary>
public class Department : EntityBase
{
    /// <summary>部门名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>父部门 Id，0 表示根</summary>
    public long ParentId { get; set; }

    /// <summary>祖级路径，逗号分隔（如 "0,1,3"），用于子树查询。</summary>
    public string Ancestors { get; set; } = "0";

    /// <summary>显示排序</summary>
    public int OrderNum { get; set; }

    /// <summary>负责人用户 Id</summary>
    public long? LeaderUserId { get; set; }

    /// <summary>联系电话</summary>
    public string? Phone { get; set; }

    /// <summary>邮箱</summary>
    public string? Email { get; set; }

    /// <summary>状态：true=启用，false=停用</summary>
    public bool Enabled { get; set; } = true;
}
