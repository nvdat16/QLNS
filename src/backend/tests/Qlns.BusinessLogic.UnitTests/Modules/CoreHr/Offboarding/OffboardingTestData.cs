using Qlns.BusinessLogic.Modules.CoreHr.Offboarding;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.UnitTests.Modules.CoreHr.Offboarding;

internal static class OffboardingTestData
{
    public static readonly DateTimeOffset Now = new(2026, 9, 15, 7, 0, 0, TimeSpan.Zero);
    public static readonly DateOnly Today = new(2026, 9, 15);
    public static readonly DateOnly LastWorkingDate = new(2026, 10, 15);

    public const long CaseId = 500;
    public const long EmployeeId = 42;
    public const long DepartmentId = 10;
    public const long ActorUserId = 7;
    public const long ManagerUserId = 77;

    public static CoreHrActor Actor(
        CoreHrDataScope? scope = null,
        long? employeeId = null,
        long userId = ActorUserId,
        params string[] permissions) => new(
        UserId: userId,
        EmployeeId: employeeId,
        DataScope: scope ?? CoreHrDataScope.Organization,
        Permissions: permissions.ToHashSet(StringComparer.Ordinal),
        CorrelationId: "test-correlation");

    public static OffboardingCase CreateCase(
        OffboardingCaseStatus status,
        long version = 2,
        FinalSettlementStatus settlement = FinalSettlementStatus.Pending,
        long? employeeEventId = null,
        DateOnly? noticeReceivedOn = null,
        long? handoverToEmployeeId = 43,
        long id = CaseId,
        long employeeId = EmployeeId) => new(
        id,
        employeeId,
        employeeEventId,
        SeparationType.Resignation,
        noticeReceivedOn,
        LastWorkingDate,
        handoverToEmployeeId,
        exitInterviewAt: null,
        settlement,
        status,
        "Moving abroad",
        createdBy: 5,
        approvedBy: status is OffboardingCaseStatus.Approved or OffboardingCaseStatus.InProgress or OffboardingCaseStatus.Completed ? 9 : null,
        approvedAt: status is OffboardingCaseStatus.Approved or OffboardingCaseStatus.InProgress or OffboardingCaseStatus.Completed ? Now.AddDays(-1) : null,
        completedAt: status == OffboardingCaseStatus.Completed ? Now.AddHours(-1) : null,
        version,
        createdAt: Now.AddDays(-2),
        updatedAt: Now.AddDays(-1));

    public static OffboardingTask CreateTask(
        long id,
        string templateKey,
        bool blocks,
        OffboardingTaskStatus status,
        long? assignedToUserId = null,
        long version = 1,
        long caseId = CaseId) => new(
        id,
        caseId,
        templateKey,
        OffboardingTaskCategory.It,
        templateKey,
        description: null,
        assignedToUserId,
        OffboardingChecklistTemplate.DueAt(LastWorkingDate),
        blocks,
        status,
        completedAt: status == OffboardingTaskStatus.Completed ? Now.AddHours(-2) : null,
        version,
        createdAt: Now.AddDays(-1),
        updatedAt: Now.AddHours(-3));

    public static OffboardingCaseWrite Write(
        long employeeId = EmployeeId,
        string? separationType = "resignation",
        DateOnly? noticeReceivedOn = null,
        DateOnly? lastWorkingDate = null,
        long? handoverToEmployeeId = 43,
        string? reason = "Moving abroad") => new(
        employeeId,
        separationType,
        noticeReceivedOn,
        lastWorkingDate ?? LastWorkingDate,
        handoverToEmployeeId,
        reason);
}

internal sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => value;
}
