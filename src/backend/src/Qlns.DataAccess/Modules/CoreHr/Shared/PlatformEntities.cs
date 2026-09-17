namespace Qlns.DataAccess.Modules.CoreHr.Shared;

// Persistence models of the two crosscutting platform tables (audit_logs, outbox_messages).
// Every module writes them inside the same transaction as its business change; no module reads them
// through an API in this delivery (administration endpoints are out of scope).

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

public sealed class OutboxMessageEntity
{
    public Guid Id { get; set; }
    public string MessageType { get; set; } = null!;
    public string AggregateType { get; set; } = null!;
    public string AggregateId { get; set; } = null!;
    /// <summary>jsonb, serialized.</summary>
    public string Payload { get; set; } = null!;
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset AvailableAt { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
    public int Attempts { get; set; }
    public string? LastError { get; set; }
}
