using System.Text.Json.Nodes;
using Qlns.BusinessLogic.Modules.CoreHr.EmployeeEvents;

namespace Qlns.BusinessLogic.Modules.Contracts.Addenda;

/// <summary>
/// The already-approved employee event that an effective addendum raises for the contract's employee
/// (CON-03.1 scenario 3). The Effective-Date Worker applies it on <see cref="EffectiveDate"/>; the Contracts module
/// never writes <c>employees</c> directly.
/// </summary>
public sealed record MasterDataEventDraft(
    long EmployeeId,
    EmployeeEventType EventType,
    DateOnly EffectiveDate,
    JsonObject BeforeData,
    JsonObject AfterData,
    string Reason)
{
    /// <summary>
    /// Maps an addendum to its event, or null when it changes no master data. Type priority when several
    /// master-data terms change: positionId → promotion, departmentId → transfer, salary → salary_adjustment.
    /// before/after carry only the master-data subset of the addendum terms.
    /// </summary>
    public static MasterDataEventDraft? From(ContractAddendum addendum, long employeeId)
    {
        ArgumentNullException.ThrowIfNull(addendum);

        if (employeeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(employeeId), "Persistent identifiers must be positive.");
        }

        var after = AddendumTermRules.MasterDataSubset(addendum.AfterTerms);
        if (after.Count == 0)
        {
            return null;
        }

        var eventType = after.ContainsKey(AddendumTermRules.PositionId) ? EmployeeEventType.Promotion
            : after.ContainsKey(AddendumTermRules.DepartmentId) ? EmployeeEventType.Transfer
            : EmployeeEventType.SalaryAdjustment;

        return new MasterDataEventDraft(
            employeeId,
            eventType,
            addendum.EffectiveDate,
            AddendumTermRules.MasterDataSubset(addendum.BeforeTerms),
            after,
            addendum.Reason);
    }
}
