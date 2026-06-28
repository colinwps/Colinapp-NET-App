using Colinapp.Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Colinapp.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("sys_refresh_token");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Token).HasMaxLength(128).IsRequired();
        builder.Property(x => x.ReplacedByToken).HasMaxLength(128);

        builder.HasIndex(x => x.Token).IsUnique();
        builder.HasIndex(x => x.UserId);
    }
}
