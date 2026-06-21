using Colinapp.Application.Common;
using Colinapp.Infrastructure.Multitenancy;
using Colinapp.Infrastructure.Persistence;
using Colinapp.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Colinapp.Infrastructure;

/// <summary>
/// 基础设施层 DI 注册入口。
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TenantOptions>(configuration.GetSection(TenantOptions.SectionName));

        services.AddScoped<AuditSaveChangesInterceptor>();

        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("缺少数据库连接字符串 ConnectionStrings:Default");

        // 显式指定版本，避免启动/迁移时为探测版本而连库（"Database:MySqlVersion" 可覆盖，默认 8.0）
        var versionText = configuration["Database:MySqlVersion"] ?? "8.0";
        var serverVersion = new MySqlServerVersion(Version.Parse(versionText));

        services.AddDbContext<AppDbContext>((sp, options) =>
        {
            options.UseMySql(
                connectionString,
                serverVersion,
                mysql => mysql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));

            options.AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        return services;
    }
}
