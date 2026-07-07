using Colinapp.Application.Common;
using Colinapp.Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Colinapp.Infrastructure.Persistence;

/// <summary>
/// 数据库初始化：应用迁移并写入种子数据（默认管理员）。
/// </summary>
public static class DbInitializer
{
    public static async Task InitializeAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var sp = scope.ServiceProvider;
        var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("DbInitializer");
        var db = sp.GetRequiredService<AppDbContext>();

        logger.LogInformation("应用数据库迁移…");
        await db.Database.MigrateAsync();

        // 一次性：旧版线性工作流 JSON → 图格式（幂等，无旧数据时零开销）
        await WorkflowGraphMigrator.MigrateAsync(db, logger);

        if (!await db.Users.IgnoreQueryFilters().AnyAsync())
        {
            var hasher = sp.GetRequiredService<IPasswordHasher>();
            db.Users.Add(new User
            {
                UserName = "admin",
                NickName = "超级管理员",
                PasswordHash = hasher.Hash("Admin@123"),
                IsAdmin = true,
                Enabled = true,
            });
            await db.SaveChangesAsync();
            logger.LogInformation("已创建默认管理员 admin / Admin@123");
        }

        // 幂等：每次启动补齐缺失的系统菜单与权限点
        await MenuSeeder.SeedAsync(db);
        logger.LogInformation("系统菜单种子已校验/补齐");

        // 幂等：补齐内置定时任务（默认暂停）
        var jobRegistry = sp.GetRequiredService<Application.Scheduling.IJobRegistry>();
        await JobSeeder.SeedAsync(db, jobRegistry);
        logger.LogInformation("定时任务种子已校验/补齐");
    }
}
