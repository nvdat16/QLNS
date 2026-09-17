using Qlns.BusinessLogic.Modules.CoreHr.Offboarding;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Xunit;
using static Qlns.BusinessLogic.UnitTests.Modules.CoreHr.Offboarding.OffboardingTestData;

namespace Qlns.BusinessLogic.UnitTests.Modules.CoreHr.Offboarding;

public sealed class OffboardingTaskServiceTests
{
    private const long TaskId = 61;
    private const long AssigneeUserId = 88;

    [Fact]
    public async Task ListAsync_VisibleCase_PassesFilterToRepository()
    {
        var tasks = new FakeOffboardingTaskRepository();
        var cases = new FakeCaseRepository(CreateCase(OffboardingCaseStatus.Approved));
        var service = Service(tasks, cases);
        var filter = new OffboardingTaskFilter(OffboardingTaskCategory.It, true);

        await service.ListAsync(CaseId, filter, Actor(permissions: OffboardingPermissions.Read), CancellationToken.None);

        Assert.Same(filter, tasks.LastFilter);
        Assert.Equal(CaseId, tasks.LastListedCaseId);
    }

    [Fact]
    public async Task ListAsync_MissingOrOutOfScopeCase_ThrowsNotFoundWithoutListing()
    {
        var tasks = new FakeOffboardingTaskRepository();
        var service = Service(tasks, new FakeCaseRepository(CreateCase(OffboardingCaseStatus.Approved)));

        await Assert.ThrowsAsync<CoreHrNotFoundException>(() => service.ListAsync(
            999, OffboardingTaskFilter.None, Actor(), CancellationToken.None));
        await Assert.ThrowsAsync<CoreHrNotFoundException>(() => service.ListAsync(
            CaseId, OffboardingTaskFilter.None, Actor(CoreHrDataScope.Departments(99)), CancellationToken.None));

        Assert.Null(tasks.LastListedCaseId);
    }

    [Fact]
    public async Task TransitionAsync_Start_SavesOnceWithPreviousStatus()
    {
        var tasks = new FakeOffboardingTaskRepository(Entry(OffboardingTaskStatus.Pending, version: 2));
        var service = Service(tasks);

        var task = await service.TransitionAsync(Transition(OffboardingTaskAction.Start, 2, Writer()), CancellationToken.None);

        Assert.Equal(OffboardingTaskStatus.InProgress, task.Status);
        Assert.Equal(3, task.Version);
        Assert.Equal(1, tasks.SaveCalls);
        Assert.Equal(OffboardingTaskStatus.Pending, tasks.LastPreviousStatus);
        Assert.Equal(2, tasks.LastExpectedVersion);
        Assert.Null(tasks.LastReason);
    }

    [Fact]
    public async Task TransitionAsync_AssigneeOutsideScope_MayWorkOwnTask()
    {
        var tasks = new FakeOffboardingTaskRepository(Entry(OffboardingTaskStatus.Pending, version: 1, assignedToUserId: AssigneeUserId));
        var service = Service(tasks);
        var assignee = Actor(CoreHrDataScope.Departments(99), userId: AssigneeUserId, permissions: OffboardingPermissions.Write);

        var task = await service.TransitionAsync(Transition(OffboardingTaskAction.Start, 1, assignee), CancellationToken.None);

        Assert.Equal(OffboardingTaskStatus.InProgress, task.Status);
    }

    [Fact]
    public async Task TransitionAsync_AssigneeWithoutWritePermission_ThrowsForbidden()
    {
        var tasks = new FakeOffboardingTaskRepository(Entry(OffboardingTaskStatus.Pending, version: 1, assignedToUserId: AssigneeUserId));
        var service = Service(tasks);
        var assignee = Actor(CoreHrDataScope.Departments(99), userId: AssigneeUserId, permissions: OffboardingPermissions.Read);

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() =>
            service.TransitionAsync(Transition(OffboardingTaskAction.Start, 1, assignee), CancellationToken.None));

