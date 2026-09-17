using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.Probation;

public sealed record SubmitProbationReviewCommand(
    long EmployeeId,
    long ExpectedVersion,
    ProbationReviewWrite Write,
    CoreHrActor Actor);

public sealed record TransitionProbationReviewCommand(
    long ReviewId,
    ProbationReviewAction Action,
    long ExpectedVersion,
    ProbationDecision Decision,
    CoreHrActor Actor);

/// <summary>Filters of <c>GET /probation-reviews</c>. Data scope is applied server-side from the actor, never from the query.</summary>
public sealed record ProbationReviewSearchQuery(
    ProbationReviewStatus? Status,
    bool? Overdue,
    long? DepartmentId,
    PageRequest Page);
