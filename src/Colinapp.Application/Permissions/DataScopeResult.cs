namespace Colinapp.Application.Permissions;

/// <summary>
/// 数据范围解析结果。多角色取并集（最宽松）。
/// 应用方式：All 为 true 时不过滤；否则保留「DeptIds 命中」或「SelfOnly 且属于本人」的行。
/// </summary>
public sealed class DataScopeResult
{
    /// <summary>是否可见全部数据（管理员或含「全部」范围角色）。</summary>
    public bool All { get; init; }

    /// <summary>是否包含「仅本人」范围。</summary>
    public bool SelfOnly { get; init; }

    /// <summary>允许访问的部门 Id 集合（已并入本部门/下级/自定义）。</summary>
    public IReadOnlyCollection<long> DeptIds { get; init; } = [];

    public static DataScopeResult AllData => new() { All = true };
}
