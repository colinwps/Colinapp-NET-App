using Colinapp.Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Colinapp.Infrastructure.Persistence.Configurations;

public class FileRecordConfiguration : IEntityTypeConfiguration<FileRecord>
{
    public void Configure(EntityTypeBuilder<FileRecord> builder)
    {
        builder.ToTable("sys_file");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FileName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.StoredName).HasMaxLength(255).IsRequired();
        builder.Property(x => x.RelativePath).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Url).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ContentType).HasMaxLength(150);
        builder.Property(x => x.Ext).HasMaxLength(20);
        builder.Property(x => x.BizType).HasMaxLength(50);
        builder.Property(x => x.StorageType).HasMaxLength(20).IsRequired();

        builder.HasIndex(x => x.BizType);
        builder.HasIndex(x => x.CreatedTime);
    }
}
