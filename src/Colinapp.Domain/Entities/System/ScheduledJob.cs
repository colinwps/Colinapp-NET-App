using Colinapp.Domain.Enums;

namespace Colinapp.Domain.Entities.System;

/// <summary>
/// 定时任务定义（sys_job）。一行对应一个 Cron 调度，InvokeTarget 指向一个已注册的内置任务键。
/// </summary>
public class ScheduledJob : EntityBase
{
    /// <summary>任务名称</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>任务分组（Quartz Group，默认 DEFAULT）</summary>
    public string JobGroup { get; set; } = "DEFAULT";

    /// <summary>调用目标：已注册任务的键（见 IJobRegistry），如 logCleanup、refreshTokenCleanup。</summary>
    public string InvokeTarget { get; set; } = string.Empty;

    /// <summary>Cron 表达式（Quartz 格式，含秒位，如 "0 0 3 * * ?"）</summary>
    public string CronExpression { get; set; } = string.Empty;

    /// <summary>任务参数（可选，由具体任务解释，如日志保留天数）</summary>
    public string? JobData { get; set; }

    /// <summary>状态：正常/暂停</summary>
    public JobStatus Status { get; set; } = JobStatus.Paused;

    /// <summary>是否允许并发执行（保留位，内置清理任务默认禁止并发）</summary>
    public bool Concurrent { get; set; }

    /// <summary>备注</summary>
    public string? Remark { get; set; }
}

/// <summary>
/// 定时任务执行日志（sys_job_log）。每次触发记录一条，由全局任务监听器写入。
/// 追加型数据，清空用物理删除。
/// </summary>
public class ScheduledJobLog : EntityBase
{
    /// <summary>任务 Id（关联 sys_job，可能已删除，故不建外键）</summary>
    public long JobId { get; set; }

    /// <summary>任务名称（执行时快照）</summary>
    public string JobName { get; set; } = string.Empty;

    /// <summary>任务分组</summary>
    public string JobGroup { get; set; } = "DEFAULT";

    /// <summary>调用目标键</summary>
    public string InvokeTarget { get; set; } = string.Empty;

    /// <summary>是否成功</summary>
    public bool Success { get; set; }

    /// <summary>执行信息（任务自报或「执行成功」）</summary>
    public string? Message { get; set; }

    /// <summary>异常堆栈（失败时）</summary>
    public string? Exception { get; set; }

    /// <summary>开始时间</summary>
    public DateTime StartTime { get; set; }

    /// <summary>结束时间</summary>
    public DateTime EndTime { get; set; }

    /// <summary>耗时（毫秒）</summary>
    public long ElapsedMs { get; set; }
}
