using Colinapp.Domain.Enums;

namespace Colinapp.Domain.Entities.System;

/// <summary>
/// 角色。功能权限的载体（RoleMenu 绑定菜单/按钮），并携带数据权限范围（DataScope）。
/// </summary>
public class Role : EntityBase
{
    /// <summary>角色名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>角色编码（唯一，如 admin、common）</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>数据权限范围</summary>
    public DataScope DataScope { get; set; } = DataScope.All;

    /// <summary>显示排序</summary>
    public int OrderNum { get; set; }

    /// <summary>状态：true=启用，false=停用</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>备注</summary>
    public string? Remark { get; set; }
}
