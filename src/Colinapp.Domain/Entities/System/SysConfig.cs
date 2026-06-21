namespace Colinapp.Domain.Entities.System;

/// <summary>
/// 系统参数配置。Key-Value 形式，支持缓存与按键读取。
/// </summary>
public class SysConfig : EntityBase
{
    /// <summary>参数名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>参数键（唯一）</summary>
    public string ConfigKey { get; set; } = string.Empty;

    /// <summary>参数值</summary>
    public string ConfigValue { get; set; } = string.Empty;

    /// <summary>是否系统内置（内置参数不允许删除）</summary>
    public bool IsSystem { get; set; }

    /// <summary>备注</summary>
    public string? Remark { get; set; }
}
