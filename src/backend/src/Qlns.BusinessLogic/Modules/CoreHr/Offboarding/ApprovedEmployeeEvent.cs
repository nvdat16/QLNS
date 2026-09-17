using System.Text.Json.Nodes;
using Qlns.BusinessLogic.Modules.CoreHr.EmployeeEvents;

namespace Qlns.BusinessLogic.Modules.CoreHr.Offboarding;

/// <summary>
/// An employee status event that a lifecycle decision (probation outcome, offboarding completion) wants
/// inserted in status <c>approved</c> inside the same transaction as the decision. Only <c>status</c> is
/// changed, so the Effective-Date Worker (<see cref="EmployeeEvent.ApplyTo"/>) is the single writer of
/// <c>employees.status</c>; nothing here touches master data directly.
/// </summary>
public sealed record ApprovedEmployeeEvent(
    long EmployeeId,
    EmployeeEventType EventType,
    DateOnly EffectiveDate,
    string BeforeStatus,
    string AfterStatus,
    string Reason)
{
    public static ApprovedEmployeeEvent StatusChange(
        long employeeId,
        EmployeeEventType eventType,
        DateOnly effectiveDate,
        string beforeStatus,
        string afterStatus,
        string reason)
    {
        if (employeeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(employeeId), "Persistent identifiers must be positive.");
        }

        if (!EmployeeStatusValues.IsValid(beforeStatus))
        {
            throw new ArgumentException($"'{beforeStatus}' is not an employee status.", nameof(beforeStatus));
        }

        if (!EmployeeStatusValues.IsValid(afterStatus))
        {
            throw new ArgumentException($"'{afterStatus}' is not an employee status.", nameof(afterStatus));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        return new ApprovedEmployeeEvent(employeeId, eventType, effectiveDate, beforeStatus, afterStatus, reason);
    }

    /// <summary>employee_events.before_data payload.</summary>
    public JsonObject BeforeData => new() { ["status"] = BeforeStatus };

    /// <summary>employee_events.after_data payload.</summary>
    public JsonObject AfterData => new() { ["status"] = AfterStatus };

    /// <summary>Names of the master-data fields this event changes (audit payload).</summary>
    public IReadOnlyList<string> ChangedFields => ["status"];
}
