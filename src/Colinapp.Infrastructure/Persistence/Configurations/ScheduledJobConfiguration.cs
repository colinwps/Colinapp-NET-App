using Colinapp.Domain.Entities.System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Colinapp.Infrastructure.Persistence.Configurations;

public class ScheduledJobConfiguration : IEntityTypeConfiguration<ScheduledJob>
{
    public void Configure(EntityTypeBuilder<ScheduledJob> builder)
    {
        builder.ToTable("sys_job");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name).HasMaxLength(100).IsRequired();
        builder.Property(x => x.JobGroup).HasMaxLength(50).IsRequired();
        builder.Property(x => x.InvokeTarget).HasMaxLength(100).IsRequired();
        builder.Property(x => x.CronExpression).HasMaxLength(100).IsRequired();
        builder.Property(x => x.JobData).HasMaxLength(500);
        builder.Property(x => x.Remark).HasMaxLength(500);

        builder.HasIndex(x => new { x.JobGroup, x.Name });
    }
}

public class ScheduledJobLogConfiguration : IEntityTypeConfiguration<ScheduledJobLog>
{
    public void Configure(EntityTypeBuilder<ScheduledJobLog> builder)
    {
        builder.ToTable("sys_job_log");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.JobName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.JobGroup).HasMaxLength(50).IsRequired();
        builder.Property(x => x.InvokeTarget).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(2000);
        builder.Property(x => x.Exception).HasColumnType("text");

        builder.HasIndex(x => x.JobId);
        builder.HasIndex(x => x.StartTime);
    }
}
