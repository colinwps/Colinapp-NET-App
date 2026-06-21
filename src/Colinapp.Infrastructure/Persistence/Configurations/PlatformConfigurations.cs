using Colinapp.Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Colinapp.Infrastructure.Persistence.Configurations;

public class OperationLogConfiguration : IEntityTypeConfiguration<OperationLog>
{
    public void Configure(EntityTypeBuilder<OperationLog> builder)
    {
        builder.ToTable("sys_oper_log");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).HasMaxLength(128).IsRequired();
        builder.Property(x => x.OperatorName).HasMaxLength(64);
        builder.Property(x => x.Method).HasMaxLength(256);
        builder.Property(x => x.RequestMethod).HasMaxLength(16);
        builder.Property(x => x.RequestUrl).HasMaxLength(512);
        builder.Property(x => x.RequestParams).HasColumnType("text");
        builder.Property(x => x.Ip).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(512);
        builder.Property(x => x.ErrorMessage).HasMaxLength(2000);

        builder.HasIndex(x => x.CreatedTime);
    }
}

public class LoginLogConfiguration : IEntityTypeConfiguration<LoginLog>
{
    public void Configure(EntityTypeBuilder<LoginLog> builder)
    {
        builder.ToTable("sys_login_log");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserName).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(256);
        builder.Property(x => x.Ip).HasMaxLength(64);
        builder.Property(x => x.UserAgent).HasMaxLength(512);

        builder.HasIndex(x => x.CreatedTime);
    }
}

public class DictTypeConfiguration : IEntityTypeConfiguration<DictType>
{
    public void Configure(EntityTypeBuilder<DictType> builder)
    {
        builder.ToTable("sys_dict_type");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Type).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Remark).HasMaxLength(256);

        builder.HasIndex(x => x.Type).IsUnique();
    }
}

public class DictDataConfiguration : IEntityTypeConfiguration<DictData>
{
    public void Configure(EntityTypeBuilder<DictData> builder)
    {
        builder.ToTable("sys_dict_data");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DictType).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Label).HasMaxLength(128).IsRequired();
        builder.Property(x => x.Value).HasMaxLength(256).IsRequired();
        builder.Property(x => x.TagType).HasMaxLength(32);
        builder.Property(x => x.Remark).HasMaxLength(256);

        builder.HasIndex(x => x.DictType);
    }
}

public class SysConfigConfiguration : IEntityTypeConfiguration<SysConfig>
{
    public void Configure(EntityTypeBuilder<SysConfig> builder)
    {
        builder.ToTable("sys_config");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ConfigKey).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ConfigValue).HasMaxLength(1024).IsRequired();
        builder.Property(x => x.Remark).HasMaxLength(256);

        builder.HasIndex(x => x.ConfigKey).IsUnique();
    }
}
