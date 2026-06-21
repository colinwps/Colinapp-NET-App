using Colinapp.Domain.Enums;

namespace Colinapp.Domain.Entities.System;

/// <summary>
/// 菜单 / 权限点。树形结构，类型为目录/菜单/按钮。
/// 目录与菜单驱动前端动态路由，按钮提供细粒度权限标识（Permission）。
/// </summary>
public class Menu : EntityBase
{
    /// <summary>菜单名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>父菜单 Id，0 表示根</summary>
    public long ParentId { get; set; }

    /// <summary>显示排序</summary>
    public int OrderNum { get; set; }

    /// <summary>菜单类型：目录/菜单/按钮</summary>
    public MenuType MenuType { get; set; } = MenuType.Menu;

    /// <summary>前端路由路径</summary>
    public string? Path { get; set; }

    /// <summary>前端组件路径（相对 views，动态加载用）</summary>
    public string? Component { get; set; }

    /// <summary>权限标识（如 sys:user:list），按钮/接口鉴权依据</summary>
    public string? Permission { get; set; }

    /// <summary>图标</summary>
    public string? Icon { get; set; }

    /// <summary>是否外链</summary>
    public bool IsExternal { get; set; }

    /// <summary>是否缓存（前端 keep-alive）</summary>
    public bool Cache { get; set; }

    /// <summary>是否显示（隐藏菜单不在侧边栏展示，但路由仍存在）</summary>
    public bool Visible { get; set; } = true;

    /// <summary>状态：true=启用，false=停用（停用后其权限标识失效）</summary>
    public bool Enabled { get; set; } = true;
}
