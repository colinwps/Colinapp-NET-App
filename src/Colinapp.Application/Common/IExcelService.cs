namespace Colinapp.Application.Common;

/// <summary>
/// 通用 Excel 导入导出。基于 MiniExcel，按 DTO 的 [ExcelColumnName] 特性映射列名。
/// </summary>
public interface IExcelService
{
    /// <summary>将行集合导出为 xlsx 字节流。</summary>
    byte[] Export<T>(IEnumerable<T> rows, string? sheetName = null);

    /// <summary>生成仅含表头（+一空行）的导入模板。</summary>
    byte[] BuildTemplate<T>() where T : new();

    /// <summary>从 xlsx 流读取为 DTO 列表（首行为表头）。</summary>
    List<T> Read<T>(Stream stream) where T : class, new();
}
