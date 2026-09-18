namespace Qlns.DataAccess.Modules.CoreHr.Shared;

// Persistence models for the Core HR tables of database/schema.sql. They are deliberately
// dumb: all rules live in Qlns.BusinessLogic. Column names are mapped in CoreHrEntityConfigurations.

public sealed class DepartmentEntity
{
    public long Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public long? ParentDepartmentId { get; set; }
    public string? CostCenter { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long Version { get; set; }
}

public sealed class PositionEntity
{
    public long Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Level { get; set; }
    public string? Description { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long Version { get; set; }
}

public sealed class UserEntity
{
    public long Id { get; set; }
    public string ExternalSubject { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long Version { get; set; }
}

public sealed class EmployeeEntity
{
    public long Id { get; set; }
    public string EmployeeCode { get; set; } = null!;
    public long? SourceApplicationId { get; set; }
    public long? UserId { get; set; }
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string? WorkEmail { get; set; }
    public string? PersonalEmail { get; set; }
    public string? Phone { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Gender { get; set; }
    public string? OfficeLocation { get; set; }
    public string? PermanentAddress { get; set; }
    public string? TemporaryAddress { get; set; }
    /// <summary>jsonb object of string values, serialized.</summary>
    public string? EmergencyContact { get; set; }
    public long? ManagerId { get; set; }
    public long DepartmentId { get; set; }
    public long PositionId { get; set; }
    public DateOnly HireDate { get; set; }
    public string Status { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long Version { get; set; }
}

public sealed class OnboardingTaskEntity
{
    public long Id { get; set; }
    public long EmployeeId { get; set; }
    public string TemplateKey { get; set; } = null!;
    public string TaskName { get; set; } = null!;
    public string? Description { get; set; }
    public long? AssignedToUserId { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public string Status { get; set; } = null!;
    public DateTimeOffset? CompletedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long Version { get; set; }
}

public sealed class EmployeeDocumentEntity
{
    public long Id { get; set; }
    public long EmployeeId { get; set; }
    public string DocumentType { get; set; } = null!;
    public int Version { get; set; }
    public string OriginalFileName { get; set; } = null!;
    public string ObjectKey { get; set; } = null!;
    public string ContentType { get; set; } = null!;
    public long SizeBytes { get; set; }
    public long UploadedBy { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
    public DateOnly? RetentionUntil { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}

public sealed class EmployeeEventEntity
{
    public long Id { get; set; }
    public long EmployeeId { get; set; }
    public string EventType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateOnly EffectiveDate { get; set; }
    /// <summary>jsonb, serialized.</summary>
    public string BeforeData { get; set; } = null!;
    /// <summary>jsonb, serialized.</summary>
    public string AfterData { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public long? CompensatesEventId { get; set; }
    public long CreatedBy { get; set; }
    public long? ApprovedBy { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? AppliedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long Version { get; set; }
}
