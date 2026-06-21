using Colinapp.Api.Authorization;
using Colinapp.Application.Organization;
using Colinapp.Shared.Common;
using Colinapp.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Colinapp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/position")]
public class PositionController(IPositionService service) : ControllerBase
{
    [HttpGet]
    [HasPermission("sys:post:list")]
    public async Task<ApiResult> List([FromQuery] PagedRequest query, CancellationToken ct)
        => ApiResult.Ok(await service.GetPagedAsync(query, ct));

    /// <summary>启用职位下拉（分配人员用）</summary>
    [HttpGet("options")]
    public async Task<ApiResult> Options(CancellationToken ct)
        => ApiResult.Ok(await service.GetAllAsync(ct));

    [HttpPost]
    [HasPermission("sys:post:add")]
    public async Task<ApiResult> Create([FromBody] PositionSaveDto dto, CancellationToken ct)
        => ApiResult.Ok(await service.CreateAsync(dto, ct));

    [HttpPut("{id:long}")]
    [HasPermission("sys:post:edit")]
    public async Task<ApiResult> Update(long id, [FromBody] PositionSaveDto dto, CancellationToken ct)
    {
        await service.UpdateAsync(id, dto, ct);
        return ApiResult.Ok();
    }

    [HttpDelete("{id:long}")]
    [HasPermission("sys:post:remove")]
    public async Task<ApiResult> Delete(long id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return ApiResult.Ok();
    }
}
