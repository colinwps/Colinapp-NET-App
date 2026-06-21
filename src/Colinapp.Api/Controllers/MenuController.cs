using Colinapp.Api.Authorization;
using Colinapp.Application.Organization;
using Colinapp.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Colinapp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/menu")]
public class MenuController(IMenuService service) : ControllerBase
{
    /// <summary>全部菜单树（菜单管理用）</summary>
    [HttpGet("tree")]
    [HasPermission("sys:menu:list")]
    public async Task<ApiResult> Tree(CancellationToken ct)
        => ApiResult.Ok(await service.GetTreeAsync(ct));

    /// <summary>当前用户可见菜单树（前端动态路由用）</summary>
    [HttpGet("routes")]
    public async Task<ApiResult> Routes(CancellationToken ct)
        => ApiResult.Ok(await service.GetCurrentUserMenusAsync(ct));

    [HttpGet("{id:long}")]
    [HasPermission("sys:menu:query")]
    public async Task<ApiResult> Get(long id, CancellationToken ct)
        => ApiResult.Ok(await service.GetAsync(id, ct));

    [HttpPost]
    [HasPermission("sys:menu:add")]
    public async Task<ApiResult> Create([FromBody] MenuSaveDto dto, CancellationToken ct)
        => ApiResult.Ok(await service.CreateAsync(dto, ct));

    [HttpPut("{id:long}")]
    [HasPermission("sys:menu:edit")]
    public async Task<ApiResult> Update(long id, [FromBody] MenuSaveDto dto, CancellationToken ct)
    {
        await service.UpdateAsync(id, dto, ct);
        return ApiResult.Ok();
    }

    [HttpDelete("{id:long}")]
    [HasPermission("sys:menu:remove")]
    public async Task<ApiResult> Delete(long id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return ApiResult.Ok();
    }
}
