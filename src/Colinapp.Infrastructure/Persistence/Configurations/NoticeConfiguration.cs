using Colinapp.Domain.Entities.Business;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Colinapp.Infrastructure.Persistence.Configurations;

public class NoticeConfiguration : IEntityTypeConfiguration<Notice>
{
    public void Configure(EntityTypeBuilder<Notice> builder)
    {
        builder.ToTable("biz_notice");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NoticeType).HasConversion<int>();
        builder.Property(x => x.Content).HasColumnType("text");

        builder.HasIndex(x => x.CreatedTime);
    }
}
