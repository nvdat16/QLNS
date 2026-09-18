using System.Globalization;
using System.Text.Json;
using Qlns.DataAccess.Modules.CoreHr.Shared;

namespace Qlns.DataAccess.Modules.Identity.Shared;

/// <summary>
/// Builds audit_logs rows for the Identity &amp; Access module. Unlike <see cref="CoreHrAudit"/> it accepts a
/// null actor and a non-numeric entity id, because a rejected sign-in has no established actor and may not
/// even match a user row. Payloads carry the attempted e-mail and the client fingerprint — never the
/// password, the password hash or any token value.
/// </summary>
internal static class IdentityAudit
{
    public const string Succeeded = "succeeded";
    public const string Rejected = "rejected";

    public static AuditLogEntity Entry(
        long? actorUserId,
        string action,
        string entityType,
        string entityId,
        object? before,
        object? after,
        string correlationId,
        DateTimeOffset occurredAt,
        string result = Succeeded) => new()
    {
        ActorUserId = actorUserId is > 0 ? actorUserId : null,
        Action = action,
        EntityType = entityType,
        EntityId = entityId,
        BeforeData = before is null ? null : JsonSerializer.Serialize(before, CoreHrAudit.JsonOptions),
        AfterData = after is null ? null : JsonSerializer.Serialize(after, CoreHrAudit.JsonOptions),
        Result = result,
        CorrelationId = correlationId,
        OccurredAt = occurredAt
    };

    public static string EntityId(long? userId) =>
        userId is { } id and > 0 ? id.ToString(CultureInfo.InvariantCulture) : "unknown";
}
