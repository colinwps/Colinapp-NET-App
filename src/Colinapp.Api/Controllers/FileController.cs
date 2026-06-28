using Colinapp.Api.Authorization;
using Colinapp.Application.Storage;
using Colinapp.Shared.Common;
using Colinapp.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Colinapp.Api.Controllers;

/// <summary>
/// 文件管理：上传、列表、删除。物理文件经静态前缀（默认 /uploads）对外访问。
/// </summary>
[ApiController]
[Authorize]
[Route("api/file")]
public class FileController(IFileService service) : ControllerBase
{
    [HttpGet]
    [HasPermission("sys:file:list")]
    public async Task<ApiResult> List([FromQuery] FileQuery query, CancellationToken ct)
        => ApiResult.Ok(await service.GetPagedAsync(query, ct));

    [HttpPost("upload")]
    [HasPermission("sys:file:upload")]
    public async Task<ApiResult> Upload(IFormFile file, [FromForm] string? bizType, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            throw new BusinessException("请选择要上传的文件");

        await using var stream = file.OpenReadStream();
        var dto = await service.UploadAsync(new FileUploadInput
        {
            Content = stream,
            FileName = file.FileName,
            ContentType = file.ContentType,
            Size = file.Length,
            BizType = bizType,
        }, ct);
        return ApiResult.Ok(dto);
    }

    [HttpDelete("{id:long}")]
    [HasPermission("sys:file:remove")]
    public async Task<ApiResult> Delete(long id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return ApiResult.Ok();
    }
}
