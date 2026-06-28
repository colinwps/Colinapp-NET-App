using System.Reflection;
using Colinapp.Application.Common;
using Colinapp.Domain.Entities;
using Colinapp.Domain.Entities.Business;
using Colinapp.Domain.Entities.System;
using Colinapp.Infrastructure.Multitenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Colinapp.Infrastructure.Persistence;

/// <summary>
/// 应用数据库上下文。统一应用软删除与租户全局查询过滤器，并自动加载实体配置。
/// </summary>
public class AppDbContext : DbContext, IAppDbContext
{
    private readonly ICurrentUser _currentUser;
    private readonly bool _tenantFilterEnabled;

    public AppDbContext(
        DbContextOptions<AppDbContext> options,
        ICurrentUser currentUser,
        IOptions<TenantOptions> tenantOptions)
        : base(options)
    {
        _currentUser = currentUser;
        _tenantFilterEnabled = tenantOptions.Value.Enabled;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Position> Positions => Set<Position>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Menu> Menus => Set<Menu>();

    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RoleMenu> RoleMenus => Set<RoleMenu>();
    public DbSet<UserPost> UserPosts => Set<UserPost>();
    public DbSet<RoleDept> RoleDepts => Set<RoleDept>();

    public DbSet<OperationLog> OperationLogs => Set<OperationLog>();
    public DbSet<LoginLog> LoginLogs => Set<LoginLog>();
    public DbSet<DictType> DictTypes => Set<DictType>();
    public DbSet<DictData> DictData => Set<DictData>();
    public DbSet<SysConfig> SysConfigs => Set<SysConfig>();
    public DbSet<FileRecord> Files => Set<FileRecord>();
    public DbSet<ScheduledJob> ScheduledJobs => Set<ScheduledJob>();
    public DbSet<ScheduledJobLog> ScheduledJobLogs => Set<ScheduledJobLog>();

    public DbSet<Notice> Notices => Set<Notice>();

    /// <summary>查询过滤器引用此成员，EF Core 会将其参数化，每次查询动态求值。</summary>
    public long? CurrentTenantId => _currentUser.TenantId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 自动应用本程序集内所有 IEntityTypeConfiguration
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // 为所有继承 EntityBase 的实体统一加上 软删除 + 租户 全局过滤器
        var setFilter = typeof(AppDbContext)
            .GetMethod(nameof(SetGlobalFilter), BindingFlags.NonPublic | BindingFlags.Instance)!;

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(EntityBase).IsAssignableFrom(entityType.ClrType))
            {
                setFilter.MakeGenericMethod(entityType.ClrType).Invoke(this, [modelBuilder]);
            }
        }
    }

    /// <summary>
    /// 强类型设置全局过滤器：!IsDeleted &amp;&amp; (租户过滤关闭 || TenantId == 当前租户)。
    /// 过滤器中对 _tenantFilterEnabled / CurrentTenantId 的引用会被 EF Core 参数化，
    /// 保证模型快照稳定（不会出现 "model has pending changes"）。
    /// </summary>
    private void SetGlobalFilter<TEntity>(ModelBuilder modelBuilder) where TEntity : EntityBase
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(
            e => !e.IsDeleted && (!_tenantFilterEnabled || e.TenantId == CurrentTenantId));
    }
}
