using Colinapp.Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Colinapp.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("sys_user");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserName).HasMaxLength(64).IsRequired();
        builder.Property(x => x.PasswordHash).HasMaxLength(128).IsRequired();
        builder.Property(x => x.NickName).HasMaxLength(64).IsRequired();
        builder.Property(x => x.Phone).HasMaxLength(32);
        builder.Property(x => x.Email).HasMaxLength(128);

        builder.HasIndex(x => x.UserName).IsUnique();
    }
}
