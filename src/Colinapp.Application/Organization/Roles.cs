using Colinapp.Application.Common;
using Colinapp.Domain.Entities.System;
using Colinapp.Domain.Enums;
using Colinapp.Shared.Exceptions;
using Colinapp.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Colinapp.Application.Organization;

// ---------- DTO ----------

public class RoleDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DataScope DataScope { get; set; }
    public int OrderNum { get; set; }
    public bool Enabled { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedTime { get; set; }
}

public class RoleDetailDto : RoleDto
{
    public List<long> MenuIds { get; set; } = [];
    public List<long> DeptIds { get; set; } = [];
}

public class RoleSaveDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DataScope DataScope { get; set; } = DataScope.All;
    public int OrderNum { get; set; }
    public bool Enabled { get; set; } = true;
    public string? Remark { get; set; }
    /// <summary>角色绑定的菜单/按钮 Id 集合（功能权限）。</summary>
    public List<long> MenuIds { get; set; } = [];
    /// <summary>自定义数据范围的部门 Id 集合（仅 DataScope=Custom 生效）。</summary>
    public List<long> DeptIds { get; set; } = [];
}

// ---------- Service ----------

public interface IRoleService
{
    Task<PagedResult<RoleDto>> GetPagedAsync(PagedRequest query, CancellationToken ct = default);
    Task<List<RoleDto>> GetAllAsync(CancellationToken ct = default);
    Task<RoleDetailDto> GetAsync(long id, CancellationToken ct = default);
    Task<long> CreateAsync(RoleSaveDto dto, CancellationToken ct = default);
    Task UpdateAsync(long id, RoleSaveDto dto, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public class RoleService(IAppDbContext db, ICacheService cache) : IRoleService
{
    public async Task<PagedResult<RoleDto>> GetPagedAsync(PagedRequest query, CancellationToken ct = default)
    {
        var q = db.Roles.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(x => x.Name.Contains(query.Keyword) || x.Code.Contains(query.Keyword));

        var total = await q.CountAsync(ct);
        var entities = await q
            .OrderBy(x => x.OrderNum).ThenBy(x => x.Id)
            .Skip(query.Skip).Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<RoleDto>(entities.Select(ToDto).ToList(), total, query.PageIndex, query.PageSize);
    }

    public async Task<List<RoleDto>> GetAllAsync(CancellationToken ct = default)
    {
        var entities = await db.Roles
            .Where(x => x.Enabled)
            .OrderBy(x => x.OrderNum).ThenBy(x => x.Id)
            .ToListAsync(ct);
        return entities.Select(ToDto).ToList();
    }

    public async Task<RoleDetailDto> GetAsync(long id, CancellationToken ct = default)
    {
        var role = await db.Roles.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("角色不存在");

        var dto = new RoleDetailDto
        {
            Id = role.Id,
            Name = role.Name,
            Code = role.Code,
            DataScope = role.DataScope,
            OrderNum = role.OrderNum,
            Enabled = role.Enabled,
            Remark = role.Remark,
            CreatedTime = role.CreatedTime,
            MenuIds = await db.RoleMenus.Where(x => x.RoleId == id).Select(x => x.MenuId).ToListAsync(ct),
            DeptIds = await db.RoleDepts.Where(x => x.RoleId == id).Select(x => x.DeptId).ToListAsync(ct),
        };
        return dto;
    }

    public async Task<long> CreateAsync(RoleSaveDto dto, CancellationToken ct = default)
    {
        if (await db.Roles.AnyAsync(x => x.Code == dto.Code, ct))
            throw new BusinessException("角色编码已存在");

        var role = new Role
        {
            Name = dto.Name,
            Code = dto.Code,
            DataScope = dto.DataScope,
            OrderNum = dto.OrderNum,
            Enabled = dto.Enabled,
            Remark = dto.Remark,
        };
        db.Roles.Add(role);
        await db.SaveChangesAsync(ct);

        await SyncRelationsAsync(role.Id, dto, ct);
        await db.SaveChangesAsync(ct);
        await cache.RemoveByPrefixAsync(CacheKeys.PermissionPrefix, ct);
        return role.Id;
    }

    public async Task UpdateAsync(long id, RoleSaveDto dto, CancellationToken ct = default)
    {
        var role = await db.Roles.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("角色不存在");

        if (await db.Roles.AnyAsync(x => x.Code == dto.Code && x.Id != id, ct))
            throw new BusinessException("角色编码已存在");

        role.Name = dto.Name;
        role.Code = dto.Code;
        role.DataScope = dto.DataScope;
        role.OrderNum = dto.OrderNum;
        role.Enabled = dto.Enabled;
        role.Remark = dto.Remark;

        // 覆盖式重建关联
        var oldMenus = await db.RoleMenus.Where(x => x.RoleId == id).ToListAsync(ct);
        db.RoleMenus.RemoveRange(oldMenus);
        var oldDepts = await db.RoleDepts.Where(x => x.RoleId == id).ToListAsync(ct);
        db.RoleDepts.RemoveRange(oldDepts);

        await SyncRelationsAsync(id, dto, ct);
        await db.SaveChangesAsync(ct);
        await cache.RemoveByPrefixAsync(CacheKeys.PermissionPrefix, ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var role = await db.Roles.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("角色不存在");

        if (await db.UserRoles.AnyAsync(x => x.RoleId == id, ct))
            throw new BusinessException("角色已分配给人员，不能删除");

        var menus = await db.RoleMenus.Where(x => x.RoleId == id).ToListAsync(ct);
        db.RoleMenus.RemoveRange(menus);
        var depts = await db.RoleDepts.Where(x => x.RoleId == id).ToListAsync(ct);
        db.RoleDepts.RemoveRange(depts);
        db.Roles.Remove(role);
        await db.SaveChangesAsync(ct);
        await cache.RemoveByPrefixAsync(CacheKeys.PermissionPrefix, ct);
    }

    private Task SyncRelationsAsync(long roleId, RoleSaveDto dto, CancellationToken ct)
    {
        foreach (var menuId in dto.MenuIds.Distinct())
            db.RoleMenus.Add(new RoleMenu { RoleId = roleId, MenuId = menuId });

        if (dto.DataScope == DataScope.Custom)
            foreach (var deptId in dto.DeptIds.Distinct())
                db.RoleDepts.Add(new RoleDept { RoleId = roleId, DeptId = deptId });

        return Task.CompletedTask;
    }

    private static RoleDto ToDto(Role x) => new()
    {
        Id = x.Id,
        Name = x.Name,
        Code = x.Code,
        DataScope = x.DataScope,
        OrderNum = x.OrderNum,
        Enabled = x.Enabled,
        Remark = x.Remark,
        CreatedTime = x.CreatedTime,
    };
}
