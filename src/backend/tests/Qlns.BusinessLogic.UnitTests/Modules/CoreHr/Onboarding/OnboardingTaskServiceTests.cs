using Qlns.BusinessLogic.Modules.CoreHr.Onboarding;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Xunit;

namespace Qlns.BusinessLogic.UnitTests.Modules.CoreHr.Onboarding;

public sealed class OnboardingTaskServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 7, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task TransitionAsync_Start_BumpsVersionAndSavesOnce()
    {
        var repository = new FakeOnboardingTaskRepository(CreateTask(OnboardingTaskStatus.Pending, version: 4));
        var service = new OnboardingTaskService(repository, new FixedTimeProvider(Now));

        var view = await service.TransitionAsync(
            Transition(OnboardingTaskAction.Start, expectedVersion: 4, Actor()),
            CancellationToken.None);

        Assert.Equal(OnboardingTaskStatus.InProgress, view.Task.Status);
        Assert.Equal(5, view.Task.Version);
        Assert.Equal(Now, view.Task.UpdatedAt);
        Assert.Equal(1, repository.SaveTransitionCalls);
        Assert.Equal(OnboardingTaskStatus.Pending, repository.LastPreviousStatus);
        Assert.Equal(4, repository.LastExpectedVersion);
        Assert.Null(repository.LastReason);
    }

    [Fact]
    public async Task TransitionAsync_Complete_SetsCompletedAtAndSavesOnce()
    {
        var repository = new FakeOnboardingTaskRepository(CreateTask(OnboardingTaskStatus.InProgress, version: 2));
        var service = new OnboardingTaskService(repository, new FixedTimeProvider(Now));

        var view = await service.TransitionAsync(
            Transition(OnboardingTaskAction.Complete, expectedVersion: 2, Actor()),
            CancellationToken.None);

        Assert.Equal(OnboardingTaskStatus.Completed, view.Task.Status);
        Assert.Equal(Now, view.Task.CompletedAt);
        Assert.False(view.Overdue);
        Assert.Equal(1, repository.SaveTransitionCalls);
        Assert.Equal(OnboardingTaskStatus.InProgress, repository.LastPreviousStatus);
    }

    [Fact]
    public async Task TransitionAsync_StaleVersion_ThrowsConcurrencyWithoutSaving()
    {
        var repository = new FakeOnboardingTaskRepository(CreateTask(OnboardingTaskStatus.Pending, version: 3));
        var service = new OnboardingTaskService(repository, new FixedTimeProvider(Now));

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => service.TransitionAsync(
            Transition(OnboardingTaskAction.Start, expectedVersion: 2, Actor()),
            CancellationToken.None));

        Assert.Equal(0, repository.SaveTransitionCalls);
    }

    [Fact]
    public async Task TransitionAsync_SaveLosesRace_ThrowsConcurrency()
    {
        var repository = new FakeOnboardingTaskRepository(CreateTask(OnboardingTaskStatus.Pending, version: 1))
        {
            SaveSucceeds = false
        };
        var service = new OnboardingTaskService(repository, new FixedTimeProvider(Now));

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => service.TransitionAsync(
            Transition(OnboardingTaskAction.Start, expectedVersion: 1, Actor()),
            CancellationToken.None));

        Assert.Equal(1, repository.SaveTransitionCalls);
    }

    [Fact]
    public async Task TransitionAsync_InvalidWorkflowStep_ThrowsBusinessRuleWithoutSaving()
    {
        var repository = new FakeOnboardingTaskRepository(CreateTask(OnboardingTaskStatus.Pending, version: 1));
        var service = new OnboardingTaskService(repository, new FixedTimeProvider(Now));

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() => service.TransitionAsync(
            Transition(OnboardingTaskAction.Complete, expectedVersion: 1, Actor()),
            CancellationToken.None));

        Assert.Equal("corehr.onboarding.invalid_transition", exception.Code);
        Assert.Equal(0, repository.SaveTransitionCalls);
    }

    [Fact]
    public async Task TransitionAsync_ReopenWithoutPermission_ThrowsForbiddenWithoutSaving()
    {
        var repository = new FakeOnboardingTaskRepository(
            CreateTask(OnboardingTaskStatus.Completed, version: 6, completedAt: Now.AddDays(-1)));
        var service = new OnboardingTaskService(repository, new FixedTimeProvider(Now));

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() => service.TransitionAsync(
            Transition(OnboardingTaskAction.Reopen, expectedVersion: 6, Actor(CoreHrPermissions.OnboardingManage), "Wrong device"),
            CancellationToken.None));

        Assert.Equal("corehr.onboarding.reopen_forbidden", exception.Code);
        Assert.Equal(0, repository.SaveTransitionCalls);
    }

    [Fact]
    public async Task TransitionAsync_ReopenWithPermission_ReturnsToPendingAndPassesReason()
    {
        var repository = new FakeOnboardingTaskRepository(
            CreateTask(OnboardingTaskStatus.Completed, version: 6, completedAt: Now.AddDays(-1)));
        var service = new OnboardingTaskService(repository, new FixedTimeProvider(Now));

        var view = await service.TransitionAsync(
            Transition(
                OnboardingTaskAction.Reopen,
                expectedVersion: 6,
                Actor(CoreHrPermissions.OnboardingManage, CoreHrPermissions.OnboardingReopen),
                "  Wrong device  "),
            CancellationToken.None);

        Assert.Equal(OnboardingTaskStatus.Pending, view.Task.Status);
        Assert.Null(view.Task.CompletedAt);
        Assert.Equal(7, view.Task.Version);
        Assert.Equal(1, repository.SaveTransitionCalls);
        Assert.Equal(OnboardingTaskStatus.Completed, repository.LastPreviousStatus);
        Assert.Equal("Wrong device", repository.LastReason);
    }

    [Fact]
    public async Task TransitionAsync_ReopenWithoutReason_ThrowsValidationWithoutSaving()
    {
        var repository = new FakeOnboardingTaskRepository(
            CreateTask(OnboardingTaskStatus.Completed, version: 6, completedAt: Now.AddDays(-1)));
        var service = new OnboardingTaskService(repository, new FixedTimeProvider(Now));

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() => service.TransitionAsync(
            Transition(OnboardingTaskAction.Reopen, expectedVersion: 6, Actor(CoreHrPermissions.OnboardingReopen), null),
            CancellationToken.None));

        Assert.True(exception.Errors.ContainsKey("reason"));
        Assert.Equal(0, repository.SaveTransitionCalls);
    }

    [Fact]
    public async Task TransitionAsync_TaskOutsideScopeOrMissing_ThrowsNotFound()
    {
        var repository = new FakeOnboardingTaskRepository(task: null);
        var service = new OnboardingTaskService(repository, new FixedTimeProvider(Now));

        var exception = await Assert.ThrowsAsync<CoreHrNotFoundException>(() => service.TransitionAsync(
            Transition(OnboardingTaskAction.Start, expectedVersion: 1, Actor()),
            CancellationToken.None));

        Assert.Equal("Onboarding task", exception.Resource);
        Assert.Equal(42, exception.Id);
        Assert.Equal(0, repository.SaveTransitionCalls);
    }

    [Fact]
    public async Task UpdateAssignmentAsync_UnknownUser_ThrowsValidationWithoutSaving()
    {
        var repository = new FakeOnboardingTaskRepository(CreateTask(OnboardingTaskStatus.Pending, version: 1))
        {
            ExistingUserIds = [7]
        };
        var service = new OnboardingTaskService(repository, new FixedTimeProvider(Now));

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() => service.UpdateAssignmentAsync(
            new UpdateOnboardingTaskCommand(42, 1, new OnboardingTaskWrite("Create email", null, 999, null), Actor()),
            CancellationToken.None));

        Assert.True(exception.Errors.ContainsKey("assignedToUserId"));
        Assert.Equal(0, repository.SaveAssignmentCalls);
    }

    [Fact]
    public async Task UpdateAssignmentAsync_Valid_SavesOnceWithChangedFields()
    {
        var repository = new FakeOnboardingTaskRepository(CreateTask(OnboardingTaskStatus.Pending, version: 1))
        {
            ExistingUserIds = [7, 8]
        };
        var service = new OnboardingTaskService(repository, new FixedTimeProvider(Now));
        var due = Now.AddDays(2);

        var view = await service.UpdateAssignmentAsync(
            new UpdateOnboardingTaskCommand(42, 1, new OnboardingTaskWrite("Create email", "Use corporate domain", 8, due), Actor()),
            CancellationToken.None);

        Assert.Equal(2, view.Task.Version);
        Assert.Equal(8, view.Task.AssignedToUserId);
        Assert.Equal(due, view.Task.DueAt);
        Assert.False(view.Overdue);
        Assert.Equal(1, repository.SaveAssignmentCalls);
        Assert.Equal(1, repository.LastExpectedVersion);
        Assert.Equal(["description", "assignedToUserId", "dueAt"], repository.LastChangedFields);
    }

    [Fact]
    public async Task UpdateAssignmentAsync_StaleVersion_ThrowsConcurrencyWithoutUserLookupOrSave()
    {
        var repository = new FakeOnboardingTaskRepository(CreateTask(OnboardingTaskStatus.Pending, version: 3));
        var service = new OnboardingTaskService(repository, new FixedTimeProvider(Now));

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => service.UpdateAssignmentAsync(
            new UpdateOnboardingTaskCommand(42, 2, new OnboardingTaskWrite("Create email", null, 8, null), Actor()),
            CancellationToken.None));

        Assert.Equal(0, repository.UserExistsCalls);
        Assert.Equal(0, repository.SaveAssignmentCalls);
    }

    [Fact]
    public async Task SearchAsync_PassesNowToRepositoryAndComputesOverdue()
    {
        var overdueTask = CreateTask(OnboardingTaskStatus.Pending, version: 1, dueAt: Now.AddHours(-1), id: 1);
        var onTimeTask = CreateTask(OnboardingTaskStatus.Pending, version: 1, dueAt: Now.AddHours(1), id: 2);
        var completedLate = CreateTask(OnboardingTaskStatus.Completed, version: 1, dueAt: Now.AddHours(-3), completedAt: Now.AddHours(-2), id: 3);
        var repository = new FakeOnboardingTaskRepository(task: null)
        {
            SearchResult = new PagedResult<OnboardingTask>([overdueTask, onTimeTask, completedLate], 1, 20, 3)
        };
        var service = new OnboardingTaskService(repository, new FixedTimeProvider(Now));
        var query = new OnboardingTaskSearchQuery(null, null, null, null, PageRequest.Default);

        var result = await service.SearchAsync(query, Actor(), CancellationToken.None);

        Assert.Equal(Now, repository.LastSearchNow);
        Assert.Same(query, repository.LastSearchQuery);
        Assert.Equal(3, result.TotalItems);
        Assert.Equal([true, false, false], result.Items.Select(view => view.Overdue));
        Assert.Equal([1L, 2L, 3L], result.Items.Select(view => view.Task.Id));
    }

    private static CoreHrActor Actor(params string[] permissions) => new(
        UserId: 7,
        EmployeeId: null,
        DataScope: CoreHrDataScope.Organization,
        Permissions: permissions.Length == 0
            ? new HashSet<string> { CoreHrPermissions.OnboardingManage }
            : permissions.ToHashSet(StringComparer.Ordinal),
        CorrelationId: "test-correlation");

    private static TransitionOnboardingTaskCommand Transition(
        OnboardingTaskAction action,
        long expectedVersion,
        CoreHrActor actor,
        string? reason = null) => new(42, action, expectedVersion, reason, actor);

    private static OnboardingTask CreateTask(
        OnboardingTaskStatus status,
        long version,
        DateTimeOffset? dueAt = null,
        DateTimeOffset? completedAt = null,
        long id = 42) => new(
        id: id,
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

    private sealed class FakeOnboardingTaskRepository(OnboardingTask? task) : IOnboardingTaskRepository
    {
        public bool SaveSucceeds { get; init; } = true;
        public HashSet<long> ExistingUserIds { get; init; } = [];
        public PagedResult<OnboardingTask>? SearchResult { get; init; }

        public int SaveTransitionCalls { get; private set; }
        public int SaveAssignmentCalls { get; private set; }
        public int UserExistsCalls { get; private set; }
        public OnboardingTaskStatus? LastPreviousStatus { get; private set; }
        public long? LastExpectedVersion { get; private set; }
        public string? LastReason { get; private set; }
        public IReadOnlyList<string>? LastChangedFields { get; private set; }
        public DateTimeOffset? LastSearchNow { get; private set; }
        public OnboardingTaskSearchQuery? LastSearchQuery { get; private set; }

        public Task<PagedResult<OnboardingTask>> SearchAsync(
            OnboardingTaskSearchQuery query,
            CoreHrActor actor,
            DateTimeOffset now,
            CancellationToken cancellationToken)
        {
            LastSearchQuery = query;
            LastSearchNow = now;
            return Task.FromResult(SearchResult ?? PagedResult<OnboardingTask>.Empty(query.Page));
        }

        public Task<OnboardingTask?> GetByIdAsync(long taskId, CoreHrActor actor, CancellationToken cancellationToken) =>
            Task.FromResult(task);

        public Task<bool> UserExistsAsync(long userId, CancellationToken cancellationToken)
        {
            UserExistsCalls++;
            return Task.FromResult(ExistingUserIds.Contains(userId));
        }

        public Task<bool> SaveTransitionAsync(
            OnboardingTask saved,
            OnboardingTaskStatus previousStatus,
            long expectedVersion,
            string? reason,
            CoreHrActor actor,
            CancellationToken cancellationToken)
        {
            SaveTransitionCalls++;
            LastPreviousStatus = previousStatus;
            LastExpectedVersion = expectedVersion;
            LastReason = reason;
            return Task.FromResult(SaveSucceeds);
        }

        public Task<bool> SaveAssignmentAsync(
            OnboardingTask saved,
            long expectedVersion,
            IReadOnlyList<string> changedFields,
            CoreHrActor actor,
            CancellationToken cancellationToken)
        {
            SaveAssignmentCalls++;
            LastExpectedVersion = expectedVersion;
            LastChangedFields = changedFields;
            return Task.FromResult(SaveSucceeds);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
