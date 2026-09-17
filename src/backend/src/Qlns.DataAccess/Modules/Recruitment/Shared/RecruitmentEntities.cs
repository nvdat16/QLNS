namespace Qlns.DataAccess.Modules.Recruitment.Shared;

// Persistence models for the Recruitment tables of database/schema.sql (v1.1). They are deliberately
// dumb: all rules live in Qlns.BusinessLogic. Column names are mapped in RecruitmentEntityConfigurations.

/// <summary>job_postings — a requisition and, once published, the careers posting.</summary>
public sealed class JobPostingEntity
{
    public long Id { get; set; }
    public string JobCode { get; set; } = null!;
    public string Title { get; set; } = null!;
    public long DepartmentId { get; set; }
    public long? PositionId { get; set; }
    public string? Description { get; set; }
    public string? Requirements { get; set; }
    public string? Location { get; set; }
    public string EmploymentType { get; set; } = null!;
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public int TargetHeadcount { get; set; }
    public string Status { get; set; } = null!;
    public DateOnly? ClosingDate { get; set; }
    public DateTimeOffset? PublishedAt { get; set; }
    public long CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long Version { get; set; }
}

public sealed class CandidateEntity
{
    public long Id { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string NormalizedEmail { get; set; } = null!;
    public string? Phone { get; set; }
    public string? NormalizedPhone { get; set; }
    public string? LinkedinUrl { get; set; }
    public string? PortfolioUrl { get; set; }
    public string PrivacyNoticeVersion { get; set; } = null!;
    public DateTimeOffset ConsentedAt { get; set; }
    public DateOnly? RetentionUntil { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long Version { get; set; }
}

/// <summary>resumes — created at intake time, before any candidate exists. intake_id is the public identifier.</summary>
public sealed class ResumeEntity
{
    public long Id { get; set; }
    public Guid IntakeId { get; set; }
    public long JobPostingId { get; set; }
    public long? CandidateId { get; set; }
    public string ObjectKey { get; set; } = null!;
    public string OriginalFileName { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long SizeBytes { get; set; }
    public string IntakeStatus { get; set; } = null!;
    public string MalwareScanStatus { get; set; } = null!;
    public string ParserStatus { get; set; } = null!;
    /// <summary>jsonb object (CandidateInput shape), serialized.</summary>
    public string? ParsedData { get; set; }
    /// <summary>jsonb object of field → confidence 0..1, serialized.</summary>
    public string? ParseConfidence { get; set; }
    public string? ParserVersion { get; set; }
    public long[] DuplicateCandidateIds { get; set; } = [];
    public long? UploadedBy { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
    public long? ConfirmedBy { get; set; }
    public DateTimeOffset? ConfirmedAt { get; set; }
}

public sealed class ApplicationEntity
{
    public long Id { get; set; }
    public long CandidateId { get; set; }
    public long JobPostingId { get; set; }
    public long? ResumeId { get; set; }
    public string Stage { get; set; } = null!;
    public decimal? AiScore { get; set; }
    public string Source { get; set; } = null!;
    public DateTimeOffset AppliedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long Version { get; set; }
}

public sealed class ApplicationStageEventEntity
{
    public long Id { get; set; }
    public long ApplicationId { get; set; }
    public string? FromStage { get; set; }
    public string ToStage { get; set; } = null!;
    public string? Reason { get; set; }
    public long ChangedBy { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
    public long ApplicationVersion { get; set; }
}

/// <summary>interviews — <see cref="InterviewerUserId"/> is the lead interviewer; the full panel is in interview_panelists.</summary>
public sealed class InterviewEntity
{
    public long Id { get; set; }
    public long ApplicationId { get; set; }
    public string InterviewType { get; set; } = null!;
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public string Timezone { get; set; } = null!;
    public long InterviewerUserId { get; set; }
    public string? Location { get; set; }
    public string? MeetingUrl { get; set; }
    public string Status { get; set; } = null!;
    public string? CancellationReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long Version { get; set; }
}

public sealed class InterviewPanelistEntity
{
    public long InterviewId { get; set; }
    public long UserId { get; set; }
}

/// <summary>evaluations — immutable scorecards; unlock inserts a new row with <see cref="Version"/> + 1.</summary>
public sealed class EvaluationEntity
{
    public long Id { get; set; }
    public long InterviewId { get; set; }
    public long EvaluatorUserId { get; set; }
    public decimal TechnicalScore { get; set; }
    public decimal CommunicationScore { get; set; }
    public decimal ProblemSolvingScore { get; set; }
    public decimal TeamworkScore { get; set; }
    public decimal OverallScore { get; set; }
    public string Recommendation { get; set; } = null!;
    public string Feedback { get; set; } = null!;
    public DateTimeOffset SubmittedAt { get; set; }
    public DateTimeOffset? UnlockedAt { get; set; }
    public long? UnlockedBy { get; set; }
    public string? UnlockReason { get; set; }
    public int Version { get; set; }
}

public sealed class OfferEntity
{
    public long Id { get; set; }
    public long ApplicationId { get; set; }
    public decimal BaseSalary { get; set; }
    public decimal? BonusAmount { get; set; }
    public decimal? AllowanceAmount { get; set; }
    public string Currency { get; set; } = null!;
    public string EmploymentType { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly ExpirationDate { get; set; }
    public string Status { get; set; } = null!;
    public string TemplateVersion { get; set; } = null!;
    public string? DocumentObjectKey { get; set; }
    public long? ApprovedBy { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? SentAt { get; set; }
    public DateTimeOffset? RespondedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long Version { get; set; }
}
