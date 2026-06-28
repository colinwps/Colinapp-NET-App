using Colinapp.Application.Scheduling;
using Colinapp.Domain.Entities.System;
using Colinapp.Domain.Enums;
using Quartz;

namespace Colinapp.Infrastructure.Scheduling;

/// <summary>
/// 基于 Quartz 的调度器实现。任务键用 sys_job.Id（分组用 JobGroup），JobDetail 设为持久化（durable），
/// 暂停任务仍保留定义以支持「立即执行」。所有操作对未知任务键安全跳过。
/// </summary>
public class QuartzJobScheduler(ISchedulerFactory factory, JobRegistry registry) : IJobScheduler
{
    public bool ValidateCron(string cron) => CronExpression.IsValidExpression(cron);

    public async Task ScheduleAsync(ScheduledJob job, CancellationToken ct = default)
    {
        var type = registry.ResolveType(job.InvokeTarget);
        if (type is null) return;

        var scheduler = await factory.GetScheduler(ct);
        var jobKey = JobKeyOf(job);

        // 幂等：先删旧定义，再按当前配置重建。
        await scheduler.DeleteJob(jobKey, ct);

        var detail = JobBuilder.Create(type)
            .WithIdentity(jobKey)
            .StoreDurably()
            .UsingJobData("jobId", job.Id)
            .UsingJobData("jobName", job.Name)
            .UsingJobData("jobGroup", job.JobGroup)
            .UsingJobData("invokeTarget", job.InvokeTarget)
            .UsingJobData("jobData", job.JobData ?? string.Empty)
            .Build();

        var trigger = TriggerBuilder.Create()
            .WithIdentity(TriggerKeyOf(job))
            .ForJob(jobKey)
            .WithCronSchedule(job.CronExpression, x => x.WithMisfireHandlingInstructionDoNothing())
            .Build();

        await scheduler.ScheduleJob(detail, trigger, ct);

        if (job.Status == JobStatus.Paused)
            await scheduler.PauseJob(jobKey, ct);
    }

    public async Task UnscheduleAsync(ScheduledJob job, CancellationToken ct = default)
    {
        var scheduler = await factory.GetScheduler(ct);
        await scheduler.DeleteJob(JobKeyOf(job), ct);
    }

    public async Task PauseAsync(ScheduledJob job, CancellationToken ct = default)
    {
        var scheduler = await factory.GetScheduler(ct);
        await scheduler.PauseJob(JobKeyOf(job), ct);
    }

    public async Task ResumeAsync(ScheduledJob job, CancellationToken ct = default)
    {
        var scheduler = await factory.GetScheduler(ct);
        var jobKey = JobKeyOf(job);
        // 若调度器中尚无该任务（如启动时为暂停而未建），先建好再恢复。
        if (!await scheduler.CheckExists(jobKey, ct))
        {
            await ScheduleAsync(job, ct);
            return;
        }
        await scheduler.ResumeJob(jobKey, ct);
    }

    public async Task TriggerAsync(ScheduledJob job, CancellationToken ct = default)
    {
        var scheduler = await factory.GetScheduler(ct);
        var jobKey = JobKeyOf(job);
        // 立即执行需要 JobDetail 存在；缺失则先登记（暂停态也能手动跑一次）。
        if (!await scheduler.CheckExists(jobKey, ct))
            await ScheduleAsync(job, ct);

        await scheduler.TriggerJob(jobKey, ct);
    }

    private static JobKey JobKeyOf(ScheduledJob job) => new(job.Id.ToString(), job.JobGroup);
    private static TriggerKey TriggerKeyOf(ScheduledJob job) => new(job.Id.ToString(), job.JobGroup);
}

/// <summary>Quartz:Enabled=false 时的空实现：CRUD 持久化照常，仅不真正调度。</summary>
public class NoOpJobScheduler(JobRegistry registry) : IJobScheduler
{
    public bool ValidateCron(string cron) => CronExpression.IsValidExpression(cron);
    public Task ScheduleAsync(ScheduledJob job, CancellationToken ct = default) => Task.CompletedTask;
    public Task UnscheduleAsync(ScheduledJob job, CancellationToken ct = default) => Task.CompletedTask;
    public Task PauseAsync(ScheduledJob job, CancellationToken ct = default) => Task.CompletedTask;
    public Task ResumeAsync(ScheduledJob job, CancellationToken ct = default) => Task.CompletedTask;
    public Task TriggerAsync(ScheduledJob job, CancellationToken ct = default) => Task.CompletedTask;

    // registry 保留注入以确保 DI 图一致（available targets 仍可用）。
    public JobRegistry Registry => registry;
}
