namespace Colinapp.Domain.Entities;

/// <summary>
/// 实体基类：统一主键、审计字段、软删除、租户字段。
/// 所有业务实体继承此类，由 SaveChanges 拦截器自动填充审计与租户信息。
/// </summary>
public abstract class EntityBase : IEntity, ISoftDelete, ITenant
{
    /// <summary>主键（雪花/序列由应用生成，默认 0 表示新建时由数据库自增）。</summary>
    public long Id { get; set; }

    /// <summary>创建人 Id</summary>
    public long? CreatedBy { get; set; }

    /// <summary>创建时间</summary>
    public DateTime CreatedTime { get; set; }

    /// <summary>最后修改人 Id</summary>
    public long? UpdatedBy { get; set; }

    /// <summary>最后修改时间</summary>
    public DateTime? UpdatedTime { get; set; }

    /// <summary>软删除标记</summary>
    public bool IsDeleted { get; set; }

    /// <summary>租户 Id（多租户预留，单租户时为 null）</summary>
    public long? TenantId { get; set; }
}
