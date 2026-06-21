using Colinapp.Api.Authorization;
using Colinapp.Application.Platform;
using Colinapp.Shared.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Colinapp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/log")]
public class LogController(ILogService service) : ControllerBase
{
    [HttpGet("operation")]
    [HasPermission("sys:log:list")]
    public async Task<ApiResult> Operation([FromQuery] OperationLogQuery query, CancellationToken ct)
        => ApiResult.Ok(await service.GetOperationLogsAsync(query, ct));

    [HttpDelete("operation")]
    [HasPermission("sys:log:remove")]
    public async Task<ApiResult> ClearOperation(CancellationToken ct)
    {
        await service.ClearOperationLogsAsync(ct);
        return ApiResult.Ok();
    }

    [HttpGet("login")]
    [HasPermission("sys:logininfor:list")]
    public async Task<ApiResult> Login([FromQuery] LoginLogQuery query, CancellationToken ct)
        => ApiResult.Ok(await service.GetLoginLogsAsync(query, ct));

    [HttpDelete("login")]
    [HasPermission("sys:logininfor:remove")]
    public async Task<ApiResult> ClearLogin(CancellationToken ct)
    {
        await service.ClearLoginLogsAsync(ct);
        return ApiResult.Ok();
    }
}
