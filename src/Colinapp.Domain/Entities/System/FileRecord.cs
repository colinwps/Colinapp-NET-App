namespace Colinapp.Domain.Entities.System;

/// <summary>
/// 文件记录。保存上传文件的元数据，物理文件由 IFileStorage 落地（本地/对象存储）。
/// </summary>
public class FileRecord : EntityBase
{
    /// <summary>原始文件名（含扩展名）</summary>
    public string FileName { get; set; } = string.Empty;

    /// <summary>存储文件名（随机生成，避免冲突与路径穿越）</summary>
    public string StoredName { get; set; } = string.Empty;

    /// <summary>相对存储根的路径，如 2026/06/28/{guid}.png（正斜杠）</summary>
    public string RelativePath { get; set; } = string.Empty;

    /// <summary>可访问 URL，如 /uploads/2026/06/28/{guid}.png</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>MIME 类型</summary>
    public string? ContentType { get; set; }

    /// <summary>文件大小（字节）</summary>
    public long Size { get; set; }

    /// <summary>扩展名（小写，不含点，如 png）</summary>
    public string? Ext { get; set; }

    /// <summary>业务分类标签（可选，便于按用途归类，如 avatar / notice）</summary>
    public string? BizType { get; set; }

    /// <summary>存储类型：local / oss / minio …</summary>
    public string StorageType { get; set; } = "local";
}
