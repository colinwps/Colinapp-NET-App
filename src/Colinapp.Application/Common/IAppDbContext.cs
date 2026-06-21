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

    DbSet<Notice> Notices { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
