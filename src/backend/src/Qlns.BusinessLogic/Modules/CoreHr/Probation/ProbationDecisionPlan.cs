using Qlns.BusinessLogic.Modules.CoreHr.Offboarding;

namespace Qlns.BusinessLogic.Modules.CoreHr.Probation;

/// <summary>
/// Side effects of a probation decision that the repository writes in the same transaction as the review
/// (EMP-06.2): the approved employee event mapped from the outcome and, for <c>terminated</c>, the draft
/// offboarding case (skipped by the repository when the employee already has an open case).
/// </summary>
public sealed record ProbationDecisionPlan(
    ApprovedEmployeeEvent EmployeeEvent,
    OffboardingCase? OffboardingCase);
