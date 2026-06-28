using Colinapp.Application.Common;
using Colinapp.Application.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Colinapp.Infrastructure.Scheduling;

/// <summary>
/// 启动加载器：宿主启动时把数据库 sys_job 中的任务装入 Quartz。
/// 注册顺序在 Quartz 托管服务之后，故调度器已就绪；迁移已由 DbInitializer 在 app.Run 前完成。
/// </summary>
public class JobBootstrapper(IServiceProvider services, ILogger<JobBootstrapper> logger) : IHostedService
{
    public async Task StartAsync(CancellationToken ct)
    {
        try
        {
            using var scope = services.CreateScope();
            var sp = scope.ServiceProvider;
            var db = sp.GetRequiredService<IAppDbContext>();
            var scheduler = sp.GetRequiredService<IJobScheduler>();

            var jobs = await db.ScheduledJobs.AsNoTracking().ToListAsync(ct);
            foreach (var job in jobs)
            {
                try
                {
                    await scheduler.ScheduleAsync(job, ct);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "装载定时任务失败：{Job}（{Target}）", job.Name, job.InvokeTarget);
                }
            }

            logger.LogInformation("定时任务装载完成，共 {Count} 个", jobs.Count);
        }
        catch (Exception ex)
        {
            // 表不存在/库未就绪等不应拖垮启动。
            logger.LogError(ex, "定时任务装载阶段异常，已跳过");
        }
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
