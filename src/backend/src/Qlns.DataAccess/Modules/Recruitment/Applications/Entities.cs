namespace Qlns.DataAccess.Modules.Recruitment.Applications;

public sealed class ApplicationEntity
{
    public long Id { get; set; }
    public long CandidateId { get; set; }
    public long JobPostingId { get; set; }
    public string Stage { get; set; } = null!;
    public DateTimeOffset UpdatedAt { get; set; }
    public long Version { get; set; }
}

public sealed class JobPostingEntity
{
    public long Id { get; set; }
    public long DepartmentId { get; set; }
}

public sealed class InterviewEntity
{
    public long Id { get; set; }
    public long ApplicationId { get; set; }
    public string Status { get; set; } = null!;
}

public sealed class EvaluationEntity
{
    public long Id { get; set; }
    public long InterviewId { get; set; }
    public string Recommendation { get; set; } = null!;
}

public sealed class ApplicationStageEventEntity
{
    public long Id { get; set; }
    public long ApplicationId { get; set; }
    public string FromStage { get; set; } = null!;
    public string ToStage { get; set; } = null!;
    public string? Reason { get; set; }
    public long ChangedBy { get; set; }
    public DateTimeOffset ChangedAt { get; set; }
    public long ApplicationVersion { get; set; }
}

public sealed class AuditLogEntity
{
    public long Id { get; set; }
    public long? ActorUserId { get; set; }
    public string Action { get; set; } = null!;
    public string EntityType { get; set; } = null!;
    public string EntityId { get; set; } = null!;
    public string? BeforeData { get; set; }
    public string? AfterData { get; set; }
    public string Result { get; set; } = null!;
    public string CorrelationId { get; set; } = null!;
    public DateTimeOffset OccurredAt { get; set; }
}
