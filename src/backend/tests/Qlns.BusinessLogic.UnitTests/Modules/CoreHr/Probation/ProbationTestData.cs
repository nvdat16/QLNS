using Qlns.BusinessLogic.Modules.CoreHr.Probation;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.UnitTests.Modules.CoreHr.Probation;

internal static class ProbationTestData
{
    public static readonly DateTimeOffset Now = new(2026, 9, 15, 7, 0, 0, TimeSpan.Zero);
    public static readonly DateOnly Today = new(2026, 9, 15);
    public static readonly DateOnly DueDate = new(2026, 9, 30);

    public const long ReviewId = 300;
    public const long EmployeeId = 42;
    public const long ContractId = 700;
    public const long DepartmentId = 10;
    public const long ReviewerUserId = 15;
    public const long HrManagerUserId = 7;

    public static CoreHrActor Actor(
        long userId = HrManagerUserId,
        CoreHrDataScope? scope = null,
        long? employeeId = null,
        params string[] permissions) => new(
        UserId: userId,
        EmployeeId: employeeId,
        DataScope: scope ?? CoreHrDataScope.Organization,
        Permissions: permissions.ToHashSet(StringComparer.Ordinal),
        CorrelationId: "test-correlation");

    public static ProbationReview CreateReview(
        ProbationReviewStatus status,
        long version = 2,
        ProbationOutcome? outcome = null,
        string? improvements = null,
        long? reviewerUserId = ReviewerUserId,
        long? employeeEventId = null,
        DateOnly? reviewDueDate = null,
        long id = ReviewId)
    {
        var decided = status == ProbationReviewStatus.Decided;
        var assessed = status is ProbationReviewStatus.InReview or ProbationReviewStatus.Decided;
        return new ProbationReview(
            id,
            EmployeeId,
            ContractId,
            reviewDueDate ?? DueDate,
            reviewerUserId,
            status,
            outcome ?? (assessed ? ProbationOutcome.Confirmed : null),
            assessed ? 4.0m : null,
            assessed ? "Fast learner" : null,
            improvements,
            decided ? new DateOnly(2026, 10, 1) : null,
            decided ? HrManagerUserId : null,
            decided ? Now.AddDays(-1) : null,
            decided ? employeeEventId ?? 900 : employeeEventId,
            version,
            createdAt: Now.AddDays(-30),
            updatedAt: Now.AddDays(-1));
    }

    public static ProbationReviewWrite Write(
        decimal? score = 4.5m,
        string? strengths = "Fast learner",
        string? improvements = "Time management",
        string? recommendedOutcome = "confirmed") => new(score, strengths, improvements, recommendedOutcome);

    public static ProbationDecision Decision(
        string? outcome = "confirmed",
        DateOnly? effectiveDate = null,
        string? reason = null) => new(outcome, effectiveDate ?? new DateOnly(2026, 10, 1), reason);
}

internal sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => value;
}
