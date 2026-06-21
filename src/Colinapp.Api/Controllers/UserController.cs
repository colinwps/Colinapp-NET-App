using Colinapp.Api.Authorization;
using Colinapp.Application.Organization;
using Colinapp.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Colinapp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/user")]
public class UserController(IUserService service) : ControllerBase
{
    /// <summary>用户分页（按当前用户数据范围过滤）</summary>
    [HttpGet]
    [HasPermission("sys:user:list")]
    public async Task<ApiResult> List([FromQuery] UserQuery query, CancellationToken ct)
        => ApiResult.Ok(await service.GetPagedAsync(query, ct));

    [HttpGet("{id:long}")]
    [HasPermission("sys:user:query")]
    public async Task<ApiResult> Get(long id, CancellationToken ct)
        => ApiResult.Ok(await service.GetAsync(id, ct));

    [HttpPost]
    [HasPermission("sys:user:add")]
    public async Task<ApiResult> Create([FromBody] UserSaveDto dto, CancellationToken ct)
        => ApiResult.Ok(await service.CreateAsync(dto, ct));

    [HttpPut("{id:long}")]
    [HasPermission("sys:user:edit")]
    public async Task<ApiResult> Update(long id, [FromBody] UserSaveDto dto, CancellationToken ct)
    {
        await service.UpdateAsync(id, dto, ct);
        return ApiResult.Ok();
    }

    [HttpDelete("{id:long}")]
    [HasPermission("sys:user:remove")]
    public async Task<ApiResult> Delete(long id, CancellationToken ct)
    {
        await service.DeleteAsync(id, ct);
        return ApiResult.Ok();
    }

    [HttpPut("{id:long}/password")]
    [HasPermission("sys:user:resetPwd")]
    public async Task<ApiResult> ResetPassword(long id, [FromBody] ResetPasswordRequest req, CancellationToken ct)
    {
        await service.ResetPasswordAsync(id, req.Password, ct);
        return ApiResult.Ok();
    }

    [HttpPut("{id:long}/status")]
    [HasPermission("sys:user:edit")]
    public async Task<ApiResult> ChangeStatus(long id, [FromBody] ChangeStatusRequest req, CancellationToken ct)
    {
        await service.ChangeStatusAsync(id, req.Enabled, ct);
        return ApiResult.Ok();
    }

    public record ResetPasswordRequest(string Password);
    public record ChangeStatusRequest(bool Enabled);
}
