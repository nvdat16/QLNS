using Qlns.BusinessLogic.Modules.CoreHr.Offboarding;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Xunit;
using static Qlns.BusinessLogic.UnitTests.Modules.CoreHr.Offboarding.OffboardingTestData;

namespace Qlns.BusinessLogic.UnitTests.Modules.CoreHr.Offboarding;

public sealed class OffboardingTaskTests
{
    [Fact]
    public void Start_FromPending_MovesToInProgressAndBumpsVersion()
    {
        var task = CreateTask(1, "it.devices", blocks: true, OffboardingTaskStatus.Pending, version: 3);

        task.Start(Now);

        Assert.Equal(OffboardingTaskStatus.InProgress, task.Status);
        Assert.Equal(4, task.Version);
        Assert.Equal(Now, task.UpdatedAt);
        Assert.Null(task.CompletedAt);
    }

    [Theory]
    [InlineData(OffboardingTaskStatus.InProgress)]
    [InlineData(OffboardingTaskStatus.Completed)]
    public void Start_FromNonPending_ThrowsInvalidTransition(OffboardingTaskStatus current)
    {
        var task = CreateTask(1, "it.devices", blocks: true, current);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => task.Start(Now));

        Assert.Equal(OffboardingTask.InvalidTransitionCode, exception.Code);
        Assert.Equal(current, task.Status);
        Assert.Equal(1, task.Version);
    }

    [Fact]
    public void Complete_FromInProgress_SetsCompletedAt()
    {
        var task = CreateTask(1, "it.devices", blocks: true, OffboardingTaskStatus.InProgress, version: 2);

        task.Complete(Now);

        Assert.Equal(OffboardingTaskStatus.Completed, task.Status);
        Assert.Equal(Now, task.CompletedAt);
        Assert.Equal(3, task.Version);
        Assert.False(task.IsBlockingOutstanding);
    }

    [Theory]
    [InlineData(OffboardingTaskStatus.Pending)]
    [InlineData(OffboardingTaskStatus.Completed)]
    public void Complete_FromPendingOrCompleted_ThrowsInvalidTransition(OffboardingTaskStatus current)
    {
        var task = CreateTask(1, "it.devices", blocks: true, current);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => task.Complete(Now));

        Assert.Equal(OffboardingTask.InvalidTransitionCode, exception.Code);
    }

    [Fact]
    public void Reopen_Completed_ReturnsToPendingAndClearsCompletedAt()
    {
        var task = CreateTask(1, "it.devices", blocks: true, OffboardingTaskStatus.Completed, version: 5);

        task.Reopen("Device returned damaged", Now);

        Assert.Equal(OffboardingTaskStatus.Pending, task.Status);
        Assert.Null(task.CompletedAt);
        Assert.Equal(6, task.Version);
        Assert.True(task.IsBlockingOutstanding);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Reopen_WithoutReason_ThrowsValidationOnReason(string? reason)
    {
        var task = CreateTask(1, "it.devices", blocks: true, OffboardingTaskStatus.Completed);

        var exception = Assert.Throws<CoreHrValidationException>(() => task.Reopen(reason, Now));

        Assert.True(exception.Errors.ContainsKey("reason"));
        Assert.Equal(OffboardingTaskStatus.Completed, task.Status);
    }

    [Theory]
    [InlineData(OffboardingTaskStatus.Pending)]
    [InlineData(OffboardingTaskStatus.InProgress)]
    public void Reopen_FromNonCompleted_ThrowsInvalidTransition(OffboardingTaskStatus current)
    {
        var task = CreateTask(1, "it.devices", blocks: true, current);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => task.Reopen("reason", Now));

        Assert.Equal(OffboardingTask.InvalidTransitionCode, exception.Code);
    }

    [Theory]
    [InlineData(OffboardingTaskAction.Start, OffboardingTaskStatus.Pending, OffboardingTaskStatus.InProgress)]
    [InlineData(OffboardingTaskAction.Complete, OffboardingTaskStatus.InProgress, OffboardingTaskStatus.Completed)]
    [InlineData(OffboardingTaskAction.Reopen, OffboardingTaskStatus.Completed, OffboardingTaskStatus.Pending)]
    public void Apply_RunsTransitionAndReturnsPreviousStatus(
        OffboardingTaskAction action,
        OffboardingTaskStatus from,
        OffboardingTaskStatus to)
    {
        var task = CreateTask(1, "it.devices", blocks: true, from);

        var previous = task.Apply(action, "reason", Now);

        Assert.Equal(from, previous);
        Assert.Equal(to, task.Status);
    }

    [Theory]
    [InlineData(true, OffboardingTaskStatus.Pending, true)]
    [InlineData(true, OffboardingTaskStatus.InProgress, true)]
    [InlineData(true, OffboardingTaskStatus.Completed, false)]
    [InlineData(false, OffboardingTaskStatus.Pending, false)]
    public void IsBlockingOutstanding_OnlyForIncompleteBlockingTasks(bool blocks, OffboardingTaskStatus status, bool expected)
    {
        var task = CreateTask(1, "it.devices", blocks, status);

        Assert.Equal(expected, task.IsBlockingOutstanding);
    }

    [Fact]
    public void Constructor_AllowsUnsavedPendingTaskOnly()
    {
        var unsaved = CreateTask(0, "it.devices", blocks: true, OffboardingTaskStatus.Pending);
        Assert.Equal(0, unsaved.Id);

        Assert.Throws<ArgumentOutOfRangeException>(() => CreateTask(0, "it.devices", true, OffboardingTaskStatus.InProgress));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateTask(1, "it.devices", true, OffboardingTaskStatus.Pending, version: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateTask(1, "it.devices", true, OffboardingTaskStatus.Pending, assignedToUserId: 0));
        Assert.Throws<ArgumentException>(() => CreateTask(1, " ", true, OffboardingTaskStatus.Pending));
    }
}
