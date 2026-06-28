namespace Colinapp.Application.Auth;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken ct = default);

    /// <summary>用刷新令牌换取新的访问令牌（令牌轮换：旧刷新令牌作废）。</summary>
    Task<LoginResult> RefreshAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>登出：撤销指定刷新令牌。</summary>
    Task LogoutAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>获取个人资料。</summary>
    Task<ProfileDto> GetProfileAsync(long userId, CancellationToken ct = default);

    /// <summary>更新个人资料（昵称/手机/邮箱）。</summary>
    Task UpdateProfileAsync(long userId, UpdateProfileRequest request, CancellationToken ct = default);

    /// <summary>修改密码：校验旧密码，更新后撤销该用户全部刷新令牌（强制其它会话重登）。</summary>
    Task ChangePasswordAsync(long userId, ChangePasswordRequest request, CancellationToken ct = default);
}
