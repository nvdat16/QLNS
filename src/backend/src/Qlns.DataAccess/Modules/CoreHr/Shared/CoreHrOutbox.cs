using System.Globalization;
using System.Text.Json;

namespace Qlns.DataAccess.Modules.CoreHr.Shared;

/// <summary>
/// Builds outbox_messages rows (transactional outbox, architecture §6.5). Repositories add the returned
/// entity to the same transaction as the business change; the background worker delivers it after commit.
/// Payloads must never contain restricted data (salary, identity numbers, tokens beyond what the recipient needs).
/// </summary>
public static class CoreHrOutbox
{
    public static OutboxMessageEntity Message(
        string messageType,
        string aggregateType,
        long aggregateId,
        object payload,
        DateTimeOffset occurredAt) =>
        Message(messageType, aggregateType, aggregateId.ToString(CultureInfo.InvariantCulture), payload, occurredAt);

    public static OutboxMessageEntity Message(
        string messageType,
        string aggregateType,
        string aggregateId,
        object payload,
        DateTimeOffset occurredAt) => new()
    {
        Id = Guid.CreateVersion7(occurredAt),
        MessageType = messageType,
        AggregateType = aggregateType,
        AggregateId = aggregateId,
        Payload = JsonSerializer.Serialize(payload, CoreHrAudit.JsonOptions),
        OccurredAt = occurredAt,
        AvailableAt = occurredAt,
        Attempts = 0
    };
}
