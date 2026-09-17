using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Qlns.DataAccess.Modules.Recruitment.Shared;

public sealed class JobPostingEntityConfiguration : IEntityTypeConfiguration<JobPostingEntity>
{
    public void Configure(EntityTypeBuilder<JobPostingEntity> builder)
    {
        builder.ToTable("job_postings");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.JobCode).HasColumnName("job_code").HasMaxLength(50);
        builder.Property(x => x.Title).HasColumnName("title").HasMaxLength(255);
        builder.Property(x => x.DepartmentId).HasColumnName("department_id");
        builder.Property(x => x.PositionId).HasColumnName("position_id");
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.Requirements).HasColumnName("requirements");
        builder.Property(x => x.Location).HasColumnName("location").HasMaxLength(255);
        builder.Property(x => x.EmploymentType).HasColumnName("employment_type").HasMaxLength(30);
        builder.Property(x => x.SalaryMin).HasColumnName("salary_min").HasPrecision(15, 2);
        builder.Property(x => x.SalaryMax).HasColumnName("salary_max").HasPrecision(15, 2);
        builder.Property(x => x.TargetHeadcount).HasColumnName("target_headcount");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(40);
        builder.Property(x => x.ClosingDate).HasColumnName("closing_date");
        builder.Property(x => x.PublishedAt).HasColumnName("published_at");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        builder.HasIndex(x => x.JobCode).IsUnique();
    }
}

public sealed class CandidateEntityConfiguration : IEntityTypeConfiguration<CandidateEntity>
{
    public void Configure(EntityTypeBuilder<CandidateEntity> builder)
    {
        builder.ToTable("candidates");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.FirstName).HasColumnName("first_name").HasMaxLength(100);
        builder.Property(x => x.LastName).HasColumnName("last_name").HasMaxLength(100);
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(320);
        builder.Property(x => x.NormalizedEmail).HasColumnName("normalized_email").HasMaxLength(320);
        builder.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(30);
        builder.Property(x => x.NormalizedPhone).HasColumnName("normalized_phone").HasMaxLength(30);
        builder.Property(x => x.LinkedinUrl).HasColumnName("linkedin_url").HasMaxLength(2048);
        builder.Property(x => x.PortfolioUrl).HasColumnName("portfolio_url").HasMaxLength(2048);
        builder.Property(x => x.PrivacyNoticeVersion).HasColumnName("privacy_notice_version").HasMaxLength(50);
        builder.Property(x => x.ConsentedAt).HasColumnName("consented_at");
        builder.Property(x => x.RetentionUntil).HasColumnName("retention_until");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        builder.HasIndex(x => x.NormalizedEmail).IsUnique().HasDatabaseName("ux_candidates_normalized_email");
    }
}

