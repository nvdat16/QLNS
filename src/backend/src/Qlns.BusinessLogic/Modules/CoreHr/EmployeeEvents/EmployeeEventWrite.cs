using System.Text.Json.Nodes;

namespace Qlns.BusinessLogic.Modules.CoreHr.EmployeeEvents;

/// <summary>Client payload of EmployeeEventWrite (OpenAPI). Validated by <see cref="EmployeeEvent.CreateDraft"/>.</summary>
public sealed record EmployeeEventWrite(
    string? EventType,
    DateOnly EffectiveDate,
    JsonObject? BeforeData,
    JsonObject? AfterData,
    string? Reason,
    long? CompensatesEventId);
