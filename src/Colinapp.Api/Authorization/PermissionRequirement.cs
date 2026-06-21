using Microsoft.AspNetCore.Authorization;

namespace Colinapp.Api.Authorization;

/// <summary>携带单个权限标识的授权要求。</summary>
public class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
