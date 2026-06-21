namespace Colinapp.Shared.Models;

/// <summary>
/// 分页查询请求基类，业务查询条件可继承此类扩展。
/// </summary>
public class PagedRequest
{
    private int _pageIndex = 1;
    private int _pageSize = 20;

    /// <summary>页码，从 1 开始</summary>
    public int PageIndex
    {
        get => _pageIndex;
        set => _pageIndex = value < 1 ? 1 : value;
    }

    /// <summary>每页条数，限制最大 200</summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value is < 1 or > 200 ? 20 : value;
    }

    /// <summary>关键字（可选，业务自定义匹配字段）</summary>
    public string? Keyword { get; set; }

    public int Skip => (PageIndex - 1) * PageSize;
}
