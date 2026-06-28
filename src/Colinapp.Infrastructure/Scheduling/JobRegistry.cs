using Colinapp.Application.Scheduling;
using Colinapp.Infrastructure.Scheduling.Jobs;
using Quartz;

namespace Colinapp.Infrastructure.Scheduling;

/// <summary>
/// 内置任务注册表。新增一个内置任务 = 在这里登记一行（键 → 类型 + 元信息）。
/// 数据库 sys_job.InvokeTarget 存键，调度器据此解析为 Quartz 任务类型。
/// </summary>
public sealed class JobRegistry : IJobRegistry
{
    private sealed record Entry(JobDescriptor Descriptor, Type JobType);

    private readonly IReadOnlyList<Entry> _entries =
    [
        new(new JobDescriptor(
            "logCleanup", "日志清理", "0 0 3 * * ?", "30",
            "清理超过保留天数（任务参数=天数，默认 30）的操作日志与登录日志"),
            typeof(LogCleanupJob)),

        new(new JobDescriptor(
            "refreshTokenCleanup", "过期令牌清理", "0 30 3 * * ?", null,
            "清理已过期或已撤销的刷新令牌（sys_refresh_token）"),
            typeof(RefreshTokenCleanupJob)),

        new(new JobDescriptor(
            "sampleHeartbeat", "示例心跳", "0/30 * * * * ?", null,
            "演示用任务：每次执行写一条心跳日志，可作为自定义任务模板"),
            typeof(SampleHeartbeatJob)),
    ];

    public IReadOnlyList<JobDescriptor> GetDescriptors() => _entries.Select(e => e.Descriptor).ToList();

    public JobDescriptor? Find(string key)
        => _entries.FirstOrDefault(e => e.Descriptor.Key == key)?.Descriptor;

    /// <summary>解析任务键为 Quartz 任务类型；未知键返回 null。</summary>
    public Type? ResolveType(string key)
        => _entries.FirstOrDefault(e => e.Descriptor.Key == key)?.JobType;

    /// <summary>全部内置任务类型（供 DI 批量注册）。</summary>
    public IEnumerable<Type> AllJobTypes() => _entries.Select(e => e.JobType);
}
