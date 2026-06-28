using Colinapp.Application.Common;

namespace Colinapp.Tests.Infrastructure;

/// <summary>
/// 测试用 <see cref="ICurrentUser"/>，各字段可直接设置以模拟不同登录身份。
/// </summary>
public sealed class TestCurrentUser : ICurrentUser
{
    public long? UserId { get; set; }
    public string? UserName { get; set; }
    public long? TenantId { get; set; }
    public bool IsAdmin { get; set; }

    public bool IsAuthenticated => UserId is not null;

    public static TestCurrentUser Anonymous => new();
    public static TestCurrentUser Admin(long id = 1) => new() { UserId = id, UserName = "admin", IsAdmin = true };
    public static TestCurrentUser Normal(long id) => new() { UserId = id, UserName = $"user{id}" };
}
