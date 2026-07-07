using Colinapp.Domain.Entities.Workflow;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Colinapp.Infrastructure.Persistence.Configurations;

public class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinition>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
    {
        builder.ToTable("wf_definition");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.GraphJson).HasColumnType("text");
        builder.Property(x => x.FormFieldsJson).HasColumnType("text");
        builder.Property(x => x.Remark).HasMaxLength(500);

        builder.HasIndex(x => x.Code);
    }
}

public class WorkflowInstanceConfiguration : IEntityTypeConfiguration<WorkflowInstance>
{
    public void Configure(EntityTypeBuilder<WorkflowInstance> builder)
    {
        builder.ToTable("wf_instance");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.DefinitionName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.FormData).HasColumnType("text");
        builder.Property(x => x.GraphJson).HasColumnType("text");
        builder.Property(x => x.FormFieldsJson).HasColumnType("text");
        builder.Property(x => x.CurrentNodeId).HasMaxLength(64);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.InitiatorName).HasMaxLength(50);

        builder.HasIndex(x => x.DefinitionId);
        builder.HasIndex(x => new { x.InitiatorId, x.Status });
    }
}

public class WorkflowTaskConfiguration : IEntityTypeConfiguration<WorkflowTask>
{
    public void Configure(EntityTypeBuilder<WorkflowTask> builder)
    {
        builder.ToTable("wf_task");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.NodeId).HasMaxLength(64);
        builder.Property(x => x.NodeName).HasMaxLength(100);
        builder.Property(x => x.ApproveMode).HasConversion<int>();
        builder.Property(x => x.ApproverName).HasMaxLength(50);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.Comment).HasMaxLength(1000);

        builder.HasIndex(x => x.InstanceId);
        builder.HasIndex(x => new { x.ApproverId, x.Status });
    }
}

public class WorkflowCcRecordConfiguration : IEntityTypeConfiguration<WorkflowCcRecord>
{
    public void Configure(EntityTypeBuilder<WorkflowCcRecord> builder)
    {
        builder.ToTable("wf_cc_record");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.NodeId).HasMaxLength(64);
        builder.Property(x => x.NodeName).HasMaxLength(100);
        builder.Property(x => x.UserName).HasMaxLength(50);

        builder.HasIndex(x => x.InstanceId);
        builder.HasIndex(x => new { x.UserId, x.ReadTime });
    }
}
