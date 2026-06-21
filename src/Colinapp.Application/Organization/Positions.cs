using Colinapp.Application.Common;
using Colinapp.Domain.Entities.System;
using Colinapp.Shared.Exceptions;
using Colinapp.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Colinapp.Application.Organization;

// ---------- DTO ----------

public class PositionDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int OrderNum { get; set; }
    public bool Enabled { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedTime { get; set; }
}

public class PositionSaveDto
{
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int OrderNum { get; set; }
    public bool Enabled { get; set; } = true;
    public string? Remark { get; set; }
}

// ---------- Service ----------

public interface IPositionService
{
    Task<PagedResult<PositionDto>> GetPagedAsync(PagedRequest query, CancellationToken ct = default);
    Task<List<PositionDto>> GetAllAsync(CancellationToken ct = default);
    Task<long> CreateAsync(PositionSaveDto dto, CancellationToken ct = default);
    Task UpdateAsync(long id, PositionSaveDto dto, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public class PositionService(IAppDbContext db) : IPositionService
{
    public async Task<PagedResult<PositionDto>> GetPagedAsync(PagedRequest query, CancellationToken ct = default)
    {
        var q = db.Positions.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(x => x.Name.Contains(query.Keyword) || x.Code.Contains(query.Keyword));

        var total = await q.CountAsync(ct);
        var entities = await q
            .OrderBy(x => x.OrderNum).ThenBy(x => x.Id)
            .Skip(query.Skip).Take(query.PageSize)
            .ToListAsync(ct);
        var items = entities.Select(ToDto).ToList();

        return new PagedResult<PositionDto>(items, total, query.PageIndex, query.PageSize);
    }

    public async Task<List<PositionDto>> GetAllAsync(CancellationToken ct = default)
    {
        var entities = await db.Positions
            .Where(x => x.Enabled)
            .OrderBy(x => x.OrderNum).ThenBy(x => x.Id)
            .ToListAsync(ct);
        return entities.Select(ToDto).ToList();
    }

    public async Task<long> CreateAsync(PositionSaveDto dto, CancellationToken ct = default)
    {
        if (await db.Positions.AnyAsync(x => x.Code == dto.Code, ct))
            throw new BusinessException("职位编码已存在");

        var entity = new Position
        {
            Name = dto.Name,
            Code = dto.Code,
            OrderNum = dto.OrderNum,
            Enabled = dto.Enabled,
            Remark = dto.Remark,
        };
        db.Positions.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateAsync(long id, PositionSaveDto dto, CancellationToken ct = default)
    {
        var entity = await db.Positions.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("职位不存在");

        if (await db.Positions.AnyAsync(x => x.Code == dto.Code && x.Id != id, ct))
            throw new BusinessException("职位编码已存在");

        entity.Name = dto.Name;
        entity.Code = dto.Code;
        entity.OrderNum = dto.OrderNum;
        entity.Enabled = dto.Enabled;
        entity.Remark = dto.Remark;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var entity = await db.Positions.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("职位不存在");

        if (await db.UserPosts.AnyAsync(x => x.PositionId == id, ct))
            throw new BusinessException("职位已分配给人员，不能删除");

        db.Positions.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    private static PositionDto ToDto(Position x) => new()
    {
        Id = x.Id,
        Name = x.Name,
        Code = x.Code,
        OrderNum = x.OrderNum,
        Enabled = x.Enabled,
        Remark = x.Remark,
        CreatedTime = x.CreatedTime,
    };
}
