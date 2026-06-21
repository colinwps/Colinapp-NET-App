namespace Colinapp.Shared.Models;

/// <summary>
/// 分页结果。
/// </summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];

    public int Total { get; set; }

    public int PageIndex { get; set; }

    public int PageSize { get; set; }

    public PagedResult() { }

    public PagedResult(IReadOnlyList<T> items, int total, int pageIndex, int pageSize)
    {
        Items = items;
        Total = total;
        PageIndex = pageIndex;
        PageSize = pageSize;
    }
}
