using Colinapp.Domain.Entities.System;

namespace Colinapp.Application.Auth;

public interface IJwtTokenService
{
    /// <summary>为用户生成访问令牌，返回令牌字符串与过期时间。</summary>
    (string token, DateTime expiresAt) CreateAccessToken(User user);

    /// <summary>生成一个不透明刷新令牌。</summary>
    string CreateRefreshToken();
}
