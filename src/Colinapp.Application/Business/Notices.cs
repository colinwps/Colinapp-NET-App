using Colinapp.Application.Common;
using Colinapp.Domain.Entities.Business;
using Colinapp.Domain.Enums;
using Colinapp.Shared.Exceptions;
using Colinapp.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Colinapp.Application.Business;

// ---------- DTO ----------

public class NoticeQuery : PagedRequest
{
    public NoticeType? NoticeType { get; set; }
    public bool? Published { get; set; }
}

public class NoticeDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public NoticeType NoticeType { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool Published { get; set; }
    public DateTime CreatedTime { get; set; }
}

public class NoticeSaveDto
{
    public string Title { get; set; } = string.Empty;
    public NoticeType NoticeType { get; set; } = NoticeType.Notification;
    public string Content { get; set; } = string.Empty;
    public bool Published { get; set; }
}

// ---------- Service ----------

public interface INoticeService
{
    Task<PagedResult<NoticeDto>> GetPagedAsync(NoticeQuery query, CancellationToken ct = default);
    Task<NoticeDto> GetAsync(long id, CancellationToken ct = default);
    Task<long> CreateAsync(NoticeSaveDto dto, CancellationToken ct = default);
    Task UpdateAsync(long id, NoticeSaveDto dto, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public class NoticeService(IAppDbContext db) : INoticeService
{
    public async Task<PagedResult<NoticeDto>> GetPagedAsync(NoticeQuery query, CancellationToken ct = default)
    {
        var q = db.Notices.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(x => x.Title.Contains(query.Keyword));
        if (query.NoticeType is { } t)
            q = q.Where(x => x.NoticeType == t);
        if (query.Published is { } p)
            q = q.Where(x => x.Published == p);

        var total = await q.CountAsync(ct);
        var entities = await q
            .OrderByDescending(x => x.Id)
            .Skip(query.Skip).Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<NoticeDto>(entities.Select(ToDto).ToList(), total, query.PageIndex, query.PageSize);
    }

    public async Task<NoticeDto> GetAsync(long id, CancellationToken ct = default)
    {
        var entity = await db.Notices.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("公告不存在");
        return ToDto(entity);
    }

    public async Task<long> CreateAsync(NoticeSaveDto dto, CancellationToken ct = default)
    {
        var entity = Apply(new Notice(), dto);
        db.Notices.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }

    public async Task UpdateAsync(long id, NoticeSaveDto dto, CancellationToken ct = default)
    {
        var entity = await db.Notices.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("公告不存在");
        Apply(entity, dto);
        await db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var entity = await db.Notices.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("公告不存在");
        db.Notices.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    private static Notice Apply(Notice e, NoticeSaveDto dto)
    {
        e.Title = dto.Title;
        e.NoticeType = dto.NoticeType;
        e.Content = dto.Content;
        e.Published = dto.Published;
        return e;
    }

    private static NoticeDto ToDto(Notice x) => new()
    {
        Id = x.Id,
        Title = x.Title,
        NoticeType = x.NoticeType,
        Content = x.Content,
        Published = x.Published,
        CreatedTime = x.CreatedTime,
    };
}
