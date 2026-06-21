namespace Colinapp.Domain.Entities;

/// <summary>主键标记接口。</summary>
public interface IEntity
{
    long Id { get; }
}

/// <summary>软删除标记接口，配合全局查询过滤器自动过滤已删除数据。</summary>
public interface ISoftDelete
{
    bool IsDeleted { get; set; }
}

/// <summary>多租户标记接口，配合全局查询过滤器实现租户隔离（初期可关闭）。</summary>
public interface ITenant
{
    long? TenantId { get; set; }
}
