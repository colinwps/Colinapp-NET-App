using Colinapp.Application.Common;
using Colinapp.Infrastructure.Caching;
using Colinapp.Infrastructure.Multitenancy;
using Colinapp.Infrastructure.Persistence;
using Colinapp.Infrastructure.Persistence.Interceptors;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

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

        // 文件存储（本地磁盘实现，后续可替换为对象存储）
        services.AddSingleton<Application.Storage.IFileStorage, Storage.LocalFileStorage>();

        // 代码生成器：表结构内省（依赖 AppDbContext 连接）
        services.AddScoped<Application.CodeGen.ITableSchemaProvider, CodeGen.MySqlTableSchemaProvider>();

        // 缓存：按 Cache:Provider 在内存与 Redis 间切换（默认内存，开箱即用）
        services.Configure<CacheOptions>(configuration.GetSection(CacheOptions.SectionName));
        var cacheOptions = configuration.GetSection(CacheOptions.SectionName).Get<CacheOptions>() ?? new CacheOptions();
        if (cacheOptions.UseRedis && !string.IsNullOrWhiteSpace(cacheOptions.RedisConnection))
        {
            var redisConfig = ConfigurationOptions.Parse(cacheOptions.RedisConnection);
            redisConfig.AbortOnConnectFail = false; // Redis 不可达时不阻断启动，运行期降级
            redisConfig.ConnectTimeout = 2000;
            redisConfig.SyncTimeout = 1000;
            redisConfig.ConnectRetry = 1;
            services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConfig));
            services.AddSingleton<ICacheService, RedisCacheService>();
        }
        else
        {
            services.AddMemoryCache();
            services.AddSingleton<ICacheService, MemoryCacheService>();
        }

        return services;
    }
}
