using Colinapp.Application.Common;
using Colinapp.Application.Permissions;
using Colinapp.Domain.Entities.System;
using Colinapp.Domain.Enums;
using Colinapp.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Colinapp.Application.Organization;

// ---------- DTO ----------

public class MenuDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long ParentId { get; set; }
    public int OrderNum { get; set; }
    public MenuType MenuType { get; set; }
    public string? Path { get; set; }
    public string? Component { get; set; }
    public string? Permission { get; set; }
    public string? Icon { get; set; }
    public bool IsExternal { get; set; }
    public bool Cache { get; set; }
    public bool Visible { get; set; }
    public bool Enabled { get; set; }
    public List<MenuDto> Children { get; set; } = [];
}

public class MenuSaveDto
{
    public string Name { get; set; } = string.Empty;
    public long ParentId { get; set; }
    public int OrderNum { get; set; }
    public MenuType MenuType { get; set; } = MenuType.Menu;
    public string? Path { get; set; }
    public string? Component { get; set; }
    public string? Permission { get; set; }
    public string? Icon { get; set; }
    public bool IsExternal { get; set; }
    public bool Cache { get; set; }
    public bool Visible { get; set; } = true;
    public bool Enabled { get; set; } = true;
}

// ---------- Service ----------

public interface IMenuService
{
    /// <summary>全部菜单树（菜单管理用）。</summary>
    Task<List<MenuDto>> GetTreeAsync(CancellationToken ct = default);

    /// <summary>当前用户可见的菜单树（前端动态路由用，排除按钮、停用、隐藏由前端决定）。</summary>
    Task<List<MenuDto>> GetCurrentUserMenusAsync(CancellationToken ct = default);

    Task<MenuDto> GetAsync(long id, CancellationToken ct = default);
    Task<long> CreateAsync(MenuSaveDto dto, CancellationToken ct = default);
    Task UpdateAsync(long id, MenuSaveDto dto, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public class MenuService(IAppDbContext db, ICurrentUser currentUser, IPermissionService permissionService) : IMenuService
{
    public async Task<List<MenuDto>> GetTreeAsync(CancellationToken ct = default)
    {
        var entities = await db.Menus
            .OrderBy(x => x.OrderNum).ThenBy(x => x.Id)
            .ToListAsync(ct);
        return BuildTree(entities.Select(ToDto).ToList(), 0);
    }

    public async Task<List<MenuDto>> GetCurrentUserMenusAsync(CancellationToken ct = default)
    {
        var query = db.Menus.Where(m => m.Enabled && m.MenuType != MenuType.Button);

        // 非管理员仅返回其角色拥有的菜单
        if (currentUser is { IsAdmin: false, UserId: { } userId })
        {
            var roleIds = await permissionService.GetUserRoleIdsAsync(userId, ct);
            var allowedMenuIds = await db.RoleMenus
                .Where(rm => roleIds.Contains(rm.RoleId))
                .Select(rm => rm.MenuId)
                .Distinct()
                .ToListAsync(ct);
            query = query.Where(m => allowedMenuIds.Contains(m.Id));
        }

        var entities = await query.OrderBy(x => x.OrderNum).ThenBy(x => x.Id).ToListAsync(ct);
        return BuildTree(entities.Select(ToDto).ToList(), 0);
    }

    public async Task<MenuDto> GetAsync(long id, CancellationToken ct = default)
    {
        var menu = await db.Menus.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("菜单不存在");
        return ToDto(menu);
    }

    public async Task<long> CreateAsync(MenuSaveDto dto, CancellationToken ct = default)
    {
        var entity = Apply(new Menu(), dto);
        db.Menus.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateAsync(long id, MenuSaveDto dto, CancellationToken ct = default)
    {
        var entity = await db.Menus.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("菜单不存在");

        if (dto.ParentId == id)
            throw new BusinessException("上级菜单不能是自己");

        Apply(entity, dto);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var entity = await db.Menus.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("菜单不存在");

        if (await db.Menus.AnyAsync(x => x.ParentId == id, ct))
            throw new BusinessException("存在下级菜单，不能删除");

        var refs = await db.RoleMenus.Where(x => x.MenuId == id).ToListAsync(ct);
        db.RoleMenus.RemoveRange(refs);
        db.Menus.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    private static Menu Apply(Menu entity, MenuSaveDto dto)
    {
        entity.Name = dto.Name;
        entity.ParentId = dto.ParentId;
        entity.OrderNum = dto.OrderNum;
        entity.MenuType = dto.MenuType;
        entity.Path = dto.Path;
        entity.Component = dto.Component;
        entity.Permission = dto.Permission;
        entity.Icon = dto.Icon;
        entity.IsExternal = dto.IsExternal;
        entity.Cache = dto.Cache;
        entity.Visible = dto.Visible;
        entity.Enabled = dto.Enabled;
        return entity;
    }

    private static MenuDto ToDto(Menu x) => new()
    {
        Id = x.Id,
        Name = x.Name,
        ParentId = x.ParentId,
        OrderNum = x.OrderNum,
        MenuType = x.MenuType,
        Path = x.Path,
        Component = x.Component,
        Permission = x.Permission,
        Icon = x.Icon,
        IsExternal = x.IsExternal,
        Cache = x.Cache,
        Visible = x.Visible,
        Enabled = x.Enabled,
    };

    private static List<MenuDto> BuildTree(List<MenuDto> all, long parentId)
    {
        var children = all.Where(x => x.ParentId == parentId).ToList();
        foreach (var node in children)
            node.Children = BuildTree(all, node.Id);
        return children;
    }
}
