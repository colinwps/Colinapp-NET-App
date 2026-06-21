using Colinapp.Api.Authorization;
using Colinapp.Application.Organization;
using Colinapp.Shared.Common;
using Colinapp.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Colinapp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/role")]
public class RoleController(IRoleService service) : ControllerBase
{
    [HttpGet]
    [HasPermission("sys:role:list")]
    public async Task<ApiResult> List([FromQuery] PagedRequest query, CancellationToken ct)
        => ApiResult.Ok(await service.GetPagedAsync(query, ct));

    /// <summary>启用角色下拉（分配人员用）</summary>
    [HttpGet("options")]
    public async Task<ApiResult> Options(CancellationToken ct)
        => ApiResult.Ok(await service.GetAllAsync(ct));

    [HttpGet("{id:long}")]
    [HasPermission("sys:role:query")]
    public async Task<ApiResult> Get(long id, CancellationToken ct)
        => ApiResult.Ok(await service.GetAsync(id, ct));

    [HttpPost]
    [HasPermission("sys:role:add")]
    public async Task<ApiResult> Create([FromBody] RoleSaveDto dto, CancellationToken ct)
        => ApiResult.Ok(await service.CreateAsync(dto, ct));

    [HttpPut("{id:long}")]
    [HasPermission("sys:role:edit")]
    public async Task<ApiResult> Update(long id, [FromBody] RoleSaveDto dto, CancellationToken ct)
    {
        await service.UpdateAsync(id, dto, ct);
        return ApiResult.Ok();
    }

    [HttpDelete("{id:long}")]
    [HasPermission("sys:role:remove")]
    public async Task<ApiResult> Delete(long id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return ApiResult.Ok();
    }
}
