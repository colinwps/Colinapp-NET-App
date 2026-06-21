using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Colinapp.Api.Authorization;

/// <summary>
/// 动态授权策略提供器：为 "PERM:xxx" 形式的策略按需生成包含 PermissionRequirement 的策略，
/// 无需为每个权限标识预先注册策略。其余策略回退到默认提供器。
/// </summary>
public class PermissionPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private readonly DefaultAuthorizationPolicyProvider _fallback = new(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (HasPermissionAttribute.TryGetPermission(policyName, out var permission))
        {
            var policy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissionRequirement(permission))
                .Build();
            return Task.FromResult<AuthorizationPolicy?>(policy);
        }

        return _fallback.GetPolicyAsync(policyName);
    }
}
