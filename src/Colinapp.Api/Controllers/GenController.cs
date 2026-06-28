using Colinapp.Api.Authorization;
using Colinapp.Application.CodeGen;
using Colinapp.Shared.Common;
using Colinapp.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Colinapp.Api.Controllers;

/// <summary>
/// 代码生成器：读取数据库表结构，按框架约定生成各层代码，支持在线预览与打包下载。
/// </summary>
[ApiController]
[Authorize]
[Route("api/gen")]
public class GenController(ICodeGenService service) : ControllerBase
{
    /// <summary>数据库表列表。</summary>
    [HttpGet("tables")]
    [HasPermission("tool:gen:list")]
    public async Task<ApiResult> Tables([FromQuery] string? keyword, CancellationToken ct)
        => ApiResult.Ok(await service.GetTablesAsync(keyword, ct));

    /// <summary>按表推断默认生成配置（含列配置），供前端编辑。</summary>
    [HttpGet("config")]
    [HasPermission("tool:gen:list")]
    public async Task<ApiResult> Config([FromQuery] string tableName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tableName))
            throw new BusinessException("请指定表名");
        return ApiResult.Ok(await service.BuildConfigAsync(tableName, ct));
    }

    /// <summary>按配置预览生成的各文件代码。</summary>
    [HttpPost("preview")]
    [HasPermission("tool:gen:list")]
    public ApiResult Preview([FromBody] GenTableConfig config)
        => ApiResult.Ok(service.Generate(config));

    /// <summary>按配置打包下载 zip。</summary>
    [HttpPost("download")]
    [HasPermission("tool:gen:code")]
    public IActionResult Download([FromBody] GenTableConfig config)
    {
        var bytes = service.GenerateZip(config);
        var fileName = $"{config.ClassName}-generated.zip";
        return File(bytes, "application/zip", fileName);
    }
}
