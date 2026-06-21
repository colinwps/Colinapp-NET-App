using Colinapp.Application.Common;
using Colinapp.Domain.Entities.System;
using Colinapp.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Colinapp.Application.Organization;

// ---------- DTO ----------

/// <summary>部门节点（树形返回，含子节点）。</summary>
public class DepartmentDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public long ParentId { get; set; }
    public int OrderNum { get; set; }
    public long? LeaderUserId { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool Enabled { get; set; }
    public DateTime CreatedTime { get; set; }
    public List<DepartmentDto> Children { get; set; } = [];
}

/// <summary>部门新增/编辑入参。</summary>
public class DepartmentSaveDto
{
    public string Name { get; set; } = string.Empty;
    public long ParentId { get; set; }
    public int OrderNum { get; set; }
    public long? LeaderUserId { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool Enabled { get; set; } = true;
}

// ---------- Service ----------

public interface IDepartmentService
{
    Task<List<DepartmentDto>> GetTreeAsync(CancellationToken ct = default);
    Task<DepartmentDto> GetAsync(long id, CancellationToken ct = default);
    Task<long> CreateAsync(DepartmentSaveDto dto, CancellationToken ct = default);
    Task UpdateAsync(long id, DepartmentSaveDto dto, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public class DepartmentService(IAppDbContext db) : IDepartmentService
{
    public async Task<List<DepartmentDto>> GetTreeAsync(CancellationToken ct = default)
    {
        var entities = await db.Departments
            .OrderBy(x => x.OrderNum).ThenBy(x => x.Id)
            .ToListAsync(ct);
        var all = entities.Select(ToDto).ToList();
        return BuildTree(all, 0);
    }

    public async Task<DepartmentDto> GetAsync(long id, CancellationToken ct = default)
    {
        var dept = await db.Departments.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("部门不存在");
        return ToDto(dept);
    }

    public async Task<long> CreateAsync(DepartmentSaveDto dto, CancellationToken ct = default)
    {
        var entity = new Department
        {
            Name = dto.Name,
            ParentId = dto.ParentId,
            Ancestors = await BuildAncestorsAsync(dto.ParentId, ct),
            OrderNum = dto.OrderNum,
            LeaderUserId = dto.LeaderUserId,
            Phone = dto.Phone,
            Email = dto.Email,
            Enabled = dto.Enabled,
        };
        db.Departments.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateAsync(long id, DepartmentSaveDto dto, CancellationToken ct = default)
    {
        var entity = await db.Departments.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("部门不存在");

        if (dto.ParentId == id)
            throw new BusinessException("上级部门不能是自己");

        entity.Name = dto.Name;
        entity.ParentId = dto.ParentId;
        entity.Ancestors = await BuildAncestorsAsync(dto.ParentId, ct);
        entity.OrderNum = dto.OrderNum;
        entity.LeaderUserId = dto.LeaderUserId;
        entity.Phone = dto.Phone;
        entity.Email = dto.Email;
        entity.Enabled = dto.Enabled;

        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var entity = await db.Departments.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("部门不存在");

        if (await db.Departments.AnyAsync(x => x.ParentId == id, ct))
            throw new BusinessException("存在下级部门，不能删除");

        if (await db.Users.AnyAsync(x => x.DeptId == id, ct))
            throw new BusinessException("部门下存在人员，不能删除");

        db.Departments.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    private async Task<string> BuildAncestorsAsync(long parentId, CancellationToken ct)
    {
        if (parentId == 0) return "0";
        var parent = await db.Departments.FirstOrDefaultAsync(x => x.Id == parentId, ct)
            ?? throw new BusinessException("上级部门不存在");
        return $"{parent.Ancestors},{parent.Id}";
    }

    private static DepartmentDto ToDto(Department x) => new()
    {
        Id = x.Id,
        Name = x.Name,
        ParentId = x.ParentId,
        OrderNum = x.OrderNum,
        LeaderUserId = x.LeaderUserId,
        Phone = x.Phone,
        Email = x.Email,
        Enabled = x.Enabled,
        CreatedTime = x.CreatedTime,
    };

    private static List<DepartmentDto> BuildTree(List<DepartmentDto> all, long parentId)
    {
        var children = all.Where(x => x.ParentId == parentId).ToList();
        foreach (var node in children)
            node.Children = BuildTree(all, node.Id);
        return children;
    }
}
