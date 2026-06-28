namespace Colinapp.Application.Storage;

/// <summary>
/// 文件存储配置（绑定 appsettings 的 "FileStorage" 节）。
/// </summary>
public class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>存储根目录。相对路径按运行目录解析（与 Serilog 的 logs/ 约定一致）。</summary>
    public string RootPath { get; set; } = "uploads";

    /// <summary>静态访问前缀，物理文件经此前缀对外暴露。</summary>
    public string RequestPath { get; set; } = "/uploads";

    /// <summary>单文件大小上限（字节），默认 50MB。</summary>
    public long MaxSizeBytes { get; set; } = 50 * 1024 * 1024;

    /// <summary>允许的扩展名白名单（小写不含点）。为空表示不限制。</summary>
    public string[] AllowedExtensions { get; set; } = [];

    /// <summary>解析存储根的绝对路径。</summary>
    public string ResolveRootPath()
        => Path.IsPathRooted(RootPath) ? RootPath : Path.Combine(Directory.GetCurrentDirectory(), RootPath);
}
