using Colinapp.Domain.Entities.System;
using Colinapp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Colinapp.Infrastructure.Persistence;

/// <summary>
/// 系统菜单种子。<b>幂等</b>：每次启动按「目录/页面/按钮」逐项确保存在（按权限标识或路径判重），
/// 因此新增功能模块时只需在此登记即可在升级后自动补齐菜单与权限点，不会重复插入。
/// </summary>
internal static class MenuSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        // ---- 系统管理 ----
        var system = await EnsureCatalogAsync(db, "系统管理", "/system", "Setting", 1, ct);
        await EnsureModuleAsync(db, system.Id, 1, "用户管理", "user", "system/user/index", "sys:user",
            ["query", "add", "edit", "remove", "resetPwd"], ct);
        await EnsureModuleAsync(db, system.Id, 2, "部门管理", "dept", "system/dept/index", "sys:dept",
            ["query", "add", "edit", "remove"], ct);
        await EnsureModuleAsync(db, system.Id, 3, "职位管理", "position", "system/position/index", "sys:post",
            ["add", "edit", "remove"], ct);
        await EnsureModuleAsync(db, system.Id, 4, "角色管理", "role", "system/role/index", "sys:role",
            ["query", "add", "edit", "remove"], ct);
        await EnsureModuleAsync(db, system.Id, 5, "菜单管理", "menu", "system/menu/index", "sys:menu",
            ["query", "add", "edit", "remove"], ct);
        await EnsureModuleAsync(db, system.Id, 6, "字典管理", "dict", "system/dict/index", "sys:dict",
            ["add", "edit", "remove"], ct);
        await EnsureModuleAsync(db, system.Id, 7, "参数设置", "config", "system/config/index", "sys:config",
            ["add", "edit", "remove"], ct);

        // ---- 系统监控 ----
        var monitor = await EnsureCatalogAsync(db, "系统监控", "/monitor", "Monitor", 2, ct);
        await EnsureModuleAsync(db, monitor.Id, 1, "操作日志", "operlog", "monitor/operlog/index", "sys:log",
            ["remove"], ct);
        await EnsureModuleAsync(db, monitor.Id, 2, "登录日志", "loginlog", "monitor/loginlog/index", "sys:logininfor",
            ["remove"], ct);

        // ---- 业务示例（扩展样例）----
        var business = await EnsureCatalogAsync(db, "业务示例", "/business", "Document", 3, ct);
        await EnsureModuleAsync(db, business.Id, 1, "公告管理", "notice", "business/notice/index", "biz:notice",
            ["query", "add", "edit", "remove"], ct);
    }

    /// <summary>确保一个页面菜单及其按钮存在（按钮权限为 {prefix}:{action}，页面权限为 {prefix}:list）。</summary>
    private static async Task EnsureModuleAsync(
        AppDbContext db, long parentId, int order, string name, string path, string component, string permPrefix,
        string[] actions, CancellationToken ct)
    {
        var page = await EnsureMenuAsync(db, parentId, name, path, component, $"{permPrefix}:list", order, ct);

        var btnOrder = 0;
        foreach (var action in actions)
        {
            var label = action switch
            {
                "query" => "查询",
                "add" => "新增",
                "edit" => "修改",
                "remove" => "删除",
                "resetPwd" => "重置密码",
                _ => action,
            };
            await EnsureButtonAsync(db, page.Id, label, $"{permPrefix}:{action}", ++btnOrder, ct);
        }
    }

    private static async Task<Menu> EnsureCatalogAsync(
        AppDbContext db, string name, string path, string icon, int order, CancellationToken ct)
    {
        var existing = await db.Menus.FirstOrDefaultAsync(
            x => x.ParentId == 0 && x.Name == name && x.MenuType == MenuType.Catalog, ct);
        if (existing is not null) return existing;

        return await AddAsync(db, new Menu
        {
            Name = name, ParentId = 0, MenuType = MenuType.Catalog, Path = path, Icon = icon, OrderNum = order,
        }, ct);
    }

    private static async Task<Menu> EnsureMenuAsync(
        AppDbContext db, long parentId, string name, string path, string component, string permission, int order,
        CancellationToken ct)
    {
        var existing = await db.Menus.FirstOrDefaultAsync(x => x.Permission == permission && x.MenuType == MenuType.Menu, ct);
        if (existing is not null) return existing;

        return await AddAsync(db, new Menu
        {
            Name = name, ParentId = parentId, MenuType = MenuType.Menu,
            Path = path, Component = component, Permission = permission, OrderNum = order,
        }, ct);
    }

    private static async Task EnsureButtonAsync(
        AppDbContext db, long parentId, string name, string permission, int order, CancellationToken ct)
    {
        if (await db.Menus.AnyAsync(x => x.Permission == permission && x.MenuType == MenuType.Button, ct))
            return;

        await AddAsync(db, new Menu
        {
            Name = name, ParentId = parentId, MenuType = MenuType.Button, Permission = permission, OrderNum = order,
        }, ct);
    }

    private static async Task<Menu> AddAsync(AppDbContext db, Menu menu, CancellationToken ct)
    {
        db.Menus.Add(menu);
        await db.SaveChangesAsync(ct);
        return menu;
    }
}
