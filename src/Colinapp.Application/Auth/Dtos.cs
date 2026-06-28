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

/// <summary>刷新令牌请求</summary>
public class RefreshTokenRequest
{
    public string RefreshToken { get; set; } = string.Empty;
}

/// <summary>修改密码请求</summary>
public class ChangePasswordRequest
{
    public string OldPassword { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>个人资料（可编辑字段）</summary>
public class ProfileDto
{
    public long Id { get; set; }

    public string UserName { get; set; } = string.Empty;

    public string NickName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public bool IsAdmin { get; set; }
}

/// <summary>更新个人资料请求</summary>
public class UpdateProfileRequest
{
    public string NickName { get; set; } = string.Empty;

    public string? Phone { get; set; }

    public string? Email { get; set; }
}
