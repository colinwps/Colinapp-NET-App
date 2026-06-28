using Colinapp.Domain.Entities.System;

namespace Colinapp.Application.Scheduling;

/// <summary>
/// 可调度任务的元信息。由基础设施层登记内置任务，供前端下拉选择「调用目标」。
/// </summary>
public sealed record JobDescriptor(string Key, string Name, string DefaultCron, string? DefaultJobData, string Description);

/// <summary>
/// 内置任务注册表。Application 通过它枚举可用任务键，无需感知具体 Quartz 任务类型。
/// </summary>
public interface IJobRegistry
{
    IReadOnlyList<JobDescriptor> GetDescriptors();
    JobDescriptor? Find(string key);
}

/// <summary>
/// 调度器抽象（由 Infrastructure 的 Quartz 实现）。Application 仅依赖此接口操作运行时调度，
/// 不直接引用 Quartz。所有方法对「未知任务键 / 调度器未就绪」需安全降级。
/// </summary>
public interface IJobScheduler
{
    /// <summary>校验 Cron 表达式是否合法（Quartz 格式，含秒位）。</summary>
    bool ValidateCron(string cron);

    /// <summary>登记/重建该任务的调度（幂等：先删后建）。Status=Paused 时建好即挂起。</summary>
    Task ScheduleAsync(ScheduledJob job, CancellationToken ct = default);

    /// <summary>移除该任务的调度。</summary>
    Task UnscheduleAsync(ScheduledJob job, CancellationToken ct = default);

    /// <summary>暂停触发。</summary>
    Task PauseAsync(ScheduledJob job, CancellationToken ct = default);

    /// <summary>恢复触发。</summary>
    Task ResumeAsync(ScheduledJob job, CancellationToken ct = default);

    /// <summary>立即执行一次（不影响既有 Cron 计划）。</summary>
    Task TriggerAsync(ScheduledJob job, CancellationToken ct = default);
}
