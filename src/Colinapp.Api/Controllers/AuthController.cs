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

    /// <summary>用刷新令牌换取新的令牌对（令牌轮换）。</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ApiResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await authService.RefreshAsync(request.RefreshToken, ct);
        return ApiResult.Ok(result);
    }

    /// <summary>登出，撤销刷新令牌。</summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<ApiResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        await authService.LogoutAsync(request.RefreshToken, ct);
        return ApiResult.Ok();
    }

    /// <summary>获取当前用户个人资料（可编辑字段）。</summary>
    [HttpGet("profile")]
    [Authorize]
    public async Task<ApiResult> GetProfile(CancellationToken ct)
    {
        var result = await authService.GetProfileAsync(currentUser.UserId ?? 0, ct);
        return ApiResult.Ok(result);
    }

    /// <summary>更新当前用户个人资料。</summary>
    [HttpPut("profile")]
    [Authorize]
    public async Task<ApiResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        await authService.UpdateProfileAsync(currentUser.UserId ?? 0, request, ct);
        return ApiResult.Ok();
    }

    /// <summary>修改当前用户密码。</summary>
    [HttpPut("password")]
    [Authorize]
    public async Task<ApiResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        await authService.ChangePasswordAsync(currentUser.UserId ?? 0, request, ct);
        return ApiResult.Ok();
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
