using Colinapp.Application.Common;
using Colinapp.Domain.Entities.System;
using Colinapp.Shared.Exceptions;
using Colinapp.Shared.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Colinapp.Application.Storage;

// ---------- DTO ----------

public class FileQuery : PagedRequest
{
    public string? BizType { get; set; }
}

public class FileRecordDto
{
    public long Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? ContentType { get; set; }
    public long Size { get; set; }
    public string? Ext { get; set; }
    public string? BizType { get; set; }
    public string StorageType { get; set; } = string.Empty;
    public DateTime CreatedTime { get; set; }
}

/// <summary>上传请求载荷（流 + 元数据），由控制器从 IFormFile 拆解传入，使服务层不依赖 ASP.NET。</summary>
public class FileUploadInput
{
    public required Stream Content { get; init; }
    public required string FileName { get; init; }
    public string? ContentType { get; init; }
    public long Size { get; init; }
    public string? BizType { get; init; }
}

// ---------- Service ----------

public interface IFileService
{
    Task<FileRecordDto> UploadAsync(FileUploadInput input, CancellationToken ct = default);
    Task<PagedResult<FileRecordDto>> GetPagedAsync(FileQuery query, CancellationToken ct = default);
    Task DeleteAsync(long id, CancellationToken ct = default);
}

public class FileService(
    IAppDbContext db,
    IFileStorage storage,
    IOptions<FileStorageOptions> options) : IFileService
{
    private readonly FileStorageOptions _options = options.Value;

    public async Task<FileRecordDto> UploadAsync(FileUploadInput input, CancellationToken ct = default)
    {
        if (input.Size <= 0)
            throw new BusinessException("文件内容为空");
        if (input.Size > _options.MaxSizeBytes)
            throw new BusinessException($"文件超过大小上限 {_options.MaxSizeBytes / 1024 / 1024}MB");

        var ext = Path.GetExtension(input.FileName).TrimStart('.').ToLowerInvariant();
        if (_options.AllowedExtensions.Length > 0 &&
            (ext.Length == 0 || !_options.AllowedExtensions.Contains(ext)))
            throw new BusinessException($"不支持的文件类型：.{ext}");

        var stored = await storage.SaveAsync(input.Content, input.FileName, ct);

        var record = new FileRecord
        {
            FileName = input.FileName,
            StoredName = stored.StoredName,
            RelativePath = stored.RelativePath,
            Url = stored.Url,
            ContentType = input.ContentType,
            Size = stored.Size,
            Ext = ext.Length == 0 ? null : ext,
            BizType = input.BizType,
            StorageType = storage.StorageType,
        };
        db.Files.Add(record);
        await db.SaveChangesAsync(ct);

        return ToDto(record);
    }

    public async Task<PagedResult<FileRecordDto>> GetPagedAsync(FileQuery query, CancellationToken ct = default)
    {
        var q = db.Files.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Keyword))
            q = q.Where(x => x.FileName.Contains(query.Keyword));
        if (!string.IsNullOrWhiteSpace(query.BizType))
            q = q.Where(x => x.BizType == query.BizType);

        var total = await q.CountAsync(ct);
        var records = await q
            .OrderByDescending(x => x.Id)
            .Skip(query.Skip).Take(query.PageSize)
            .ToListAsync(ct);

        var items = records.Select(ToDto).ToList();
        return new PagedResult<FileRecordDto>(items, total, query.PageIndex, query.PageSize);
    }

    public async Task DeleteAsync(long id, CancellationToken ct = default)
    {
        var record = await db.Files.FirstOrDefaultAsync(x => x.Id == id, ct)
            ?? throw BusinessException.NotFound("文件不存在");

        await storage.DeleteAsync(record.RelativePath, ct);
        db.Files.Remove(record);
        await db.SaveChangesAsync(ct);
    }

    private static FileRecordDto ToDto(FileRecord x) => new()
    {
        Id = x.Id,
        FileName = x.FileName,
        Url = x.Url,
        ContentType = x.ContentType,
        Size = x.Size,
        Ext = x.Ext,
        BizType = x.BizType,
        StorageType = x.StorageType,
        CreatedTime = x.CreatedTime,
    };
}
