namespace Qlns.BusinessLogic.Modules.CoreHr.Probation;

/// <summary>Repository read model: the review plus the department of its employee (data scope).</summary>
public sealed record ProbationReviewEntry(ProbationReview Review, long EmployeeDepartmentId);

/// <summary>Service result: the review plus the time-dependent <c>overdue</c> flag of OpenAPI ProbationReview.</summary>
public sealed record ProbationReviewView(ProbationReview Review, bool Overdue);
