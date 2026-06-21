using Colinapp.Api.Authorization;
using Colinapp.Application.Platform;
using Colinapp.Shared.Common;
using Colinapp.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Colinapp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dict")]
public class DictController(IDictService service) : ControllerBase
{
    // ---- 字典类型 ----

    [HttpGet("type")]
    [HasPermission("sys:dict:list")]
    public async Task<ApiResult> Types([FromQuery] PagedRequest query, CancellationToken ct)
        => ApiResult.Ok(await service.GetTypesAsync(query, ct));

    [HttpPost("type")]
    [HasPermission("sys:dict:add")]
    public async Task<ApiResult> CreateType([FromBody] DictTypeSaveDto dto, CancellationToken ct)
        => ApiResult.Ok(await service.CreateTypeAsync(dto, ct));

    [HttpPut("type/{id:long}")]
    [HasPermission("sys:dict:edit")]
    public async Task<ApiResult> UpdateType(long id, [FromBody] DictTypeSaveDto dto, CancellationToken ct)
    {
        await service.UpdateTypeAsync(id, dto, ct);
        return ApiResult.Ok();
    }

    [HttpDelete("type/{id:long}")]
    [HasPermission("sys:dict:remove")]
    public async Task<ApiResult> DeleteType(long id, CancellationToken ct)
    {
        await service.DeleteTypeAsync(id, ct);
        return ApiResult.Ok();
    }

    // ---- 字典数据 ----

    /// <summary>按类型取启用字典项（前端下拉，已登录即可访问）</summary>
    [HttpGet("data/options/{type}")]
    public async Task<ApiResult> Options(string type, CancellationToken ct)
        => ApiResult.Ok(await service.GetDataByTypeAsync(type, ct));

    [HttpGet("data")]
    [HasPermission("sys:dict:list")]
    public async Task<ApiResult> Data([FromQuery] string type, [FromQuery] PagedRequest query, CancellationToken ct)
        => ApiResult.Ok(await service.GetDataPagedAsync(type, query, ct));

    [HttpPost("data")]
    [HasPermission("sys:dict:add")]
    public async Task<ApiResult> CreateData([FromBody] DictDataSaveDto dto, CancellationToken ct)
        => ApiResult.Ok(await service.CreateDataAsync(dto, ct));

    [HttpPut("data/{id:long}")]
    [HasPermission("sys:dict:edit")]
    public async Task<ApiResult> UpdateData(long id, [FromBody] DictDataSaveDto dto, CancellationToken ct)
    {
        await service.UpdateDataAsync(id, dto, ct);
        return ApiResult.Ok();
    }

    [HttpDelete("data/{id:long}")]
    [HasPermission("sys:dict:remove")]
    public async Task<ApiResult> DeleteData(long id, CancellationToken ct)
    {
        await service.DeleteDataAsync(id, ct);
        return ApiResult.Ok();
    }
}
