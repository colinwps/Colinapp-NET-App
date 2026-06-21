using Microsoft.AspNetCore.Authorization;

namespace Colinapp.Api.Authorization;

/// <summary>
/// 标注接口所需的功能权限标识，如 [HasPermission("sys:user:list")]。
/// 通过动态策略（PermissionPolicyProvider）转换为 PermissionRequirement 校验。
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class HasPermissionAttribute : AuthorizeAttribute
{
    public const string PolicyPrefix = "PERM:";

    public HasPermissionAttribute(string permission)
    {
        Permission = permission;
        Policy = PolicyPrefix + permission;
    }

    public string Permission { get; }

    public static bool TryGetPermission(string policyName, out string permission)
    {
        if (policyName.StartsWith(PolicyPrefix, StringComparison.Ordinal))
        {
            permission = policyName[PolicyPrefix.Length..];
            return true;
        }
        permission = string.Empty;
        return false;
    }
}
