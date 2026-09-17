using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.Onboarding;

/// <summary>Filters of <c>GET /onboarding/tasks</c>. Data scope is applied server-side from the actor, never from the query.</summary>
public sealed record OnboardingTaskSearchQuery(
    long? EmployeeId,
    long? AssignedToUserId,
    OnboardingTaskStatus? Status,
    bool? Overdue,
    PageRequest Page);
