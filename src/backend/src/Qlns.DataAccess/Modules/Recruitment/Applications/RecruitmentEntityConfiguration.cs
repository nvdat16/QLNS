using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Qlns.DataAccess.Modules.Recruitment.Applications;

public sealed class ApplicationEntityConfiguration : IEntityTypeConfiguration<ApplicationEntity>
{
    public void Configure(EntityTypeBuilder<ApplicationEntity> builder)
    {
        builder.ToTable("applications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CandidateId).HasColumnName("candidate_id");
        builder.Property(x => x.JobPostingId).HasColumnName("job_posting_id");
        builder.Property(x => x.Stage).HasColumnName("stage").HasMaxLength(40);
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
    }
}

public sealed class JobPostingEntityConfiguration : IEntityTypeConfiguration<JobPostingEntity>
{
    public void Configure(EntityTypeBuilder<JobPostingEntity> builder)
    {
        builder.ToTable("job_postings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.DepartmentId).HasColumnName("department_id");
    }
}

public sealed class InterviewEntityConfiguration : IEntityTypeConfiguration<InterviewEntity>
{
    public void Configure(EntityTypeBuilder<InterviewEntity> builder)
    {
        builder.ToTable("interviews");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ApplicationId).HasColumnName("application_id");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30);
    }
}

public sealed class EvaluationEntityConfiguration : IEntityTypeConfiguration<EvaluationEntity>
{
    public void Configure(EntityTypeBuilder<EvaluationEntity> builder)
    {
        builder.ToTable("evaluations");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.InterviewId).HasColumnName("interview_id");
        builder.Property(x => x.Recommendation).HasColumnName("recommendation").HasMaxLength(30);
    }
}

public sealed class ApplicationStageEventEntityConfiguration : IEntityTypeConfiguration<ApplicationStageEventEntity>
{
    public void Configure(EntityTypeBuilder<ApplicationStageEventEntity> builder)
    {
        builder.ToTable("application_stage_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ApplicationId).HasColumnName("application_id");
        builder.Property(x => x.FromStage).HasColumnName("from_stage").HasMaxLength(40);
        builder.Property(x => x.ToStage).HasColumnName("to_stage").HasMaxLength(40);
        builder.Property(x => x.Reason).HasColumnName("reason");
        builder.Property(x => x.ChangedBy).HasColumnName("changed_by");
        builder.Property(x => x.ChangedAt).HasColumnName("changed_at");
        builder.Property(x => x.ApplicationVersion).HasColumnName("application_version");
    }
}

public sealed class AuditLogEntityConfiguration : IEntityTypeConfiguration<AuditLogEntity>
{
    public void Configure(EntityTypeBuilder<AuditLogEntity> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ActorUserId).HasColumnName("actor_user_id");
        builder.Property(x => x.Action).HasColumnName("action").HasMaxLength(120);
        builder.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(100);
        builder.Property(x => x.EntityId).HasColumnName("entity_id").HasMaxLength(100);
        builder.Property(x => x.BeforeData).HasColumnName("before_data").HasColumnType("jsonb");
        builder.Property(x => x.AfterData).HasColumnName("after_data").HasColumnType("jsonb");
        builder.Property(x => x.Result).HasColumnName("result").HasMaxLength(30);
        builder.Property(x => x.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100);
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at");
    }
}
