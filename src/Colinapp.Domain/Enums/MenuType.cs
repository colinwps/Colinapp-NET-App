namespace Colinapp.Domain.Enums;

/// <summary>
/// 菜单类型。目录/菜单构成前端路由树，按钮为细粒度功能权限点（绑定权限标识）。
/// </summary>
public enum MenuType
{
    /// <summary>目录（仅作为分组，对应前端路由父节点）</summary>
    Catalog = 1,

    /// <summary>菜单（对应一个前端页面/路由）</summary>
    Menu = 2,

    /// <summary>按钮（细粒度权限点，不产生路由，仅用于权限标识）</summary>
    Button = 3,
}
