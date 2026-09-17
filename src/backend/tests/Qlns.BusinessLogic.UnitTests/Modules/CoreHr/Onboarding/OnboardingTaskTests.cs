using Qlns.BusinessLogic.Modules.CoreHr.Onboarding;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Xunit;

namespace Qlns.BusinessLogic.UnitTests.Modules.CoreHr.Onboarding;

public sealed class OnboardingTaskTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 7, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Start_FromPending_MovesToInProgressAndBumpsVersion()
    {
        var task = CreateTask(OnboardingTaskStatus.Pending, version: 3);

        task.Start(Now);

        Assert.Equal(OnboardingTaskStatus.InProgress, task.Status);
        Assert.Equal(4, task.Version);
        Assert.Equal(Now, task.UpdatedAt);
        Assert.Null(task.CompletedAt);
    }

    [Theory]
    [InlineData(OnboardingTaskStatus.InProgress)]
    [InlineData(OnboardingTaskStatus.Completed)]
    public void Start_FromNonPending_ThrowsInvalidTransition(OnboardingTaskStatus current)
    {
        var task = CreateTask(current, version: 1);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => task.Start(Now));

        Assert.Equal("corehr.onboarding.invalid_transition", exception.Code);
        Assert.Equal(current, task.Status);
        Assert.Equal(1, task.Version);
    }

    [Fact]
    public void Complete_FromInProgress_SetsCompletedAt()
    {
        var task = CreateTask(OnboardingTaskStatus.InProgress, version: 2);

        task.Complete(Now);

        Assert.Equal(OnboardingTaskStatus.Completed, task.Status);
        Assert.Equal(Now, task.CompletedAt);
        Assert.Equal(3, task.Version);
        Assert.Equal(Now, task.UpdatedAt);
    }

    [Theory]
    [InlineData(OnboardingTaskStatus.Pending)]
    [InlineData(OnboardingTaskStatus.Completed)]
    public void Complete_FromPendingOrCompleted_ThrowsInvalidTransition(OnboardingTaskStatus current)
    {
        var task = CreateTask(current, version: 1);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => task.Complete(Now));

        Assert.Equal("corehr.onboarding.invalid_transition", exception.Code);
        Assert.Equal(1, task.Version);
    }

    [Fact]
    public void Reopen_Completed_ReturnsToPendingAndClearsCompletedAt()
    {
        var task = CreateTask(OnboardingTaskStatus.Completed, version: 5, completedAt: Now.AddDays(-1));

        task.Reopen("Laptop returned defective", Now);

        Assert.Equal(OnboardingTaskStatus.Pending, task.Status);
        Assert.Null(task.CompletedAt);
        Assert.Equal(6, task.Version);
        Assert.Equal(Now, task.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Reopen_WithoutReason_ThrowsValidationOnReason(string? reason)
    {
        var task = CreateTask(OnboardingTaskStatus.Completed, version: 1, completedAt: Now.AddDays(-1));

        var exception = Assert.Throws<CoreHrValidationException>(() => task.Reopen(reason, Now));

        Assert.True(exception.Errors.ContainsKey("reason"));
        Assert.Equal(OnboardingTaskStatus.Completed, task.Status);
        Assert.Equal(1, task.Version);
    }

    [Theory]
    [InlineData(OnboardingTaskStatus.Pending)]
    [InlineData(OnboardingTaskStatus.InProgress)]
    public void Reopen_FromNonCompleted_ThrowsInvalidTransition(OnboardingTaskStatus current)
    {
        var task = CreateTask(current, version: 1);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => task.Reopen("reason", Now));

        Assert.Equal("corehr.onboarding.invalid_transition", exception.Code);
        Assert.Equal(current, task.Status);
    }

    [Fact]
    public void IsOverdue_PastDueAndNotCompleted_IsTrue()
    {
        var task = CreateTask(OnboardingTaskStatus.Pending, version: 1, dueAt: Now.AddHours(-1));

        Assert.True(task.IsOverdue(Now));
    }

    [Fact]
    public void IsOverdue_PastDueButCompleted_IsFalse()
    {
        var task = CreateTask(OnboardingTaskStatus.Completed, version: 1, dueAt: Now.AddHours(-1), completedAt: Now.AddHours(-2));

        Assert.False(task.IsOverdue(Now));
    }

    [Fact]
    public void IsOverdue_FutureDue_IsFalse()
    {
        var task = CreateTask(OnboardingTaskStatus.InProgress, version: 1, dueAt: Now.AddHours(1));

        Assert.False(task.IsOverdue(Now));
    }

    [Fact]
    public void IsOverdue_NoDueDate_IsFalse()
    {
        var task = CreateTask(OnboardingTaskStatus.Pending, version: 1, dueAt: null);

        Assert.False(task.IsOverdue(Now));
    }

    [Fact]
    public void UpdateAssignment_Valid_AppliesTrimmedValuesAndReportsChangedFields()
    {
        var task = CreateTask(OnboardingTaskStatus.Pending, version: 1);
        var due = Now.AddDays(3);

        var changed = task.UpdateAssignment(new OnboardingTaskWrite("  Provision laptop  ", "  16GB RAM ", 99, due), Now);

        Assert.Equal("Provision laptop", task.TaskName);
        Assert.Equal("16GB RAM", task.Description);
        Assert.Equal(99, task.AssignedToUserId);
        Assert.Equal(due, task.DueAt);
        Assert.Equal(2, task.Version);
        Assert.Equal(Now, task.UpdatedAt);
        Assert.Equal(["taskName", "description", "assignedToUserId", "dueAt"], changed);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateAssignment_BlankName_ThrowsValidationOnTaskName(string name)
    {
        var task = CreateTask(OnboardingTaskStatus.Pending, version: 1);

        var exception = Assert.Throws<CoreHrValidationException>(() =>
            task.UpdateAssignment(new OnboardingTaskWrite(name, null, null, null), Now));

        Assert.True(exception.Errors.ContainsKey("taskName"));
        Assert.Equal("Create email", task.TaskName);
        Assert.Equal(1, task.Version);
    }

    [Fact]
    public void UpdateAssignment_TooLongNameAndDescriptionAndNegativeUser_ReportsAllFields()
    {
        var task = CreateTask(OnboardingTaskStatus.Pending, version: 1);

        var exception = Assert.Throws<CoreHrValidationException>(() => task.UpdateAssignment(
            new OnboardingTaskWrite(new string('a', 256), new string('b', 5001), -1, null),
            Now));

        Assert.True(exception.Errors.ContainsKey("taskName"));
        Assert.True(exception.Errors.ContainsKey("description"));
        Assert.True(exception.Errors.ContainsKey("assignedToUserId"));
    }

    [Fact]
    public void Constructor_NonPositiveIdOrVersion_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new OnboardingTask(
            0, 1, "it.email", "Create email", null, null, null, OnboardingTaskStatus.Pending, null, 1, Now, Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => new OnboardingTask(
            1, 1, "it.email", "Create email", null, null, null, OnboardingTaskStatus.Pending, null, 0, Now, Now));
    }

    private static OnboardingTask CreateTask(
        OnboardingTaskStatus status,
        long version,
        DateTimeOffset? dueAt = null,
        DateTimeOffset? completedAt = null) => new(
        id: 42,
        employeeId: 10,
        templateKey: "it.email",
        taskName: "Create email",
        description: null,
        assignedToUserId: 7,
        dueAt: dueAt,
        status: status,
        completedAt: completedAt,
        version: version,
        createdAt: Now.AddDays(-2),
        updatedAt: Now.AddMinutes(-5));
}
