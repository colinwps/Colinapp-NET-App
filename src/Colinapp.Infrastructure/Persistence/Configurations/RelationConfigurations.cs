using Colinapp.Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Colinapp.Infrastructure.Persistence.Configurations;

public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("sys_user_role");
        builder.HasKey(x => new { x.UserId, x.RoleId });
        builder.HasIndex(x => x.RoleId);
    }
}

public class RoleMenuConfiguration : IEntityTypeConfiguration<RoleMenu>
{
    public void Configure(EntityTypeBuilder<RoleMenu> builder)
    {
        builder.ToTable("sys_role_menu");
        builder.HasKey(x => new { x.RoleId, x.MenuId });
        builder.HasIndex(x => x.MenuId);
    }
}

public class UserPostConfiguration : IEntityTypeConfiguration<UserPost>
{
    public void Configure(EntityTypeBuilder<UserPost> builder)
    {
        builder.ToTable("sys_user_post");
        builder.HasKey(x => new { x.UserId, x.PositionId });
        builder.HasIndex(x => x.PositionId);
    }
}

public class RoleDeptConfiguration : IEntityTypeConfiguration<RoleDept>
{
    public void Configure(EntityTypeBuilder<RoleDept> builder)
    {
        builder.ToTable("sys_role_dept");
        builder.HasKey(x => new { x.RoleId, x.DeptId });
    }
}
