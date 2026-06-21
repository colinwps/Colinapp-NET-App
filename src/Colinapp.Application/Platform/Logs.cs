using Colinapp.Application.Common;
using Colinapp.Domain.Entities.System;
using Colinapp.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace Colinapp.Application.Platform;

// ---------- DTO ----------

public class OperationLogQuery : PagedRequest
{
    public bool? Success { get; set; }
}

public class OperationLogDto
{
    public long Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? OperatorName { get; set; }
    public string? Method { get; set; }
    public string RequestMethod { get; set; } = string.Empty;
    public string RequestUrl { get; set; } = string.Empty;
    public string? RequestParams { get; set; }
    public string? Ip { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public long Duration { get; set; }
    public DateTime CreatedTime { get; set; }
}

public class LoginLogQuery : PagedRequest
{
    public bool? Success { get; set; }
}

public class LoginLogDto
{
    public long Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? Message { get; set; }
    public string? Ip { get; set; }
    public string? UserAgent { get; set; }
    public DateTime CreatedTime { get; set; }
}

// ---------- Service ----------

public interface ILogService
{
    Task AddOperationLogAsync(OperationLog log, CancellationToken ct = default);
    Task AddLoginLogAsync(string userName, bool success, string? message, string? ip, string? userAgent, CancellationToken ct = default);

    Task<PagedResult<OperationLogDto>> GetOperationLogsAsync(OperationLogQuery query, CancellationToken ct = default);
    Task<PagedResult<LoginLogDto>> GetLoginLogsAsync(LoginLogQuery query, CancellationToken ct = default);

    Task ClearOperationLogsAsync(CancellationToken ct = default);
    Task ClearLoginLogsAsync(CancellationToken ct = default);
}

public class LogService(IAppDbContext db) : ILogService
{
    public async Task AddOperationLogAsync(OperationLog log, CancellationToken ct = default)
    {
        db.OperationLogs.Add(log);
        await db.SaveChangesAsync(ct);
    }

    public async Task AddLoginLogAsync(
        string userName, bool success, string? message, string? ip, string? userAgent, CancellationToken ct = default)
    {
        db.LoginLogs.Add(new LoginLog
        {
            UserName = userName,
            Success = success,
            Message = message,
            Ip = ip,
            UserAgent = userAgent,
        });
        await db.SaveChangesAsync(ct);
    }

    public async Task<PagedResult<OperationLogDto>> GetOperationLogsAsync(OperationLogQuery query, CancellationToken ct = default)
    {
        var q = db.OperationLogs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(x => x.Title.Contains(query.Keyword) || (x.OperatorName != null && x.OperatorName.Contains(query.Keyword)));
        if (query.Success is { } s)
            q = q.Where(x => x.Success == s);

        var total = await q.CountAsync(ct);
        var entities = await q
            .OrderByDescending(x => x.Id)
            .Skip(query.Skip).Take(query.PageSize)
            .ToListAsync(ct);

        var items = entities.Select(x => new OperationLogDto
        {
            Id = x.Id,
            Title = x.Title,
            OperatorName = x.OperatorName,
            Method = x.Method,
            RequestMethod = x.RequestMethod,
            RequestUrl = x.RequestUrl,
            RequestParams = x.RequestParams,
            Ip = x.Ip,
            Success = x.Success,
            ErrorMessage = x.ErrorMessage,
            Duration = x.Duration,
            CreatedTime = x.CreatedTime,
        }).ToList();

        return new PagedResult<OperationLogDto>(items, total, query.PageIndex, query.PageSize);
    }

    public async Task<PagedResult<LoginLogDto>> GetLoginLogsAsync(LoginLogQuery query, CancellationToken ct = default)
    {
        var q = db.LoginLogs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(x => x.UserName.Contains(query.Keyword));
        if (query.Success is { } s)
            q = q.Where(x => x.Success == s);

        var total = await q.CountAsync(ct);
        var entities = await q
            .OrderByDescending(x => x.Id)
            .Skip(query.Skip).Take(query.PageSize)
            .ToListAsync(ct);

        var items = entities.Select(x => new LoginLogDto
        {
            Id = x.Id,
            UserName = x.UserName,
            Success = x.Success,
            Message = x.Message,
            Ip = x.Ip,
            UserAgent = x.UserAgent,
            CreatedTime = x.CreatedTime,
        }).ToList();

        return new PagedResult<LoginLogDto>(items, total, query.PageIndex, query.PageSize);
    }

    // 日志为追加型数据，清空用物理删除（绕过软删除拦截器）。
    public async Task ClearOperationLogsAsync(CancellationToken ct = default)
        => await db.OperationLogs.IgnoreQueryFilters().ExecuteDeleteAsync(ct);

    public async Task ClearLoginLogsAsync(CancellationToken ct = default)
        => await db.LoginLogs.IgnoreQueryFilters().ExecuteDeleteAsync(ct);
}
