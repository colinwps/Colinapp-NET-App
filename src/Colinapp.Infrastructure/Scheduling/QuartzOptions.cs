namespace Colinapp.Infrastructure.Scheduling;

/// <summary>
/// 定时任务配置（绑定 appsettings 的 "Quartz" 节）。
/// Enabled=false 时不启动调度器（CRUD 仍可用，仅不真正触发），便于本地关闭。
/// 命名为 SchedulingOptions 以避开 Quartz.QuartzOptions 同名冲突。
/// </summary>
public class SchedulingOptions
{
    public const string SectionName = "Quartz";

    /// <summary>是否启用调度器（默认 true）。</summary>
    public bool Enabled { get; set; } = true;
}
