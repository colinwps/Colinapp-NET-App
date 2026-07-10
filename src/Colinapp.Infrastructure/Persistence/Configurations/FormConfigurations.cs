using Colinapp.Domain.Entities.Forms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Colinapp.Infrastructure.Persistence.Configurations;

public class FormDefinitionConfiguration : IEntityTypeConfiguration<FormDefinition>
{
    public void Configure(EntityTypeBuilder<FormDefinition> builder)
    {
        builder.ToTable("form_definition");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.Icon).HasMaxLength(50);
        builder.Property(x => x.SchemaJson).HasColumnType("text");
        builder.Property(x => x.Status).HasConversion<int>();

        builder.HasIndex(x => x.Code);
        builder.HasIndex(x => x.Status);
    }
}

public class FormEntryConfiguration : IEntityTypeConfiguration<FormEntry>
{
    public void Configure(EntityTypeBuilder<FormEntry> builder)
    {
        builder.ToTable("form_entry");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FormName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.SchemaJson).HasColumnType("text");
        builder.Property(x => x.DataJson).HasColumnType("text");
        builder.Property(x => x.SubmitterName).HasMaxLength(50);

        builder.HasIndex(x => x.FormDefinitionId);
        builder.HasIndex(x => x.SubmitterId);
        builder.HasIndex(x => x.WorkflowInstanceId);
    }
}
