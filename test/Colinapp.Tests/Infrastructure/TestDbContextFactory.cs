using Colinapp.Application.Common;
using Colinapp.Infrastructure.Multitenancy;
using Colinapp.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Colinapp.Tests.Infrastructure;

/// <summary>
/// 用 EF Core InMemory 提供器构造真实的 <see cref="AppDbContext"/>，
/// 复用其全部实体配置与全局查询过滤器（软删除/租户），无需连接 MySQL。
/// 每次调用使用独立库名，测试之间相互隔离。
/// </summary>
public static class TestDbContextFactory
{
    public static AppDbContext Create(
        ICurrentUser? currentUser = null,
        bool tenantEnabled = false,
        string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName ?? $"colinapp-test-{Guid.NewGuid():N}")
            .EnableSensitiveDataLogging()
            .Options;

        var tenantOptions = Options.Create(new TenantOptions { Enabled = tenantEnabled });
        return new AppDbContext(options, currentUser ?? TestCurrentUser.Anonymous, tenantOptions);
    }
}
