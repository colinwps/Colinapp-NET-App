using Colinapp.Application.Common;
using Colinapp.Shared.Common;
using Colinapp.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace Colinapp.Application.Auth;

public class AuthService(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService) : IAuthService
{
    public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(x => x.UserName == request.UserName, ct);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new BusinessException("账号或密码错误", ResultCode.Unauthorized);

        if (!user.Enabled)
            throw new BusinessException("账号已停用", ResultCode.Forbidden);

        var (accessToken, expiresAt) = jwtTokenService.CreateAccessToken(user);
        var refreshToken = jwtTokenService.CreateRefreshToken();

        user.LastLoginTime = DateTime.Now;
        await db.SaveChangesAsync(ct);

        return new LoginResult
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt,
            User = new UserProfile
            {
                Id = user.Id,
                UserName = user.UserName,
                NickName = user.NickName,
                IsAdmin = user.IsAdmin,
            }
        };
    }
}
