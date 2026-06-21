using System.Security.Claims;
using Colinapp.Application.Common;

namespace Colinapp.Api.Common;

/// <summary>
/// 基于 HttpContext 的当前用户实现。从 JWT Claims 中解析用户信息。
/// </summary>
public class CurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => accessor.HttpContext?.User;

    public long? UserId
        => long.TryParse(Principal?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    public string? UserName => Principal?.FindFirstValue(ClaimTypes.Name);

    /// <summary>
    /// 当前租户：优先取 JWT 的 tenant_id 声明（权威来源）；
    /// 若无声明，则回退读取请求头 X-Tenant-Id（便于开发/测试在不建租户用户时验证隔离）。
    /// </summary>
    public long? TenantId
    {
        get
        {
            if (long.TryParse(Principal?.FindFirstValue("tenant_id"), out var claimId))
                return claimId;
            var header = accessor.HttpContext?.Request.Headers["X-Tenant-Id"].ToString();
            return long.TryParse(header, out var headerId) ? headerId : null;
        }
    }

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public bool IsAdmin
        => string.Equals(Principal?.FindFirstValue("is_admin"), "true", StringComparison.OrdinalIgnoreCase);
}
