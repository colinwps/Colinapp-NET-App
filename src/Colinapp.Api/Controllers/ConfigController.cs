using Colinapp.Api.Authorization;
using Colinapp.Application.Platform;
using Colinapp.Shared.Common;
using Colinapp.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Colinapp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/config")]
public class ConfigController(IConfigService service) : ControllerBase
{
    [HttpGet]
    [HasPermission("sys:config:list")]
    public async Task<ApiResult> List([FromQuery] PagedRequest query, CancellationToken ct)
        => ApiResult.Ok(await service.GetPagedAsync(query, ct));

    /// <summary>按键读取参数值（带缓存，已登录即可访问）</summary>
    [HttpGet("key/{key}")]
    public async Task<ApiResult> GetByKey(string key, CancellationToken ct)
        => ApiResult.Ok(await service.GetValueAsync(key, ct));

    [HttpPost]
    [HasPermission("sys:config:add")]
    public async Task<ApiResult> Create([FromBody] ConfigSaveDto dto, CancellationToken ct)
        => ApiResult.Ok(await service.CreateAsync(dto, ct));

    [HttpPut("{id:long}")]
    [HasPermission("sys:config:edit")]
    public async Task<ApiResult> Update(long id, [FromBody] ConfigSaveDto dto, CancellationToken ct)
    {
        await service.UpdateAsync(id, dto, ct);
        return ApiResult.Ok();
    }

    [HttpDelete("{id:long}")]
    [HasPermission("sys:config:remove")]
    public async Task<ApiResult> Delete(long id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return ApiResult.Ok();
    }
}
