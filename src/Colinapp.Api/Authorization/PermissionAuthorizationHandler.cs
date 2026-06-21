using Colinapp.Application.Common;
using Colinapp.Application.Permissions;
using Microsoft.AspNetCore.Authorization;

namespace Colinapp.Api.Authorization;

/// <summary>
/// 权限校验处理器。管理员直接放行；否则比对用户聚合的权限标识集合。
/// </summary>
public class PermissionAuthorizationHandler(
    ICurrentUser currentUser,
    IPermissionService permissionService) : AuthorizationHandler<PermissionRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context, PermissionRequirement requirement)
    {
        if (currentUser.UserId is not { } userId)
            return; // 未认证，保持失败

        if (currentUser.IsAdmin)
        {
            context.Succeed(requirement);
            return;
        }

        var permissions = await permissionService.GetUserPermissionsAsync(userId);
        if (permissions.Contains(requirement.Permission))
            context.Succeed(requirement);
    }
}
