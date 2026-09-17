using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Qlns.DataAccess.Modules.CoreHr.Offboarding;

/// <summary>offboarding_cases — at most one open case per employee (ux_offboarding_open_case).</summary>
public sealed class OffboardingCaseEntity
{
    public long Id { get; set; }
    public long EmployeeId { get; set; }
    public long? EmployeeEventId { get; set; }
    public string SeparationType { get; set; } = null!;
    public DateOnly? NoticeReceivedOn { get; set; }
    public DateOnly LastWorkingDate { get; set; }
    public long? HandoverToEmployeeId { get; set; }
    public DateTimeOffset? ExitInterviewAt { get; set; }
    public string FinalSettlementStatus { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public long CreatedBy { get; set; }
    public long? ApprovedBy { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long Version { get; set; }
}

/// <summary>offboarding_tasks — checklist generated from the template on approval (ux_offboarding_task_template).</summary>
public sealed class OffboardingTaskEntity
{
    public long Id { get; set; }
    public long OffboardingCaseId { get; set; }
    public string TemplateKey { get; set; } = null!;
    public string Category { get; set; } = null!;
    public string TaskName { get; set; } = null!;
    public string? Description { get; set; }
    public long? AssignedToUserId { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public bool BlocksLastWorkingDay { get; set; }
    public string Status { get; set; } = null!;
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long Version { get; set; }
}

public sealed class OffboardingCaseEntityConfiguration : IEntityTypeConfiguration<OffboardingCaseEntity>
{
    public void Configure(EntityTypeBuilder<OffboardingCaseEntity> builder)
    {
        builder.ToTable("offboarding_cases");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.EmployeeId).HasColumnName("employee_id");
        builder.Property(x => x.EmployeeEventId).HasColumnName("employee_event_id");
        builder.Property(x => x.SeparationType).HasColumnName("separation_type").HasMaxLength(40);
        builder.Property(x => x.NoticeReceivedOn).HasColumnName("notice_received_on");
        builder.Property(x => x.LastWorkingDate).HasColumnName("last_working_date");
        builder.Property(x => x.HandoverToEmployeeId).HasColumnName("handover_to_employee_id");
        builder.Property(x => x.ExitInterviewAt).HasColumnName("exit_interview_at");
        builder.Property(x => x.FinalSettlementStatus).HasColumnName("final_settlement_status").HasMaxLength(30);
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30);
        builder.Property(x => x.Reason).HasColumnName("reason");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.ApprovedBy).HasColumnName("approved_by");
        builder.Property(x => x.ApprovedAt).HasColumnName("approved_at");
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
    }
}

public sealed class OffboardingTaskEntityConfiguration : IEntityTypeConfiguration<OffboardingTaskEntity>
{
    public void Configure(EntityTypeBuilder<OffboardingTaskEntity> builder)
    {
        builder.ToTable("offboarding_tasks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.OffboardingCaseId).HasColumnName("offboarding_case_id");
        builder.Property(x => x.TemplateKey).HasColumnName("template_key").HasMaxLength(100);
        builder.Property(x => x.Category).HasColumnName("category").HasMaxLength(30);
        builder.Property(x => x.TaskName).HasColumnName("task_name").HasMaxLength(255);
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.AssignedToUserId).HasColumnName("assigned_to_user_id");
        builder.Property(x => x.DueAt).HasColumnName("due_at");
        builder.Property(x => x.BlocksLastWorkingDay).HasColumnName("blocks_last_working_day");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30);
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        builder.HasIndex(x => new { x.OffboardingCaseId, x.TemplateKey }).IsUnique().HasDatabaseName("ux_offboarding_task_template");
    }
}