        Assert.Equal(OffboardingCaseService.WriteForbiddenCode, exception.Code);
        Assert.Equal(0, tasks.SaveCalls);
    }

    [Fact]
    public async Task TransitionAsync_OutOfScopeAndNotAssignee_ThrowsNotFound()
    {
        var tasks = new FakeOffboardingTaskRepository(Entry(OffboardingTaskStatus.Pending, version: 1, assignedToUserId: AssigneeUserId));
        var service = Service(tasks);

        var exception = await Assert.ThrowsAsync<CoreHrNotFoundException>(() => service.TransitionAsync(
            Transition(OffboardingTaskAction.Start, 1, Writer(CoreHrDataScope.Departments(99))), CancellationToken.None));

        Assert.Equal(OffboardingTaskService.ResourceName, exception.Resource);
        Assert.Equal(TaskId, exception.Id);
    }

    [Fact]
    public async Task TransitionAsync_MissingTask_ThrowsNotFound()
    {
        var service = Service(new FakeOffboardingTaskRepository());

        await Assert.ThrowsAsync<CoreHrNotFoundException>(() => service.TransitionAsync(
            Transition(OffboardingTaskAction.Start, 1, Writer()), CancellationToken.None));
    }

    [Fact]
    public async Task TransitionAsync_StaleVersion_ThrowsConcurrencyWithoutSave()
    {
        var tasks = new FakeOffboardingTaskRepository(Entry(OffboardingTaskStatus.Pending, version: 3));
        var service = Service(tasks);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => service.TransitionAsync(
            Transition(OffboardingTaskAction.Start, 2, Writer()), CancellationToken.None));

        Assert.Equal(0, tasks.SaveCalls);
    }

    [Fact]
    public async Task TransitionAsync_SaveLosesRace_ThrowsConcurrency()
    {
        var tasks = new FakeOffboardingTaskRepository(Entry(OffboardingTaskStatus.Pending, version: 1)) { SaveSucceeds = false };
        var service = Service(tasks);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => service.TransitionAsync(
            Transition(OffboardingTaskAction.Start, 1, Writer()), CancellationToken.None));

        Assert.Equal(1, tasks.SaveCalls);
    }

    [Fact]
    public async Task TransitionAsync_ReopenWithoutApprove_ThrowsForbiddenWithoutSave()
    {
        var tasks = new FakeOffboardingTaskRepository(Entry(OffboardingTaskStatus.Completed, version: 5));
        var service = Service(tasks);

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() => service.TransitionAsync(
            Transition(OffboardingTaskAction.Reopen, 5, Writer(), "Badge not returned"), CancellationToken.None));

        Assert.Equal(OffboardingTaskService.ReopenForbiddenCode, exception.Code);
        Assert.Equal(0, tasks.SaveCalls);
    }

    [Fact]
    public async Task TransitionAsync_ReopenWithApprove_SavesTrimmedReason()
    {
        var tasks = new FakeOffboardingTaskRepository(Entry(OffboardingTaskStatus.Completed, version: 5));
        var service = Service(tasks);
        var manager = Actor(permissions: [OffboardingPermissions.Write, OffboardingPermissions.Approve]);

        var task = await service.TransitionAsync(
            Transition(OffboardingTaskAction.Reopen, 5, manager, "  Badge not returned "), CancellationToken.None);

        Assert.Equal(OffboardingTaskStatus.Pending, task.Status);
        Assert.Equal("Badge not returned", tasks.LastReason);
        Assert.Equal(OffboardingTaskStatus.Completed, tasks.LastPreviousStatus);
    }

    [Theory]
    [InlineData(OffboardingCaseStatus.Draft)]
    [InlineData(OffboardingCaseStatus.Completed)]
    [InlineData(OffboardingCaseStatus.Cancelled)]
    public async Task TransitionAsync_CaseNotActive_ThrowsBusinessRuleWithoutSave(OffboardingCaseStatus caseStatus)
    {
        var tasks = new FakeOffboardingTaskRepository(Entry(OffboardingTaskStatus.Pending, version: 1, caseStatus: caseStatus));
        var service = Service(tasks);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() => service.TransitionAsync(
            Transition(OffboardingTaskAction.Start, 1, Writer()), CancellationToken.None));

        Assert.Equal(OffboardingTaskService.CaseNotActiveCode, exception.Code);
        Assert.Equal(0, tasks.SaveCalls);
    }

    [Fact]
    public async Task TransitionAsync_InvalidWorkflowStep_ThrowsBusinessRuleWithoutSave()
    {
        var tasks = new FakeOffboardingTaskRepository(Entry(OffboardingTaskStatus.Pending, version: 1));
        var service = Service(tasks);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() => service.TransitionAsync(
            Transition(OffboardingTaskAction.Complete, 1, Writer()), CancellationToken.None));

        Assert.Equal(OffboardingTask.InvalidTransitionCode, exception.Code);
        Assert.Equal(0, tasks.SaveCalls);
    }

    private static OffboardingTaskService Service(FakeOffboardingTaskRepository tasks, FakeCaseRepository? cases = null) =>
        new(tasks, cases ?? new FakeCaseRepository(null), new FixedTimeProvider(Now));

    private static CoreHrActor Writer(CoreHrDataScope? scope = null) =>
        Actor(scope, permissions: [OffboardingPermissions.Read, OffboardingPermissions.Write]);

    private static TransitionOffboardingTaskCommand Transition(
        OffboardingTaskAction action,
        long expectedVersion,
        CoreHrActor actor,
        string? reason = null) => new(TaskId, action, expectedVersion, reason, actor);

    private static OffboardingTaskEntry Entry(
        OffboardingTaskStatus status,
        long version,
        long? assignedToUserId = null,
        OffboardingCaseStatus caseStatus = OffboardingCaseStatus.InProgress) => new(
        CreateTask(TaskId, "admin.badge", blocks: true, status, assignedToUserId, version),
        EmployeeId,
        DepartmentId,
        caseStatus);

    private sealed class FakeOffboardingTaskRepository(OffboardingTaskEntry? entry = null) : IOffboardingTaskRepository
    {
        public bool SaveSucceeds { get; init; } = true;
        public int SaveCalls { get; private set; }
        public long? LastListedCaseId { get; private set; }
        public OffboardingTaskFilter? LastFilter { get; private set; }
        public OffboardingTaskStatus? LastPreviousStatus { get; private set; }
        public long? LastExpectedVersion { get; private set; }
        public string? LastReason { get; private set; }

        public Task<IReadOnlyList<OffboardingTask>> ListByCaseAsync(long caseId, OffboardingTaskFilter filter, CancellationToken cancellationToken)
        {
            LastListedCaseId = caseId;
            LastFilter = filter;
            return Task.FromResult<IReadOnlyList<OffboardingTask>>([]);
        }

        public Task<OffboardingTaskEntry?> GetByIdAsync(long taskId, CancellationToken cancellationToken) =>
            Task.FromResult(entry is not null && entry.Task.Id == taskId ? entry : null);

        public Task<bool> SaveTransitionAsync(
            OffboardingTask task,
            OffboardingTaskStatus previousStatus,
            long expectedVersion,
            string? reason,
            CoreHrActor actor,
            CancellationToken cancellationToken)
        {
            SaveCalls++;
            LastPreviousStatus = previousStatus;
            LastExpectedVersion = expectedVersion;
            LastReason = reason;
            return Task.FromResult(SaveSucceeds);
        }
    }

    /// <summary>Only <see cref="GetByIdAsync"/> is exercised by the task service; every write is out of reach here.</summary>
    private sealed class FakeCaseRepository(OffboardingCase? offboardingCase) : IOffboardingCaseRepository
    {
        public Task<OffboardingCaseEntry?> GetByIdAsync(long caseId, CancellationToken cancellationToken) =>
            Task.FromResult(offboardingCase is not null && offboardingCase.Id == caseId
                ? new OffboardingCaseEntry(offboardingCase, new OffboardingEmployee(EmployeeId, DepartmentId, "active", ManagerUserId), 0, null)
                : null);

        public Task<OffboardingEmployee?> GetEmployeeAsync(long employeeId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<int?> GetNoticePeriodDaysAsync(long employeeId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> HasOpenCaseAsync(long employeeId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<PagedResult<OffboardingCaseEntry>> SearchAsync(OffboardingCaseSearchQuery query, CoreHrActor actor, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlyList<OffboardingTask>> ListBlockingTasksAsync(long caseId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<IReadOnlySet<string>> ListTemplateKeysAsync(long caseId, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<OffboardingCase> InsertAsync(OffboardingCase offboardingCase, int? noticePeriodShortfallDays, CoreHrActor actor, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> SaveApprovalAsync(OffboardingCase offboardingCase, OffboardingCaseStatus previousStatus, IReadOnlyList<OffboardingTask> generatedTasks, long expectedVersion, CoreHrActor actor, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> SaveTransitionAsync(OffboardingCase offboardingCase, OffboardingCaseStatus previousStatus, long expectedVersion, string? reason, CoreHrActor actor, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> SaveCompletionAsync(OffboardingCase offboardingCase, ApprovedEmployeeEvent? terminationEvent, long expectedVersion, string? overrideReason, CoreHrActor actor, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