public sealed class ResumeEntityConfiguration : IEntityTypeConfiguration<ResumeEntity>
{
    public void Configure(EntityTypeBuilder<ResumeEntity> builder)
    {
        builder.ToTable("resumes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.IntakeId).HasColumnName("intake_id");
        builder.Property(x => x.JobPostingId).HasColumnName("job_posting_id");
        builder.Property(x => x.CandidateId).HasColumnName("candidate_id");
        builder.Property(x => x.ObjectKey).HasColumnName("object_key").HasMaxLength(1024);
        builder.Property(x => x.OriginalFileName).HasColumnName("original_file_name").HasMaxLength(255);
        builder.Property(x => x.ContentType).HasColumnName("content_type").HasMaxLength(100);
        builder.Property(x => x.SizeBytes).HasColumnName("size_bytes");
        builder.Property(x => x.IntakeStatus).HasColumnName("intake_status").HasMaxLength(30);
        builder.Property(x => x.MalwareScanStatus).HasColumnName("malware_scan_status").HasMaxLength(30);
        builder.Property(x => x.ParserStatus).HasColumnName("parser_status").HasMaxLength(30);
        builder.Property(x => x.ParsedData).HasColumnName("parsed_data").HasColumnType("jsonb");
        builder.Property(x => x.ParseConfidence).HasColumnName("parse_confidence").HasColumnType("jsonb");
        builder.Property(x => x.ParserVersion).HasColumnName("parser_version").HasMaxLength(100);
        builder.Property(x => x.DuplicateCandidateIds).HasColumnName("duplicate_candidate_ids");
        builder.Property(x => x.UploadedBy).HasColumnName("uploaded_by");
        builder.Property(x => x.UploadedAt).HasColumnName("uploaded_at");
        builder.Property(x => x.ConfirmedBy).HasColumnName("confirmed_by");
        builder.Property(x => x.ConfirmedAt).HasColumnName("confirmed_at");
        builder.HasIndex(x => x.IntakeId).IsUnique();
        builder.HasIndex(x => x.ObjectKey).IsUnique();
    }
}

public sealed class ApplicationEntityConfiguration : IEntityTypeConfiguration<ApplicationEntity>
{
    public void Configure(EntityTypeBuilder<ApplicationEntity> builder)
    {
        builder.ToTable("applications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.CandidateId).HasColumnName("candidate_id");
        builder.Property(x => x.JobPostingId).HasColumnName("job_posting_id");
        builder.Property(x => x.ResumeId).HasColumnName("resume_id");
        builder.Property(x => x.Stage).HasColumnName("stage").HasMaxLength(40);
        builder.Property(x => x.AiScore).HasColumnName("ai_score").HasPrecision(5, 2);
        builder.Property(x => x.Source).HasColumnName("source").HasMaxLength(80);
        builder.Property(x => x.AppliedAt).HasColumnName("applied_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        builder.HasIndex(x => new { x.CandidateId, x.JobPostingId }).IsUnique().HasDatabaseName("ux_applications_candidate_job");
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
        builder.HasIndex(x => new { x.ApplicationId, x.ApplicationVersion }).IsUnique().HasDatabaseName("ux_application_stage_version");
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
        builder.Property(x => x.InterviewType).HasColumnName("interview_type").HasMaxLength(50);
        builder.Property(x => x.StartsAt).HasColumnName("starts_at");
        builder.Property(x => x.EndsAt).HasColumnName("ends_at");
        builder.Property(x => x.Timezone).HasColumnName("timezone").HasMaxLength(100);
        builder.Property(x => x.InterviewerUserId).HasColumnName("interviewer_user_id");
        builder.Property(x => x.Location).HasColumnName("location").HasMaxLength(255);
        builder.Property(x => x.MeetingUrl).HasColumnName("meeting_url").HasMaxLength(2048);
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30);
        builder.Property(x => x.CancellationReason).HasColumnName("cancellation_reason");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
    }
}

public sealed class InterviewPanelistEntityConfiguration : IEntityTypeConfiguration<InterviewPanelistEntity>
{
    public void Configure(EntityTypeBuilder<InterviewPanelistEntity> builder)
    {
        builder.ToTable("interview_panelists");
        builder.HasKey(x => new { x.InterviewId, x.UserId });
        builder.Property(x => x.InterviewId).HasColumnName("interview_id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
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
        builder.Property(x => x.EvaluatorUserId).HasColumnName("evaluator_user_id");
        builder.Property(x => x.TechnicalScore).HasColumnName("technical_score").HasPrecision(2, 1);
        builder.Property(x => x.CommunicationScore).HasColumnName("communication_score").HasPrecision(2, 1);
        builder.Property(x => x.ProblemSolvingScore).HasColumnName("problem_solving_score").HasPrecision(2, 1);
        builder.Property(x => x.TeamworkScore).HasColumnName("teamwork_score").HasPrecision(2, 1);
        builder.Property(x => x.OverallScore).HasColumnName("overall_score").HasPrecision(2, 1);
        builder.Property(x => x.Recommendation).HasColumnName("recommendation").HasMaxLength(30);
        builder.Property(x => x.Feedback).HasColumnName("feedback");
        builder.Property(x => x.SubmittedAt).HasColumnName("submitted_at");
        builder.Property(x => x.UnlockedAt).HasColumnName("unlocked_at");
        builder.Property(x => x.UnlockedBy).HasColumnName("unlocked_by");
        builder.Property(x => x.UnlockReason).HasColumnName("unlock_reason");
        builder.Property(x => x.Version).HasColumnName("version");
        builder.HasIndex(x => new { x.InterviewId, x.EvaluatorUserId, x.Version }).IsUnique()
            .HasDatabaseName("ux_evaluation_interviewer_version");
    }
}

public sealed class OfferEntityConfiguration : IEntityTypeConfiguration<OfferEntity>
{
    public void Configure(EntityTypeBuilder<OfferEntity> builder)
    {
        builder.ToTable("offers");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ApplicationId).HasColumnName("application_id");
        builder.Property(x => x.BaseSalary).HasColumnName("base_salary").HasPrecision(15, 2);
        builder.Property(x => x.BonusAmount).HasColumnName("bonus_amount").HasPrecision(15, 2);
        builder.Property(x => x.AllowanceAmount).HasColumnName("allowance_amount").HasPrecision(15, 2);
        builder.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3).IsFixedLength();
        builder.Property(x => x.EmploymentType).HasColumnName("employment_type").HasMaxLength(50);
        builder.Property(x => x.StartDate).HasColumnName("start_date");
        builder.Property(x => x.ExpirationDate).HasColumnName("expiration_date");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30);
        builder.Property(x => x.TemplateVersion).HasColumnName("template_version").HasMaxLength(50);
        builder.Property(x => x.DocumentObjectKey).HasColumnName("document_object_key").HasMaxLength(1024);
        builder.Property(x => x.ApprovedBy).HasColumnName("approved_by");
        builder.Property(x => x.ApprovedAt).HasColumnName("approved_at");
        builder.Property(x => x.SentAt).HasColumnName("sent_at");
        builder.Property(x => x.RespondedAt).HasColumnName("responded_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
    }
}
