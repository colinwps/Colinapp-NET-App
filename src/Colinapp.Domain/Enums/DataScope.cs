namespace Colinapp.Domain.Enums;

/// <summary>
/// 数据权限范围。决定角色可访问的数据行范围，由数据范围过滤器按当前用户的部门动态注入。
/// 一个用户拥有多个角色时取并集（最宽松）。
/// </summary>
public enum DataScope
{
    /// <summary>全部数据</summary>
    All = 1,

    /// <summary>自定义部门（由角色关联的部门集合 RoleDept 决定）</summary>
    Custom = 2,

    /// <summary>本部门数据</summary>
    Dept = 3,

    /// <summary>本部门及下级数据</summary>
    DeptAndChild = 4,

    /// <summary>仅本人数据</summary>
    Self = 5,
}
