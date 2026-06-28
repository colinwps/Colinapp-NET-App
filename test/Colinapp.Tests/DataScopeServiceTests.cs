using Colinapp.Domain.Entities.System;
using Colinapp.Domain.Enums;
using Colinapp.Infrastructure.Persistence;
using Colinapp.Application.Permissions;
using Colinapp.Tests.Infrastructure;
using Xunit;

namespace Colinapp.Tests;

/// <summary>
/// 数据范围解析：管理员=全部、无角色=仅本人兜底、五种范围的并集（最宽松）。
/// </summary>
public class DataScopeServiceTests
{
    private static void SeedRole(AppDbContext db, long id, DataScope scope)
        => db.Roles.Add(new Role { Id = id, Name = $"r{id}", Code = $"r{id}", Enabled = true, DataScope = scope });

    private static void Assign(AppDbContext db, long userId, long roleId)
        => db.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });

    [Fact]
    public async Task Admin_sees_all()
    {
        await using var db = TestDbContextFactory.Create();
        var sut = new DataScopeService(db, TestCurrentUser.Admin(1));

        var result = await sut.ResolveAsync();

        Assert.True(result.All);
    }

    [Fact]
    public async Task Anonymous_sees_nothing()
    {
        await using var db = TestDbContextFactory.Create();
        var sut = new DataScopeService(db, TestCurrentUser.Anonymous);

        var result = await sut.ResolveAsync();

        Assert.False(result.All);
        Assert.False(result.SelfOnly);
        Assert.Empty(result.DeptIds);
    }

    [Fact]
    public async Task User_without_roles_falls_back_to_self_only()
    {
        await using var db = TestDbContextFactory.Create();
        db.Users.Add(new User { Id = 5, UserName = "u5", DeptId = 2 });
        await db.SaveChangesAsync();

        var sut = new DataScopeService(db, TestCurrentUser.Normal(5));
        var result = await sut.ResolveAsync();

        Assert.False(result.All);
        Assert.True(result.SelfOnly);
        Assert.Empty(result.DeptIds);
    }

    [Fact]
    public async Task Scope_all_overrides_others()
    {
        await using var db = TestDbContextFactory.Create();
        db.Users.Add(new User { Id = 5, UserName = "u5", DeptId = 2 });
        SeedRole(db, 10, DataScope.Self);
        SeedRole(db, 11, DataScope.All);
        Assign(db, 5, 10);
        Assign(db, 5, 11);
        await db.SaveChangesAsync();

        var sut = new DataScopeService(db, TestCurrentUser.Normal(5));
        var result = await sut.ResolveAsync();

        Assert.True(result.All); // 含「全部」角色即最宽松
    }

    [Fact]
    public async Task Scope_self_sets_self_only()
    {
        await using var db = TestDbContextFactory.Create();
        db.Users.Add(new User { Id = 5, UserName = "u5", DeptId = 2 });
        SeedRole(db, 10, DataScope.Self);
        Assign(db, 5, 10);
        await db.SaveChangesAsync();

        var sut = new DataScopeService(db, TestCurrentUser.Normal(5));
        var result = await sut.ResolveAsync();

        Assert.False(result.All);
        Assert.True(result.SelfOnly);
        Assert.Empty(result.DeptIds);
    }

    [Fact]
    public async Task Scope_dept_returns_own_department_only()
    {
        await using var db = TestDbContextFactory.Create();
        db.Users.Add(new User { Id = 5, UserName = "u5", DeptId = 2 });
        SeedRole(db, 10, DataScope.Dept);
        Assign(db, 5, 10);
        await db.SaveChangesAsync();

        var sut = new DataScopeService(db, TestCurrentUser.Normal(5));
        var result = await sut.ResolveAsync();

        Assert.Equal(new long[] { 2 }, result.DeptIds.OrderBy(x => x));
    }

    [Fact]
    public async Task Scope_dept_and_child_includes_descendants_via_ancestors()
    {
        await using var db = TestDbContextFactory.Create();
        // 部门树：1(根) → 2 → 3，旁支 4 在根下。用户在部门 2。
        db.Departments.Add(new Department { Id = 1, Name = "d1", ParentId = 0, Ancestors = "0" });
        db.Departments.Add(new Department { Id = 2, Name = "d2", ParentId = 1, Ancestors = "0,1" });
        db.Departments.Add(new Department { Id = 3, Name = "d3", ParentId = 2, Ancestors = "0,1,2" });
        db.Departments.Add(new Department { Id = 4, Name = "d4", ParentId = 1, Ancestors = "0,1" });
        db.Users.Add(new User { Id = 5, UserName = "u5", DeptId = 2 });
        SeedRole(db, 10, DataScope.DeptAndChild);
        Assign(db, 5, 10);
        await db.SaveChangesAsync();

        var sut = new DataScopeService(db, TestCurrentUser.Normal(5));
        var result = await sut.ResolveAsync();

        // 本部门 2 + 其子孙 3（Ancestors 含 ",2,"）；4 是兄弟，不含。
        Assert.Equal(new long[] { 2, 3 }, result.DeptIds.OrderBy(x => x));
    }

    [Fact]
    public async Task Scope_custom_returns_role_departments()
    {
        await using var db = TestDbContextFactory.Create();
        db.Users.Add(new User { Id = 5, UserName = "u5", DeptId = 2 });
        SeedRole(db, 10, DataScope.Custom);
        Assign(db, 5, 10);
        db.RoleDepts.Add(new RoleDept { RoleId = 10, DeptId = 7 });
        db.RoleDepts.Add(new RoleDept { RoleId = 10, DeptId = 8 });
        await db.SaveChangesAsync();

        var sut = new DataScopeService(db, TestCurrentUser.Normal(5));
        var result = await sut.ResolveAsync();

        Assert.Equal(new long[] { 7, 8 }, result.DeptIds.OrderBy(x => x));
    }

    [Fact]
    public async Task Multiple_scopes_union_departments_and_self()
    {
        await using var db = TestDbContextFactory.Create();
        db.Users.Add(new User { Id = 5, UserName = "u5", DeptId = 2 });
        SeedRole(db, 10, DataScope.Dept);    // → {2}
        SeedRole(db, 11, DataScope.Custom);  // → {7}
        SeedRole(db, 12, DataScope.Self);    // → SelfOnly
        Assign(db, 5, 10);
        Assign(db, 5, 11);
        Assign(db, 5, 12);
        db.RoleDepts.Add(new RoleDept { RoleId = 11, DeptId = 7 });
        await db.SaveChangesAsync();

        var sut = new DataScopeService(db, TestCurrentUser.Normal(5));
        var result = await sut.ResolveAsync();

        Assert.False(result.All);
        Assert.True(result.SelfOnly);
        Assert.Equal(new long[] { 2, 7 }, result.DeptIds.OrderBy(x => x));
    }
}
