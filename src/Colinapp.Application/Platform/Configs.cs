using Colinapp.Application.Common;
using Colinapp.Domain.Entities.System;
using Colinapp.Shared.Exceptions;
using Colinapp.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Colinapp.Application.Platform;

// ---------- DTO ----------

public class ConfigDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ConfigKey { get; set; } = string.Empty;
    public string ConfigValue { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedTime { get; set; }
}

public class ConfigSaveDto
{
    public string Name { get; set; } = string.Empty;
    public string ConfigKey { get; set; } = string.Empty;
    public string ConfigValue { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public string? Remark { get; set; }
}

// ---------- Service ----------

public interface IConfigService
{
    Task<PagedResult<ConfigDto>> GetPagedAsync(PagedRequest query, CancellationToken ct = default);

    /// <summary>按键读取参数值（带缓存），不存在返回 null。</summary>
    Task<string?> GetValueAsync(string key, CancellationToken ct = default);

    Task<long> CreateAsync(ConfigSaveDto dto, CancellationToken ct = default);
    Task UpdateAsync(long id, ConfigSaveDto dto, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public class ConfigService(IAppDbContext db, ICacheService cache) : IConfigService
{
    public async Task<PagedResult<ConfigDto>> GetPagedAsync(PagedRequest query, CancellationToken ct = default)
    {
        var q = db.SysConfigs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(x => x.Name.Contains(query.Keyword) || x.ConfigKey.Contains(query.Keyword));

        var total = await q.CountAsync(ct);
        var entities = await q
            .OrderByDescending(x => x.Id)
            .Skip(query.Skip).Take(query.PageSize)
            .ToListAsync(ct);

        var items = entities.Select(ToDto).ToList();
        return new PagedResult<ConfigDto>(items, total, query.PageIndex, query.PageSize);
    }

    public async Task<string?> GetValueAsync(string key, CancellationToken ct = default)
        => await cache.GetOrSetAsync(CacheKeys.Config(key), async () =>
            await db.SysConfigs
                .Where(x => x.ConfigKey == key)
                .Select(x => x.ConfigValue)
                .FirstOrDefaultAsync(ct),
            ct: ct);

    public async Task<long> CreateAsync(ConfigSaveDto dto, CancellationToken ct = default)
    {
        if (await db.SysConfigs.AnyAsync(x => x.ConfigKey == dto.ConfigKey, ct))
            throw new BusinessException("参数键已存在");

        var entity = Apply(new SysConfig(), dto);
        db.SysConfigs.Add(entity);
        await db.SaveChangesAsync(ct);
        await cache.RemoveAsync(CacheKeys.Config(entity.ConfigKey), ct);
        return entity.Id;
    }

    public async Task UpdateAsync(long id, ConfigSaveDto dto, CancellationToken ct = default)
    {
        var entity = await db.SysConfigs.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("参数不存在");
        if (await db.SysConfigs.AnyAsync(x => x.ConfigKey == dto.ConfigKey && x.Id != id, ct))
            throw new BusinessException("参数键已存在");

        var oldKey = entity.ConfigKey;
        Apply(entity, dto);
        await db.SaveChangesAsync(ct);
        await cache.RemoveAsync(CacheKeys.Config(oldKey), ct);
        await cache.RemoveAsync(CacheKeys.Config(entity.ConfigKey), ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var entity = await db.SysConfigs.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("参数不存在");
        if (entity.IsSystem)
            throw new BusinessException("系统内置参数不能删除");

        db.SysConfigs.Remove(entity);
        await db.SaveChangesAsync(ct);
        await cache.RemoveAsync(CacheKeys.Config(entity.ConfigKey), ct);
    }

    private static SysConfig Apply(SysConfig e, ConfigSaveDto dto)
    {
        e.Name = dto.Name;
        e.ConfigKey = dto.ConfigKey;
        e.ConfigValue = dto.ConfigValue;
        e.IsSystem = dto.IsSystem;
        e.Remark = dto.Remark;
        return e;
    }

    private static ConfigDto ToDto(SysConfig x) => new()
    {
        Id = x.Id, Name = x.Name, ConfigKey = x.ConfigKey, ConfigValue = x.ConfigValue,
        IsSystem = x.IsSystem, Remark = x.Remark, CreatedTime = x.CreatedTime,
    };
}
