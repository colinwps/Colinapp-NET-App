using Colinapp.Application.Storage;
using Microsoft.Extensions.Options;

namespace Colinapp.Infrastructure.Storage;

/// <summary>
/// 本地磁盘文件存储。按 yyyy/MM/dd 分目录，随机文件名避免冲突与路径穿越。
/// </summary>
public class LocalFileStorage(IOptions<FileStorageOptions> options) : IFileStorage
{
    private readonly FileStorageOptions _options = options.Value;

    public string StorageType => "local";

    public async Task<StoredFileInfo> SaveAsync(Stream content, string originalFileName, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(originalFileName);
        var storedName = $"{Guid.NewGuid():N}{ext}";
        var subDir = DateTime.Now.ToString("yyyy/MM/dd");

        var root = _options.ResolveRootPath();
        var dir = Path.Combine(root, subDir.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(dir);

        var fullPath = Path.Combine(dir, storedName);
        await using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            await content.CopyToAsync(fs, ct);
        }

        var relativePath = $"{subDir}/{storedName}";
        var url = $"{_options.RequestPath.TrimEnd('/')}/{relativePath}";
        var size = new FileInfo(fullPath).Length;
        return new StoredFileInfo(storedName, relativePath, url, size);
    }

    public Task<Stream?> OpenReadAsync(string relativePath, CancellationToken ct = default)
    {
        var fullPath = ToFullPath(relativePath);
        if (fullPath is null || !File.Exists(fullPath))
            return Task.FromResult<Stream?>(null);

        Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string relativePath, CancellationToken ct = default)
    {
        var fullPath = ToFullPath(relativePath);
        if (fullPath is not null && File.Exists(fullPath))
            File.Delete(fullPath);
        return Task.CompletedTask;
    }

    /// <summary>将相对路径解析为根目录内的绝对路径，越界（路径穿越）返回 null。</summary>
    private string? ToFullPath(string relativePath)
    {
        var root = Path.GetFullPath(_options.ResolveRootPath());
        var combined = Path.GetFullPath(Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar)));
        return combined.StartsWith(root, StringComparison.Ordinal) ? combined : null;
    }
}
