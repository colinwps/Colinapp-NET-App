using Colinapp.Application.Common;
using Colinapp.Domain.Entities.System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Colinapp.Infrastructure.Scheduling;

/// <summary>
/// 全局任务监听器：为每次任务执行写入一条 sys_job_log（成功/失败、耗时、异常）。
/// 单例，按需开作用域获取 DbContext；写日志失败不影响任务本身。
/// </summary>
public class JobExecutionListener(IServiceProvider services, ILogger<JobExecutionListener> logger) : IJobListener
{
    private const string StartKey = "__startTime";

    public string Name => "ColinappJobExecutionListener";

    public Task JobToBeExecuted(IJobExecutionContext context, CancellationToken ct = default)
    {
        context.Put(StartKey, DateTime.Now);
        return Task.CompletedTask;
    }

    public Task JobExecutionVetoed(IJobExecutionContext context, CancellationToken ct = default)
        => Task.CompletedTask;

    public async Task JobWasExecuted(
        IJobExecutionContext context, JobExecutionException? jobException, CancellationToken ct = default)
    {
        var start = context.Get(StartKey) is DateTime s ? s : DateTime.Now;
        var end = DateTime.Now;
        var map = context.MergedJobDataMap;

        var log = new ScheduledJobLog
        {
            JobId = map.GetLong("jobId"),
            JobName = map.GetString("jobName") ?? string.Empty,
            JobGroup = map.GetString("jobGroup") ?? "DEFAULT",
            InvokeTarget = map.GetString("invokeTarget") ?? string.Empty,
            Success = jobException is null,
            Message = jobException is null ? context.Result?.ToString() ?? "执行成功" : jobException.Message,
            Exception = jobException?.ToString(),
            StartTime = start,
            EndTime = end,
            ElapsedMs = (long)(end - start).TotalMilliseconds,
        };

        try
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();
            db.ScheduledJobLogs.Add(log);
            await db.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "写入定时任务执行日志失败：{Job}", log.JobName);
        }
    }
}
