namespace Colinapp.Application.Permissions;

/// <summary>
/// 数据范围服务：根据当前登录用户的角色数据范围，计算其可访问的部门集合。
/// </summary>
public interface IDataScopeService
{
    /// <summary>解析当前用户的数据范围。</summary>
    Task<DataScopeResult> ResolveAsync(CancellationToken ct = default);
}
