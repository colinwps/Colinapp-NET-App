using Colinapp.Application.Common;
using Colinapp.Domain.Entities.System;
using Colinapp.Shared.Exceptions;
using Colinapp.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Colinapp.Application.Platform;

// ---------- DTO ----------

public class DictTypeDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedTime { get; set; }
}

public class DictTypeSaveDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
    public string? Remark { get; set; }
}

public class DictDataDto
{
    public long Id { get; set; }
    public string DictType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int OrderNum { get; set; }
    public string? TagType { get; set; }
    public bool IsDefault { get; set; }
    public bool Enabled { get; set; }
    public string? Remark { get; set; }
}

public class DictDataSaveDto
{
    public string DictType { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int OrderNum { get; set; }
    public string? TagType { get; set; }
    public bool IsDefault { get; set; }
    public bool Enabled { get; set; } = true;
    public string? Remark { get; set; }
}

// ---------- Service ----------

public interface IDictService
{
    Task<PagedResult<DictTypeDto>> GetTypesAsync(PagedRequest query, CancellationToken ct = default);
    Task<long> CreateTypeAsync(DictTypeSaveDto dto, CancellationToken ct = default);
    Task UpdateTypeAsync(long id, DictTypeSaveDto dto, CancellationToken ct = default);
    Task DeleteTypeAsync(long id, CancellationToken ct = default);

    /// <summary>按字典类型编码取启用数据项（前端下拉用，公开给已登录用户）。</summary>
    Task<List<DictDataDto>> GetDataByTypeAsync(string type, CancellationToken ct = default);
    Task<PagedResult<DictDataDto>> GetDataPagedAsync(string type, PagedRequest query, CancellationToken ct = default);
    Task<long> CreateDataAsync(DictDataSaveDto dto, CancellationToken ct = default);
    Task UpdateDataAsync(long id, DictDataSaveDto dto, CancellationToken ct = default);
    Task DeleteDataAsync(long id, CancellationToken ct = default);
}

public class DictService(IAppDbContext db) : IDictService
{
    public async Task<PagedResult<DictTypeDto>> GetTypesAsync(PagedRequest query, CancellationToken ct = default)
    {
        var q = db.DictTypes.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(x => x.Name.Contains(query.Keyword) || x.Type.Contains(query.Keyword));

        var total = await q.CountAsync(ct);
        var entities = await q
            .OrderByDescending(x => x.Id)
            .Skip(query.Skip).Take(query.PageSize)
            .ToListAsync(ct);

        var items = entities.Select(ToTypeDto).ToList();
        return new PagedResult<DictTypeDto>(items, total, query.PageIndex, query.PageSize);
    }

    public async Task<long> CreateTypeAsync(DictTypeSaveDto dto, CancellationToken ct = default)
    {
        if (await db.DictTypes.AnyAsync(x => x.Type == dto.Type, ct))
            throw new BusinessException("字典类型编码已存在");

        var entity = new DictType { Name = dto.Name, Type = dto.Type, Enabled = dto.Enabled, Remark = dto.Remark };
        db.DictTypes.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateTypeAsync(long id, DictTypeSaveDto dto, CancellationToken ct = default)
    {
        var entity = await db.DictTypes.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("字典类型不存在");
        if (await db.DictTypes.AnyAsync(x => x.Type == dto.Type && x.Id != id, ct))
            throw new BusinessException("字典类型编码已存在");

        entity.Name = dto.Name;
        entity.Type = dto.Type;
        entity.Enabled = dto.Enabled;
        entity.Remark = dto.Remark;
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteTypeAsync(long id, CancellationToken ct = default)
    {
        var entity = await db.DictTypes.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("字典类型不存在");
        if (await db.DictData.AnyAsync(x => x.DictType == entity.Type, ct))
            throw new BusinessException("该字典类型下存在数据项，不能删除");

        db.DictTypes.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task<List<DictDataDto>> GetDataByTypeAsync(string type, CancellationToken ct = default)
    {
        var entities = await db.DictData
            .Where(x => x.DictType == type && x.Enabled)
            .OrderBy(x => x.OrderNum).ThenBy(x => x.Id)
            .ToListAsync(ct);
        return entities.Select(ToDataDto).ToList();
    }

    public async Task<PagedResult<DictDataDto>> GetDataPagedAsync(string type, PagedRequest query, CancellationToken ct = default)
    {
        var q = db.DictData.Where(x => x.DictType == type);
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(x => x.Label.Contains(query.Keyword) || x.Value.Contains(query.Keyword));

        var total = await q.CountAsync(ct);
        var entities = await q
            .OrderBy(x => x.OrderNum).ThenBy(x => x.Id)
            .Skip(query.Skip).Take(query.PageSize)
            .ToListAsync(ct);

        var items = entities.Select(ToDataDto).ToList();
        return new PagedResult<DictDataDto>(items, total, query.PageIndex, query.PageSize);
    }

    public async Task<long> CreateDataAsync(DictDataSaveDto dto, CancellationToken ct = default)
    {
        var entity = ApplyData(new DictData(), dto);
        db.DictData.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateDataAsync(long id, DictDataSaveDto dto, CancellationToken ct = default)
    {
        var entity = await db.DictData.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("字典数据不存在");
        ApplyData(entity, dto);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteDataAsync(long id, CancellationToken ct = default)
    {
        var entity = await db.DictData.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("字典数据不存在");
        db.DictData.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    private static DictData ApplyData(DictData e, DictDataSaveDto dto)
    {
        e.DictType = dto.DictType;
        e.Label = dto.Label;
        e.Value = dto.Value;
        e.OrderNum = dto.OrderNum;
        e.TagType = dto.TagType;
        e.IsDefault = dto.IsDefault;
        e.Enabled = dto.Enabled;
        e.Remark = dto.Remark;
        return e;
    }

    private static DictTypeDto ToTypeDto(DictType x) => new()
    {
        Id = x.Id, Name = x.Name, Type = x.Type, Enabled = x.Enabled, Remark = x.Remark, CreatedTime = x.CreatedTime,
    };

    private static DictDataDto ToDataDto(DictData x) => new()
    {
        Id = x.Id, DictType = x.DictType, Label = x.Label, Value = x.Value, OrderNum = x.OrderNum,
        TagType = x.TagType, IsDefault = x.IsDefault, Enabled = x.Enabled, Remark = x.Remark,
    };
}
