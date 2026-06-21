namespace Colinapp.Domain.Entities.System;

/// <summary>
/// 职位 / 岗位。可与用户多对多关联（UserPost）。
/// </summary>
public class Position : EntityBase
{
    /// <summary>职位名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>职位编码（唯一）</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>显示排序</summary>
    public int OrderNum { get; set; }

    /// <summary>状态：true=启用，false=停用</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>备注</summary>
    public string? Remark { get; set; }
}
