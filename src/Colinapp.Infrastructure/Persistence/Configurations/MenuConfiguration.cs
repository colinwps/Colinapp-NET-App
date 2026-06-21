using Colinapp.Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Colinapp.Infrastructure.Persistence.Configurations;

public class MenuConfiguration : IEntityTypeConfiguration<Menu>
{
    public void Configure(EntityTypeBuilder<Menu> builder)
    {
        builder.ToTable("sys_menu");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(64).IsRequired();
        builder.Property(x => x.MenuType).HasConversion<int>();
        builder.Property(x => x.Path).HasMaxLength(256);
        builder.Property(x => x.Component).HasMaxLength(256);
        builder.Property(x => x.Permission).HasMaxLength(128);
        builder.Property(x => x.Icon).HasMaxLength(64);

        builder.HasIndex(x => x.ParentId);
        builder.HasIndex(x => x.Permission);
    }
}
