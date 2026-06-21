namespace Colinapp.Application.Auth;

/// <summary>登录请求</summary>
public class LoginRequest
{
    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}

/// <summary>登录结果 / 令牌</summary>
public class LoginResult
{
    public string AccessToken { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>访问令牌过期时间（UTC）</summary>
    public DateTime ExpiresAt { get; set; }

    public UserProfile User { get; set; } = new();
}

/// <summary>当前登录用户基本信息</summary>
public class UserProfile
{
    public long Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string NickName { get; set; } = string.Empty;

    public bool IsAdmin { get; set; }
}
