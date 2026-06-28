using Colinapp.Application.Common;
using Colinapp.Domain.Entities.System;
using Colinapp.Shared.Common;
using Colinapp.Shared.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Colinapp.Application.Auth;

public class AuthService(
    IAppDbContext db,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private readonly JwtOptions _jwt = jwtOptions.Value;

    public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await db.Users
            .FirstOrDefaultAsync(x => x.UserName == request.UserName, ct);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new BusinessException("账号或密码错误", ResultCode.Unauthorized);

        if (!user.Enabled)
            throw new BusinessException("账号已停用", ResultCode.Forbidden);

        user.LastLoginTime = DateTime.Now;
        var result = IssueTokens(user);
        await db.SaveChangesAsync(ct);
        return result;
    }

    public async Task<LoginResult> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new BusinessException("刷新令牌无效", ResultCode.Unauthorized);

        var existing = await db.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == refreshToken, ct);

        if (existing is null || !existing.IsActive(DateTime.UtcNow))
            throw new BusinessException("登录已过期，请重新登录", ResultCode.Unauthorized);

        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == existing.UserId, ct);
        if (user is null || !user.Enabled)
            throw new BusinessException("账号不可用，请重新登录", ResultCode.Unauthorized);

        // 令牌轮换：作废旧刷新令牌并签发新令牌对
        existing.RevokedAt = DateTime.UtcNow;
        var result = IssueTokens(user);
        existing.ReplacedByToken = result.RefreshToken;
        await db.SaveChangesAsync(ct);
        return result;
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;

        var existing = await db.RefreshTokens
            .FirstOrDefaultAsync(x => x.Token == refreshToken && x.RevokedAt == null, ct);
        if (existing is null) return;

        existing.RevokedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }

    public async Task<ProfileDto> GetProfileAsync(long userId, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct)
            ?? throw BusinessException.NotFound("用户不存在");

        return new ProfileDto
        {
            Id = user.Id,
            UserName = user.UserName,
            NickName = user.NickName,
            Phone = user.Phone,
            Email = user.Email,
            IsAdmin = user.IsAdmin,
        };
    }

    public async Task UpdateProfileAsync(long userId, UpdateProfileRequest request, CancellationToken ct = default)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct)
            ?? throw BusinessException.NotFound("用户不存在");

        user.NickName = request.NickName;
        user.Phone = request.Phone;
        user.Email = request.Email;
        await db.SaveChangesAsync(ct);
    }

    public async Task ChangePasswordAsync(long userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword))
            throw new BusinessException("新密码不能为空");

        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == userId, ct)
            ?? throw BusinessException.NotFound("用户不存在");

        if (!passwordHasher.Verify(request.OldPassword, user.PasswordHash))
            throw new BusinessException("原密码不正确");

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);

        // 改密后撤销该用户全部未失效刷新令牌，强制其它会话重新登录
        var tokens = await db.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAt == null)
            .ToListAsync(ct);
        var now = DateTime.UtcNow;
        foreach (var t in tokens) t.RevokedAt = now;

        await db.SaveChangesAsync(ct);
    }

    /// <summary>签发访问令牌与刷新令牌，并将刷新令牌入库（调用方负责 SaveChanges）。</summary>
    private LoginResult IssueTokens(User user)
    {
        var (accessToken, expiresAt) = jwtTokenService.CreateAccessToken(user);
        var refreshToken = jwtTokenService.CreateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            Token = refreshToken,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwt.RefreshTokenDays),
        });

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
