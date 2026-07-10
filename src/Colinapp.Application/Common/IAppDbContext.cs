using Colinapp.Domain.Entities.Business;
using Colinapp.Domain.Entities.System;
using Microsoft.EntityFrameworkCore;

namespace Colinapp.Application.Common;

/// <summary>
/// 数据库上下文抽象。应用层依赖此接口而非具体 DbContext，便于测试与解耦。
/// 由 Infrastructure 层的 AppDbContext 实现。
/// </summary>
public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Department> Departments { get; }
    DbSet<Position> Positions { get; }
    DbSet<Role> Roles { get; }
    DbSet<Menu> Menus { get; }

    DbSet<UserRole> UserRoles { get; }
    DbSet<RoleMenu> RoleMenus { get; }
    DbSet<UserPost> UserPosts { get; }
    DbSet<RoleDept> RoleDepts { get; }

    DbSet<OperationLog> OperationLogs { get; }
    DbSet<LoginLog> LoginLogs { get; }
    DbSet<DictType> DictTypes { get; }
    DbSet<DictData> DictData { get; }
    DbSet<SysConfig> SysConfigs { get; }
    DbSet<FileRecord> Files { get; }
    DbSet<ScheduledJob> ScheduledJobs { get; }
    DbSet<ScheduledJobLog> ScheduledJobLogs { get; }

    DbSet<Notice> Notices { get; }

    DbSet<Domain.Entities.Workflow.WorkflowDefinition> WorkflowDefinitions { get; }
    DbSet<Domain.Entities.Workflow.WorkflowInstance> WorkflowInstances { get; }
    DbSet<Domain.Entities.Workflow.WorkflowTask> WorkflowTasks { get; }
    DbSet<Domain.Entities.Workflow.WorkflowCcRecord> WorkflowCcRecords { get; }

    DbSet<Domain.Entities.Forms.FormDefinition> FormDefinitions { get; }
    DbSet<Domain.Entities.Forms.FormEntry> FormEntries { get; }

    /// <summary>泛型实体集访问（供代码生成等通用场景，无需为每个实体声明命名 DbSet）。</summary>
    DbSet<TEntity> Set<TEntity>() where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
