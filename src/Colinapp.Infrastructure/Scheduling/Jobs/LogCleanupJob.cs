using Colinapp.Application.Common;
using Microsoft.EntityFrameworkCore;
using Quartz;

namespace Colinapp.Infrastructure.Scheduling.Jobs;

/// <summary>
/// 日志清理任务：物理删除超过保留天数的操作日志与登录日志。
/// 保留天数取任务参数（JobData，整数），缺省 30 天。
/// </summary>
[DisallowConcurrentExecution]
public class LogCleanupJob(IAppDbContext db) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var days = ParseDays(context.MergedJobDataMap.GetString("jobData"));
        var threshold = DateTime.Now.AddDays(-days);

        var ct = context.CancellationToken;
        var opLogs = await db.OperationLogs.IgnoreQueryFilters()
            .Where(x => x.CreatedTime < threshold).ExecuteDeleteAsync(ct);
        var loginLogs = await db.LoginLogs.IgnoreQueryFilters()
            .Where(x => x.CreatedTime < threshold).ExecuteDeleteAsync(ct);

        context.Result = $"保留 {days} 天，清理操作日志 {opLogs} 条、登录日志 {loginLogs} 条";
    }

    private static int ParseDays(string? raw)
        => int.TryParse(raw, out var d) && d > 0 ? d : 30;
}
