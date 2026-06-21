namespace Colinapp.Infrastructure.Multitenancy;

/// <summary>
/// 多租户配置（绑定 appsettings 的 "MultiTenancy" 节）。
/// 初期 Enabled = false，仅预留字段与过滤器，不实际隔离数据。
/// </summary>
public class TenantOptions
{
    public const string SectionName = "MultiTenancy";

    /// <summary>是否启用租户数据隔离</summary>
    public bool Enabled { get; set; }
}
