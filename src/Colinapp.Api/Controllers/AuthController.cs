using Colinapp.Application.Auth;
using Colinapp.Application.Common;
using Colinapp.Application.Permissions;
using Colinapp.Application.Platform;
using Colinapp.Shared.Common;
using Colinapp.Shared.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Colinapp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    IAuthService authService,
    ICurrentUser currentUser,
    IPermissionService permissionService,
    ILogService logService) : ControllerBase
{
    /// <summary>账号密码登录，返回 JWT 令牌。登录结果（成功/失败）写入登录日志。</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ApiResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var ua = HttpContext.Request.Headers.UserAgent.ToString();
        try
        {
            var result = await authService.LoginAsync(request, ct);
            await logService.AddLoginLogAsync(request.UserName, true, "登录成功", ip, ua, ct);
            return ApiResult.Ok(result);
        }
        catch (BusinessException ex)
        {
            await logService.AddLoginLogAsync(request.UserName, false, ex.Message, ip, ua, ct);
            throw;
        }
    }

    /// <summary>获取当前登录用户信息、角色与权限标识（前端鉴权用）。</summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<ApiResult> Me(CancellationToken ct)
    {
        var userId = currentUser.UserId ?? 0;

        // 管理员拥有全部权限，前端以 "*" 通配；普通用户返回聚合权限标识。
        var permissions = currentUser.IsAdmin
            ? (IReadOnlyCollection<string>)["*"]
            : (await permissionService.GetUserPermissionsAsync(userId, ct)).ToList();
        var roleIds = currentUser.IsAdmin
            ? []
            : await permissionService.GetUserRoleIdsAsync(userId, ct);

        return ApiResult.Ok(new
        {
            currentUser.UserId,
            currentUser.UserName,
            currentUser.IsAdmin,
            currentUser.TenantId,
            roleIds,
            permissions,
        });
    }
}
