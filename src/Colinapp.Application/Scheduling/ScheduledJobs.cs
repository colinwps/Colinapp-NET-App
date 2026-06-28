using Colinapp.Application.Common;
using Colinapp.Domain.Entities.System;
using Colinapp.Domain.Enums;
using Colinapp.Shared.Exceptions;
using Colinapp.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Colinapp.Application.Scheduling;

// ---------- DTO ----------

public class ScheduledJobQuery : PagedRequest
{
    public JobStatus? Status { get; set; }
}

public class ScheduledJobDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string JobGroup { get; set; } = "DEFAULT";
    public string InvokeTarget { get; set; } = string.Empty;
    public string CronExpression { get; set; } = string.Empty;
    public string? JobData { get; set; }
    public JobStatus Status { get; set; }
    public bool Concurrent { get; set; }
    public string? Remark { get; set; }
    public DateTime CreatedTime { get; set; }
}

public class ScheduledJobSaveDto
{
    public string Name { get; set; } = string.Empty;
    public string JobGroup { get; set; } = "DEFAULT";
    public string InvokeTarget { get; set; } = string.Empty;
    public string CronExpression { get; set; } = string.Empty;
    public string? JobData { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Paused;
    public bool Concurrent { get; set; }
    public string? Remark { get; set; }
}

public class JobLogQuery : PagedRequest
{
    public bool? Success { get; set; }
}

public class JobLogDto
{
    public long Id { get; set; }
    public long JobId { get; set; }
    public string JobName { get; set; } = string.Empty;
    public string JobGroup { get; set; } = "DEFAULT";
    public string InvokeTarget { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Exception { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public long ElapsedMs { get; set; }
}

// ---------- Service ----------

public interface IScheduledJobService
{
    Task<PagedResult<ScheduledJobDto>> GetPagedAsync(ScheduledJobQuery query, CancellationToken ct = default);
    Task<ScheduledJobDto> GetAsync(long id, CancellationToken ct = default);
    Task<long> CreateAsync(ScheduledJobSaveDto dto, CancellationToken ct = default);
    Task UpdateAsync(long id, ScheduledJobSaveDto dto, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);

    /// <summary>切换状态：running=恢复调度，paused=暂停。</summary>
    Task ChangeStatusAsync(long id, JobStatus status, CancellationToken ct = default);

    /// <summary>立即执行一次。</summary>
    Task RunOnceAsync(long id, CancellationToken ct = default);

    /// <summary>可选调用目标（内置任务清单）。</summary>
    IReadOnlyList<JobDescriptor> GetAvailableTargets();

    Task<PagedResult<JobLogDto>> GetLogsAsync(JobLogQuery query, CancellationToken ct = default);
    Task ClearLogsAsync(CancellationToken ct = default);
}

public class ScheduledJobService(IAppDbContext db, IJobScheduler scheduler, IJobRegistry registry) : IScheduledJobService
{
    public async Task<PagedResult<ScheduledJobDto>> GetPagedAsync(ScheduledJobQuery query, CancellationToken ct = default)
    {
        var q = db.ScheduledJobs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(x => x.Name.Contains(query.Keyword) || x.InvokeTarget.Contains(query.Keyword));
        if (query.Status is { } s)
            q = q.Where(x => x.Status == s);

        var total = await q.CountAsync(ct);
        var entities = await q
            .OrderByDescending(x => x.Id)
            .Skip(query.Skip).Take(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<ScheduledJobDto>(entities.Select(ToDto).ToList(), total, query.PageIndex, query.PageSize);
    }

    public async Task<ScheduledJobDto> GetAsync(long id, CancellationToken ct = default)
        => ToDto(await FindAsync(id, ct));

    public async Task<long> CreateAsync(ScheduledJobSaveDto dto, CancellationToken ct = default)
    {
        Validate(dto);
        var entity = Apply(new ScheduledJob(), dto);
        db.ScheduledJobs.Add(entity);
        await db.SaveChangesAsync(ct);

        await scheduler.ScheduleAsync(entity, ct);
        return entity.Id;
    }

    public async Task UpdateAsync(long id, ScheduledJobSaveDto dto, CancellationToken ct = default)
    {
        Validate(dto);
        var entity = await FindAsync(id, ct);
        Apply(entity, dto);
        await db.SaveChangesAsync(ct);

        // 重建调度以反映新的 Cron/目标/状态。
        await scheduler.ScheduleAsync(entity, ct);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var entity = await FindAsync(id, ct);
        await scheduler.UnscheduleAsync(entity, ct);
        db.ScheduledJobs.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task ChangeStatusAsync(long id, JobStatus status, CancellationToken ct = default)
    {
        var entity = await FindAsync(id, ct);
        if (entity.Status == status) return;

        entity.Status = status;
        await db.SaveChangesAsync(ct);

        if (status == JobStatus.Running)
            await scheduler.ResumeAsync(entity, ct);
        else
            await scheduler.PauseAsync(entity, ct);
    }

    public async Task RunOnceAsync(long id, CancellationToken ct = default)
    {
        var entity = await FindAsync(id, ct);
        await scheduler.TriggerAsync(entity, ct);
    }

    public IReadOnlyList<JobDescriptor> GetAvailableTargets() => registry.GetDescriptors();

    public async Task<PagedResult<JobLogDto>> GetLogsAsync(JobLogQuery query, CancellationToken ct = default)
    {
        var q = db.ScheduledJobLogs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(x => x.JobName.Contains(query.Keyword) || x.InvokeTarget.Contains(query.Keyword));
        if (query.Success is { } s)
            q = q.Where(x => x.Success == s);

        var total = await q.CountAsync(ct);
        var entities = await q
            .OrderByDescending(x => x.Id)
            .Skip(query.Skip).Take(query.PageSize)
            .ToListAsync(ct);

        var items = entities.Select(x => new JobLogDto
        {
            Id = x.Id,
            JobId = x.JobId,
            JobName = x.JobName,
            JobGroup = x.JobGroup,
            InvokeTarget = x.InvokeTarget,
            Success = x.Success,
            Message = x.Message,
            Exception = x.Exception,
            StartTime = x.StartTime,
            EndTime = x.EndTime,
            ElapsedMs = x.ElapsedMs,
        }).ToList();

        return new PagedResult<JobLogDto>(items, total, query.PageIndex, query.PageSize);
    }

    // 执行日志为追加型，清空用物理删除（绕过软删除拦截器）。
    public async Task ClearLogsAsync(CancellationToken ct = default)
        => await db.ScheduledJobLogs.IgnoreQueryFilters().ExecuteDeleteAsync(ct);

    private async Task<ScheduledJob> FindAsync(long id, CancellationToken ct)
        => await db.ScheduledJobs.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("定时任务不存在");

    private void Validate(ScheduledJobSaveDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new BusinessException("任务名称不能为空");
        if (registry.Find(dto.InvokeTarget) is null)
            throw new BusinessException($"调用目标无效：{dto.InvokeTarget}");
        if (!scheduler.ValidateCron(dto.CronExpression))
            throw new BusinessException($"Cron 表达式不合法：{dto.CronExpression}");
    }

    private static ScheduledJob Apply(ScheduledJob e, ScheduledJobSaveDto dto)
    {
        e.Name = dto.Name;
        e.JobGroup = string.IsNullOrWhiteSpace(dto.JobGroup) ? "DEFAULT" : dto.JobGroup;
        e.InvokeTarget = dto.InvokeTarget;
        e.CronExpression = dto.CronExpression;
        e.JobData = dto.JobData;
        e.Status = dto.Status;
        e.Concurrent = dto.Concurrent;
        e.Remark = dto.Remark;
        return e;
    }

    private static ScheduledJobDto ToDto(ScheduledJob x) => new()
    {
        Id = x.Id,
        Name = x.Name,
        JobGroup = x.JobGroup,
        InvokeTarget = x.InvokeTarget,
        CronExpression = x.CronExpression,
        JobData = x.JobData,
        Status = x.Status,
        Concurrent = x.Concurrent,
        Remark = x.Remark,
        CreatedTime = x.CreatedTime,
    };
}
