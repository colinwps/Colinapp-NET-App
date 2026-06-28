using Colinapp.Application.Scheduling;
using Colinapp.Domain.Entities.System;
using Colinapp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace Colinapp.Infrastructure.Persistence;

/// <summary>
/// 定时任务种子。<b>幂等</b>：每次启动按内置任务键补齐 sys_job 行（按 InvokeTarget 判重）。
/// 默认以「暂停」状态写入，避免升级后意外触发；管理员在后台启用即可。
/// </summary>
internal static class JobSeeder
{
    public static async Task SeedAsync(AppDbContext db, IJobRegistry registry, CancellationToken ct = default)
    {
        var changed = false;
        foreach (var d in registry.GetDescriptors())
        {
            if (await db.ScheduledJobs.AnyAsync(x => x.InvokeTarget == d.Key, ct))
                continue;

            db.ScheduledJobs.Add(new ScheduledJob
            {
                Name = d.Name,
                JobGroup = "DEFAULT",
                InvokeTarget = d.Key,
                CronExpression = d.DefaultCron,
                JobData = d.DefaultJobData,
                Status = JobStatus.Paused,
                Remark = d.Description,
            });
            changed = true;
        }

        if (changed)
            await db.SaveChangesAsync(ct);
    }
}
