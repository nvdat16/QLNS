using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.Offboarding;

/// <summary>Filters of <c>GET /offboarding/cases</c>. Data scope is applied server-side from the actor, never from the query.</summary>
public sealed record OffboardingCaseSearchQuery(
    OffboardingCaseStatus? Status,
    long? DepartmentId,
    PageRequest Page);

/// <summary>Filters of <c>GET /offboarding/cases/{caseId}/tasks</c>.</summary>
public sealed record OffboardingTaskFilter(
    OffboardingTaskCategory? Category,
    bool? BlockingOnly)
{
    public static OffboardingTaskFilter None { get; } = new(null, null);
}
