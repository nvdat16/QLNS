namespace Qlns.BusinessLogic.Modules.Contracts.Contracts;

/// <summary>
/// The probation_reviews row that activating a probation contract creates (EMP-06 step 1): pending, due
/// <see cref="ExpiryAlertPolicy.ProbationRedDays"/> days before the contract ends, assigned to the employee's
/// manager when that manager has a user account. Exactly one review exists per contract (ux_probation_review_contract).
/// </summary>
public sealed record ProbationReviewDraft(
    long EmployeeId,
    long ContractId,
    DateOnly ReviewDueDate,
    long? ReviewerUserId)
{
    public const string PendingStatus = "pending";

    public static ProbationReviewDraft For(Contract contract, ContractEmployee employee)
    {
        ArgumentNullException.ThrowIfNull(contract);
        ArgumentNullException.ThrowIfNull(employee);

        if (contract.ContractType != ContractType.Probation || contract.EndDate is not { } endDate)
        {
            throw new ArgumentException("Only a probation contract with an end date needs a probation review.", nameof(contract));
        }

        if (contract.Id <= 0)
        {
            throw new ArgumentException("The contract must be persisted before a review can reference it.", nameof(contract));
        }

        return new ProbationReviewDraft(
            contract.EmployeeId,
            contract.Id,
            endDate.AddDays(-ExpiryAlertPolicy.ProbationRedDays),
            employee.ManagerUserId);
    }
}
