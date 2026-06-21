namespace Colinapp.Application.Auth;

/// <summary>
/// JWT 配置（绑定 appsettings 的 "Jwt" 节）。
/// </summary>
public class JwtOptions
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "Colinapp";

    public string Audience { get; set; } = "ColinappClient";

    /// <summary>签名密钥，至少 32 字节。生产环境务必通过安全配置注入，勿写死。</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>访问令牌有效期（分钟）</summary>
    public int AccessTokenMinutes { get; set; } = 120;

    /// <summary>刷新令牌有效期（天）</summary>
    public int RefreshTokenDays { get; set; } = 7;
}
