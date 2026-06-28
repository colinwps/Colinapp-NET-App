using Colinapp.Application.Common;
using Colinapp.Application.Scheduling;
using Colinapp.Infrastructure.Caching;
using Colinapp.Infrastructure.Multitenancy;
using Colinapp.Infrastructure.Persistence;
using Colinapp.Infrastructure.Persistence.Interceptors;
using Colinapp.Infrastructure.Scheduling;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
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

        // 定时任务：按 Quartz:Enabled 决定是否启动调度器（默认 true）。
        AddScheduling(services, configuration);

        return services;
    }

    private static void AddScheduling(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<Scheduling.SchedulingOptions>(configuration.GetSection(Scheduling.SchedulingOptions.SectionName));
        var quartzOptions = configuration.GetSection(Scheduling.SchedulingOptions.SectionName).Get<Scheduling.SchedulingOptions>()
            ?? new Scheduling.SchedulingOptions();

        // 注册表 + 内置任务类型（供 Quartz 作业工厂在执行作用域内解析依赖）。
        var registry = new JobRegistry();
        services.AddSingleton(registry);
        services.AddSingleton<IJobRegistry>(registry);
        foreach (var jobType in registry.AllJobTypes())
            services.AddTransient(jobType);

        if (!quartzOptions.Enabled)
        {
            // 关闭调度：CRUD 仍可用，仅不真正触发。
            services.AddSingleton<IJobScheduler, NoOpJobScheduler>();
            return;
        }

        services.AddSingleton<JobExecutionListener>();
        services.AddQuartz(q =>
        {
            // 全局监听器（无匹配器=匹配所有任务），从 DI 解析。
            q.AddJobListener<JobExecutionListener>();
        });
        services.AddQuartzHostedService(options => options.WaitForJobsToComplete = true);

        services.AddSingleton<IJobScheduler, QuartzJobScheduler>();

        // 在 Quartz 托管服务之后注册，确保调度器已就绪再装载数据库中的任务。
        services.AddHostedService<JobBootstrapper>();
    }
}
