using System.Globalization;
using System.Text.Json;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.DataAccess.Modules.Recruitment.Applications;

namespace Qlns.DataAccess.Modules.CoreHr.Shared;

/// <summary>
/// Builds audit_logs rows for Core HR repositories. Repositories add the returned entity to the
/// same DbContext transaction that writes the business change, so audit and change commit together.
/// Never pass restricted payloads (identity numbers, bank data, salary) into before/after.
/// </summary>
public static class CoreHrAudit
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static AuditLogEntity Entry(
        CoreHrActor actor,
        string action,
        string entityType,
        long entityId,
        object? before,
        object? after,
        DateTimeOffset occurredAt,
        string result = "succeeded") => new()
    {
        ActorUserId = actor.UserId > 0 ? actor.UserId : null,
        Action = action,
        EntityType = entityType,
        EntityId = entityId.ToString(CultureInfo.InvariantCulture),
        BeforeData = before is null ? null : JsonSerializer.Serialize(before, JsonOptions),
        AfterData = after is null ? null : JsonSerializer.Serialize(after, JsonOptions),
        Result = result,
        CorrelationId = actor.CorrelationId,
        OccurredAt = occurredAt
    };
}
