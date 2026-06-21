using Colinapp.Application.Common;
using Colinapp.Infrastructure.Multitenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;

namespace Colinapp.Infrastructure.Persistence;

/// <summary>
/// 设计时上下文工厂，供 `dotnet ef migrations` 使用，无需启动整个应用。
/// 生成迁移时不会真正连库（已显式指定 MySqlServerVersion）。
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("COLINAPP_CONNECTION")
            ?? "server=localhost;port=3306;database=colinapp;user=root;password=root;CharSet=utf8mb4;";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(
                connectionString,
                new MySqlServerVersion(new Version(8, 0)),
                mysql => mysql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .Options;

        return new AppDbContext(options, new DesignTimeCurrentUser(), Options.Create(new TenantOptions()));
    }

    private sealed class DesignTimeCurrentUser : ICurrentUser
    {
        public long? UserId => null;
        public string? UserName => null;
        public long? TenantId => null;
        public bool IsAuthenticated => false;
        public bool IsAdmin => false;
    }
}
