namespace Colinapp.Domain.Entities.System;

/// <summary>
/// 字典类型。一个类型（如 sys_user_sex）下挂多个字典数据项。
/// </summary>
public class DictType : EntityBase
{
    /// <summary>字典名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>字典类型编码（唯一，如 sys_user_sex）</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>状态：true=启用</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>备注</summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 字典数据项。前端下拉/标签的统一来源。
/// </summary>
public class DictData : EntityBase
{
    /// <summary>所属字典类型编码</summary>
    public string DictType { get; set; } = string.Empty;

    /// <summary>显示标签</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>实际值</summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>显示排序</summary>
    public int OrderNum { get; set; }

    /// <summary>标签样式（前端 el-tag type，如 success/info/warning/danger）</summary>
    public string? TagType { get; set; }

    /// <summary>是否默认项</summary>
    public bool IsDefault { get; set; }

    /// <summary>状态：true=启用</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>备注</summary>
    public string? Remark { get; set; }
}
