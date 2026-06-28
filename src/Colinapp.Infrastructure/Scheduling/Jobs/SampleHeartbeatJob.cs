using Microsoft.Extensions.Logging;
using Quartz;

namespace Colinapp.Infrastructure.Scheduling.Jobs;

/// <summary>
/// 示例心跳任务：仅写一条日志，作为自定义任务的最小模板。
/// 复制本类、在 JobRegistry 登记一行即得到一个可在后台管理的新任务。
/// </summary>
[DisallowConcurrentExecution]
public class SampleHeartbeatJob(ILogger<SampleHeartbeatJob> logger) : IJob
{
    public Task Execute(IJobExecutionContext context)
    {
        var ts = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        logger.LogInformation("示例心跳任务执行 @ {Timestamp}", ts);
        context.Result = $"心跳 OK @ {ts}";
        return Task.CompletedTask;
    }
}
