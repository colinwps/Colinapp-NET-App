using Colinapp.Api.Authorization;
using Colinapp.Application.Organization;
using Colinapp.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Colinapp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/dept")]
public class DepartmentController(IDepartmentService service) : ControllerBase
{
    /// <summary>部门树</summary>
    [HttpGet("tree")]
    [HasPermission("sys:dept:list")]
    public async Task<ApiResult> Tree(CancellationToken ct)
        => ApiResult.Ok(await service.GetTreeAsync(ct));

    [HttpGet("{id:long}")]
    [HasPermission("sys:dept:query")]
    public async Task<ApiResult> Get(long id, CancellationToken ct)
        => ApiResult.Ok(await service.GetAsync(id, ct));

    [HttpPost]
    [HasPermission("sys:dept:add")]
    public async Task<ApiResult> Create([FromBody] DepartmentSaveDto dto, CancellationToken ct)
        => ApiResult.Ok(await service.CreateAsync(dto, ct));

    [HttpPut("{id:long}")]
    [HasPermission("sys:dept:edit")]
    public async Task<ApiResult> Update(long id, [FromBody] DepartmentSaveDto dto, CancellationToken ct)
    {
        await service.UpdateAsync(id, dto, ct);
        return ApiResult.Ok();
    }

    [HttpDelete("{id:long}")]
    [HasPermission("sys:dept:remove")]
    public async Task<ApiResult> Delete(long id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return ApiResult.Ok();
    }
}
