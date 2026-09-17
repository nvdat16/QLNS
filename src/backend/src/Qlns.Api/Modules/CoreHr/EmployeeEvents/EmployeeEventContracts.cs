using System.ComponentModel.DataAnnotations;
using System.Text.Json.Nodes;
using Qlns.BusinessLogic.Modules.CoreHr.EmployeeEvents;

namespace Qlns.Api.Modules.CoreHr.EmployeeEvents;

/// <summary>OpenAPI EmployeeEventWrite.</summary>
public sealed record EmployeeEventWriteRequest(
    [Required, MaxLength(50)] string EventType,
    DateOnly EffectiveDate,
    [Required] JsonObject BeforeData,
    [Required] JsonObject AfterData,
    [Required, MaxLength(EmployeeEvent.ReasonMaxLength)] string Reason,
    long? CompensatesEventId)
{
    public EmployeeEventWrite ToWrite() => new(EventType, EffectiveDate, BeforeData, AfterData, Reason, CompensatesEventId);
}

/// <summary>OpenAPI ReasonRequest (optional body of the transition endpoint).</summary>
public sealed record ReasonRequest([MaxLength(1000)] string? Reason);

/// <summary>OpenAPI EmployeeEvent.</summary>
public sealed record EmployeeEventResponse(
    long Id,
    long EmployeeId,
    string EventType,
    DateOnly EffectiveDate,
    JsonObject BeforeData,
    JsonObject AfterData,
    string Reason,
    long? CompensatesEventId,
    string Status,
    long CreatedBy,
    long? ApprovedBy,
    DateTimeOffset? AppliedAt,
    long Version)
{
    public static EmployeeEventResponse From(EmployeeEvent employeeEvent) => new(
        employeeEvent.Id,
        employeeEvent.EmployeeId,
        employeeEvent.EventType.ToContract(),
        employeeEvent.EffectiveDate,
        employeeEvent.BeforeData,
        employeeEvent.AfterData,
        employeeEvent.Reason,
        employeeEvent.CompensatesEventId,
        employeeEvent.Status.ToContract(),
        employeeEvent.CreatedBy,
        employeeEvent.ApprovedBy,
        employeeEvent.AppliedAt,
        employeeEvent.Version);
}
