using MiniExcelLibs;

namespace Colinapp.Application.Common;

public class MiniExcelService : IExcelService
{
    public byte[] Export<T>(IEnumerable<T> rows, string? sheetName = null)
    {
        using var ms = new MemoryStream();
        MiniExcel.SaveAs(ms, rows, sheetName: sheetName ?? "Sheet1");
        return ms.ToArray();
    }

    public byte[] BuildTemplate<T>() where T : new()
        // MiniExcel 依据首行运行时类型推断列；用一行默认实例确保写出表头。
        => Export(new List<T> { new() });

    public List<T> Read<T>(Stream stream) where T : class, new()
        => stream.Query<T>().ToList();
}
