namespace Colinapp.Domain.Enums;

/// <summary>
/// 定时任务状态。Running=正常调度，Paused=暂停（任务定义保留，触发器挂起）。
/// </summary>
public enum JobStatus
{
    /// <summary>正常（按 Cron 调度）</summary>
    Running = 0,

    /// <summary>暂停（不触发，可随时恢复）</summary>
    Paused = 1,
}
