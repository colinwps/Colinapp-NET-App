using Colinapp.Application.Common;
using Colinapp.Application.Permissions;
using Colinapp.Domain.Entities.System;
using Colinapp.Domain.Enums;
using Colinapp.Infrastructure.Persistence;
using Colinapp.Tests.Infrastructure;
using Xunit;

namespace Colinapp.Tests;

/// <summary>
/// 权限聚合链路：用户 → 角色 → 菜单。校验启用过滤、去重、空集兜底与跨请求缓存/失效。
/// </summary>
public class PermissionServiceTests
{
    private static void SeedRole(AppDbContext db, long id, bool enabled = true)
        => db.Roles.Add(new Role { Id = id, Name = $"r{id}", Code = $"r{id}", Enabled = enabled });

    private static void SeedMenu(AppDbContext db, long id, string? permission, bool enabled = true)
        => db.Menus.Add(new Menu { Id = id, Name = $"m{id}", MenuType = MenuType.Button, Permission = permission, Enabled = enabled });

    [Fact]
    public async Task Aggregates_permissions_through_user_role_menu()
    {
        await using var db = TestDbContextFactory.Create();
        SeedRole(db, 10);
        SeedMenu(db, 100, "sys:user:list");
        SeedMenu(db, 101, "sys:user:add");
        db.UserRoles.Add(new UserRole { UserId = 1, RoleId = 10 });
        db.RoleMenus.Add(new RoleMenu { RoleId = 10, MenuId = 100 });
        db.RoleMenus.Add(new RoleMenu { RoleId = 10, MenuId = 101 });
        await db.SaveChangesAsync();

        var sut = new PermissionService(db, TestCache.NoOp());
        var perms = await sut.GetUserPermissionsAsync(1);

        Assert.Equal(new[] { "sys:user:add", "sys:user:list" }, perms.OrderBy(p => p));
    }

    [Fact]
    public async Task Excludes_disabled_roles_and_menus_and_empty_permissions()
    {
        await using var db = TestDbContextFactory.Create();
        SeedRole(db, 10);                 // 启用角色
        SeedRole(db, 20, enabled: false); // 停用角色 → 其权限不计
        SeedMenu(db, 100, "sys:user:list");
        SeedMenu(db, 101, "sys:user:edit", enabled: false); // 停用菜单 → 不计
        SeedMenu(db, 102, "");                              // 空权限串 → 不计
        SeedMenu(db, 200, "sys:role:list");                // 挂在停用角色上
        db.UserRoles.Add(new UserRole { UserId = 1, RoleId = 10 });
        db.UserRoles.Add(new UserRole { UserId = 1, RoleId = 20 });
        db.RoleMenus.Add(new RoleMenu { RoleId = 10, MenuId = 100 });
        db.RoleMenus.Add(new RoleMenu { RoleId = 10, MenuId = 101 });
        db.RoleMenus.Add(new RoleMenu { RoleId = 10, MenuId = 102 });
        db.RoleMenus.Add(new RoleMenu { RoleId = 20, MenuId = 200 });
        await db.SaveChangesAsync();

        var sut = new PermissionService(db, TestCache.NoOp());
        var perms = await sut.GetUserPermissionsAsync(1);

        Assert.Equal(new[] { "sys:user:list" }, perms);
    }

    [Fact]
    public async Task Deduplicates_permissions_shared_across_roles()
    {
        await using var db = TestDbContextFactory.Create();
        SeedRole(db, 10);
        SeedRole(db, 11);
        SeedMenu(db, 100, "sys:user:list");
        db.UserRoles.Add(new UserRole { UserId = 1, RoleId = 10 });
        db.UserRoles.Add(new UserRole { UserId = 1, RoleId = 11 });
        db.RoleMenus.Add(new RoleMenu { RoleId = 10, MenuId = 100 });
        db.RoleMenus.Add(new RoleMenu { RoleId = 11, MenuId = 100 }); // 同一菜单挂两个角色
        await db.SaveChangesAsync();

        var sut = new PermissionService(db, TestCache.NoOp());
        var perms = await sut.GetUserPermissionsAsync(1);

        Assert.Single(perms);
        Assert.Contains("sys:user:list", perms);
    }

    [Fact]
    public async Task Returns_empty_for_user_without_roles()
    {
        await using var db = TestDbContextFactory.Create();
        SeedMenu(db, 100, "sys:user:list");
        await db.SaveChangesAsync();

        var sut = new PermissionService(db, TestCache.NoOp());
        var perms = await sut.GetUserPermissionsAsync(999);

        Assert.Empty(perms);
    }

    [Fact]
    public async Task Permission_lookup_is_case_insensitive()
    {
        await using var db = TestDbContextFactory.Create();
        SeedRole(db, 10);
        SeedMenu(db, 100, "Sys:User:List");
        db.UserRoles.Add(new UserRole { UserId = 1, RoleId = 10 });
        db.RoleMenus.Add(new RoleMenu { RoleId = 10, MenuId = 100 });
        await db.SaveChangesAsync();

        var sut = new PermissionService(db, TestCache.NoOp());
        var perms = await sut.GetUserPermissionsAsync(1);

        Assert.Contains("sys:user:list", perms); // OrdinalIgnoreCase 集合
    }

    [Fact]
    public async Task Cross_request_cache_serves_stale_until_invalidated()
    {
        await using var db = TestDbContextFactory.Create();
        SeedRole(db, 10);
        SeedMenu(db, 100, "sys:user:list");
        db.UserRoles.Add(new UserRole { UserId = 1, RoleId = 10 });
        var grant = new RoleMenu { RoleId = 10, MenuId = 100 };
        db.RoleMenus.Add(grant);
        await db.SaveChangesAsync();

        var cache = TestCache.Memory(); // 跨请求共享缓存

        // 第一次请求：填充缓存
        var first = await new PermissionService(db, cache).GetUserPermissionsAsync(1);
        Assert.Contains("sys:user:list", first);

        // 后台撤销授权，但未触发失效
        db.RoleMenus.Remove(grant);
        await db.SaveChangesAsync();

        // 新的服务实例（无请求内缓存）仍读到缓存的旧值
        var stale = await new PermissionService(db, cache).GetUserPermissionsAsync(1);
        Assert.Contains("sys:user:list", stale);

        // 失效后重算，读到最新（空）
        await cache.RemoveAsync(CacheKeys.UserPermissions(1));
        var fresh = await new PermissionService(db, cache).GetUserPermissionsAsync(1);
        Assert.Empty(fresh);
    }
}
