namespace Colinapp.Application.Storage;

/// <summary>物理文件落地结果。</summary>
public record StoredFileInfo(string StoredName, string RelativePath, string Url, long Size);

/// <summary>
/// 文件存储抽象。本地存储起步，后续可替换为对象存储（OSS/MinIO）而不改动业务代码。
/// </summary>
public interface IFileStorage
{
    /// <summary>存储类型标识（local / oss …）。</summary>
    string StorageType { get; }

    /// <summary>保存文件流，返回落地信息（存储名、相对路径、访问 URL、大小）。</summary>
    Task<StoredFileInfo> SaveAsync(Stream content, string originalFileName, CancellationToken ct = default);

    /// <summary>按相对路径打开读取流，不存在返回 null。</summary>
    Task<Stream?> OpenReadAsync(string relativePath, CancellationToken ct = default);

    /// <summary>按相对路径删除物理文件（不存在则忽略）。</summary>
    Task DeleteAsync(string relativePath, CancellationToken ct = default);
}
