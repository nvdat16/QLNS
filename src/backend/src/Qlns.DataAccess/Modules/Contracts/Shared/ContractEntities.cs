namespace Qlns.DataAccess.Modules.Contracts.Shared;

// Persistence models for the Contracts tables of database/schema.sql (v1.1). Rules live in Qlns.BusinessLogic.

public sealed class ContractEntity
{
    public long Id { get; set; }
    public long EmployeeId { get; set; }
    public string ContractNumber { get; set; } = null!;
    public string ContractType { get; set; } = null!;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public decimal Salary { get; set; }
    public string Currency { get; set; } = null!;
    public int? NoticePeriodDays { get; set; }
    public string Status { get; set; } = null!;
    public bool IsPrimary { get; set; }
    public string? DocumentObjectKey { get; set; }
    public DateTimeOffset? SignedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long Version { get; set; }
}

public sealed class ContractAddendumEntity
{
    public long Id { get; set; }
    public long ContractId { get; set; }
    public string AddendumNumber { get; set; } = null!;
    /// <summary>Optimistic concurrency version (ETag), not a sibling sequence — see schema v1.1.</summary>
    public long Version { get; set; }
    public string Status { get; set; } = null!;
    public DateOnly EffectiveDate { get; set; }
    /// <summary>jsonb, serialized.</summary>
    public string BeforeTerms { get; set; } = null!;
    /// <summary>jsonb, serialized.</summary>
    public string AfterTerms { get; set; } = null!;
    public string Reason { get; set; } = null!;
    public string? DocumentObjectKey { get; set; }
    public long CreatedBy { get; set; }
    public long? ApprovedBy { get; set; }
    public DateTimeOffset? ApprovedAt { get; set; }
    public DateTimeOffset? SignedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
