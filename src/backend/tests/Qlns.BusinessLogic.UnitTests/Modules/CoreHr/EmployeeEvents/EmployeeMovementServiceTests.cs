using System.Text.Json.Nodes;
using Qlns.BusinessLogic.Modules.CoreHr.EmployeeEvents;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Xunit;

namespace Qlns.BusinessLogic.UnitTests.Modules.CoreHr.EmployeeEvents;

public sealed class EmployeeMovementServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 7, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 9, 15);
    private const long EmployeeId = 42;
    private const long DepartmentId = 10;

    [Fact]
    public async Task ListAsync_OutOfScopeEmployee_ThrowsNotFound()
    {
        var repository = new FakeEmployeeEventRepository();
        var service = Service(repository);

        await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            service.ListAsync(EmployeeId, PageRequest.Default, Actor(scope: CoreHrDataScope.Departments(99)), CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_Valid_InsertsOnceAndReturnsPersistedId()
    {
        var repository = new FakeEmployeeEventRepository();
        var service = Service(repository);

        var created = await service.CreateAsync(
            new CreateEmployeeEventCommand(EmployeeId, Write(), Actor(CoreHrPermissions.EventWrite)),
            CancellationToken.None);

        Assert.Equal(1, repository.InsertCalls);
        Assert.Equal(1001, created.Id);
        Assert.Equal(EmployeeEventStatus.Draft, created.Status);
        Assert.Equal(7, created.CreatedBy);
        Assert.Equal(Now, created.CreatedAt);
        Assert.Single(repository.Events);
    }

    [Fact]
    public async Task CreateAsync_OutOfScopeEmployee_ThrowsNotFoundWithoutInsert()
    {
        var repository = new FakeEmployeeEventRepository();
        var service = Service(repository);

        await Assert.ThrowsAsync<CoreHrNotFoundException>(() => service.CreateAsync(
            new CreateEmployeeEventCommand(EmployeeId, Write(), Actor(CoreHrPermissions.EventWrite, scope: CoreHrDataScope.Departments(99))),
            CancellationToken.None));

        Assert.Equal(0, repository.InsertCalls);
    }

    [Fact]
    public async Task CreateAsync_UnknownEmployee_ThrowsNotFound()
    {
        var repository = new FakeEmployeeEventRepository { Employee = null };
        var service = Service(repository);

        await Assert.ThrowsAsync<CoreHrNotFoundException>(() => service.CreateAsync(
            new CreateEmployeeEventCommand(EmployeeId, Write(), Actor(CoreHrPermissions.EventWrite)),
            CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_WithoutEventWrite_ThrowsForbidden()
    {
        var repository = new FakeEmployeeEventRepository();
        var service = Service(repository);

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() => service.CreateAsync(
            new CreateEmployeeEventCommand(EmployeeId, Write(), Actor(CoreHrPermissions.EventRead)),
            CancellationToken.None));

        Assert.Equal("corehr.event.write_forbidden", exception.Code);
        Assert.Equal(0, repository.InsertCalls);
    }

    [Fact]
    public async Task CreateAsync_SameDaySameField_ThrowsConflictingFieldWithDetails()
    {
        var repository = new FakeEmployeeEventRepository();
        repository.Events.Add(Event(500, EmployeeEventStatus.PendingApproval, new DateOnly(2026, 10, 1), new JsonObject { ["departmentId"] = 3, ["positionId"] = 4 }));
        var service = Service(repository);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() => service.CreateAsync(
            new CreateEmployeeEventCommand(EmployeeId, Write(afterData: new JsonObject { ["departmentId"] = 2 }), Actor(CoreHrPermissions.EventWrite)),
            CancellationToken.None));

        Assert.Equal("corehr.event.conflicting_field", exception.Code);
        Assert.Equal(500L, exception.Details["conflictingEventId"]);
        Assert.Equal(["departmentId"], Assert.IsAssignableFrom<IReadOnlyList<string>>(exception.Details["fields"]));
        Assert.Equal(0, repository.InsertCalls);
    }

    [Fact]
    public async Task CreateAsync_SameDayDifferentFieldOrCancelled_Inserts()
    {
        var repository = new FakeEmployeeEventRepository();
        repository.Events.Add(Event(500, EmployeeEventStatus.Approved, new DateOnly(2026, 10, 1), new JsonObject { ["positionId"] = 4 }));
        repository.Events.Add(Event(501, EmployeeEventStatus.Cancelled, new DateOnly(2026, 10, 1), new JsonObject { ["departmentId"] = 4 }));
        var service = Service(repository);

        await service.CreateAsync(
            new CreateEmployeeEventCommand(EmployeeId, Write(afterData: new JsonObject { ["departmentId"] = 2 }), Actor(CoreHrPermissions.EventWrite)),
            CancellationToken.None);

        Assert.Equal(1, repository.InsertCalls);
    }

    [Fact]
    public async Task CreateAsync_CorrectionOfNonAppliedEvent_ThrowsValidationOnCompensatesEventId()
    {
        var repository = new FakeEmployeeEventRepository();
        repository.Events.Add(Event(500, EmployeeEventStatus.Approved, new DateOnly(2026, 9, 1), new JsonObject { ["departmentId"] = 3 }));
        var service = Service(repository);

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() => service.CreateAsync(
            new CreateEmployeeEventCommand(EmployeeId, Write(eventType: "correction", compensatesEventId: 500), Actor(CoreHrPermissions.EventWrite)),
            CancellationToken.None));

        Assert.Contains("compensatesEventId", exception.Errors.Keys);
        Assert.Equal(0, repository.InsertCalls);
    }

    [Fact]
    public async Task CreateAsync_CorrectionOfAppliedEvent_Inserts()
    {
        var repository = new FakeEmployeeEventRepository();
        repository.Events.Add(Event(500, EmployeeEventStatus.Applied, new DateOnly(2026, 9, 1), new JsonObject { ["departmentId"] = 3 }));
        var service = Service(repository);

        var created = await service.CreateAsync(
            new CreateEmployeeEventCommand(EmployeeId, Write(eventType: "correction", compensatesEventId: 500), Actor(CoreHrPermissions.EventWrite)),
            CancellationToken.None);

        Assert.Equal(500, created.CompensatesEventId);
        Assert.Equal(1, repository.InsertCalls);
    }

    [Fact]
    public async Task TransitionAsync_ApproveWithoutEventApprove_ThrowsForbiddenWithoutSave()
    {
        var repository = new FakeEmployeeEventRepository();
        repository.Events.Add(Event(500, EmployeeEventStatus.PendingApproval, version: 2));
        var service = Service(repository);

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() => service.TransitionAsync(
            Transition(500, EmployeeEventAction.Approve, expectedVersion: 2, Actor(CoreHrPermissions.EventWrite)),
            CancellationToken.None));

        Assert.Equal("corehr.event.approve_forbidden", exception.Code);
        Assert.Equal(0, repository.SaveTransitionCalls);
    }

    [Fact]
    public async Task TransitionAsync_ApproveOk_SetsApproverAndSaves()
    {
        var repository = new FakeEmployeeEventRepository();
        repository.Events.Add(Event(500, EmployeeEventStatus.PendingApproval, version: 2));
        var service = Service(repository);

        var result = await service.TransitionAsync(
            Transition(500, EmployeeEventAction.Approve, expectedVersion: 2, Actor(CoreHrPermissions.EventApprove)),
            CancellationToken.None);

        Assert.Equal(EmployeeEventStatus.Approved, result.Status);
        Assert.Equal(7, result.ApprovedBy);
        Assert.Equal(Now, result.ApprovedAt);
        Assert.Equal(3, result.Version);
        Assert.Equal(1, repository.SaveTransitionCalls);
        Assert.Equal(EmployeeEventStatus.PendingApproval, repository.LastPreviousStatus);
    }

    [Fact]
    public async Task TransitionAsync_SubmitByCreatorWithoutWritePermission_Saves()
    {
        var repository = new FakeEmployeeEventRepository();
        repository.Events.Add(Event(500, EmployeeEventStatus.Draft, version: 1, createdBy: 7));
        var service = Service(repository);

        var result = await service.TransitionAsync(
            Transition(500, EmployeeEventAction.Submit, expectedVersion: 1, Actor(CoreHrPermissions.EventRead)),
            CancellationToken.None);

        Assert.Equal(EmployeeEventStatus.PendingApproval, result.Status);
        Assert.Equal(1, repository.SaveTransitionCalls);
    }

    [Fact]
    public async Task TransitionAsync_CancelByNonCreatorWithoutWrite_ThrowsForbidden()
    {
        var repository = new FakeEmployeeEventRepository();
        repository.Events.Add(Event(500, EmployeeEventStatus.Draft, version: 1, createdBy: 99));
        var service = Service(repository);

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() => service.TransitionAsync(
            Transition(500, EmployeeEventAction.Cancel, expectedVersion: 1, Actor(CoreHrPermissions.EventRead), reason: "x"),
            CancellationToken.None));

        Assert.Equal("corehr.event.write_forbidden", exception.Code);
        Assert.Equal(0, repository.SaveTransitionCalls);
    }

    [Fact]
    public async Task TransitionAsync_CancelWithReason_SavesReason()
    {
        var repository = new FakeEmployeeEventRepository();
        repository.Events.Add(Event(500, EmployeeEventStatus.Approved, version: 3));
        var service = Service(repository);

        var result = await service.TransitionAsync(
            Transition(500, EmployeeEventAction.Cancel, expectedVersion: 3, Actor(CoreHrPermissions.EventWrite), reason: "Decision revoked"),
            CancellationToken.None);

        Assert.Equal(EmployeeEventStatus.Cancelled, result.Status);
        Assert.Equal("Decision revoked", repository.LastReason);
    }

    [Fact]
    public async Task TransitionAsync_OutOfScope_ThrowsNotFound()
    {
        var repository = new FakeEmployeeEventRepository();
        repository.Events.Add(Event(500, EmployeeEventStatus.PendingApproval, version: 2));
        var service = Service(repository);

        await Assert.ThrowsAsync<CoreHrNotFoundException>(() => service.TransitionAsync(
            Transition(500, EmployeeEventAction.Approve, expectedVersion: 2, Actor(CoreHrPermissions.EventApprove, scope: CoreHrDataScope.Departments(99))),
            CancellationToken.None));

        Assert.Equal(0, repository.SaveTransitionCalls);
    }

    [Fact]
    public async Task TransitionAsync_StaleVersion_ThrowsConcurrencyBeforeSave()
    {
        var repository = new FakeEmployeeEventRepository();
        repository.Events.Add(Event(500, EmployeeEventStatus.PendingApproval, version: 3));
        var service = Service(repository);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => service.TransitionAsync(
            Transition(500, EmployeeEventAction.Approve, expectedVersion: 2, Actor(CoreHrPermissions.EventApprove)),
            CancellationToken.None));

        Assert.Equal(0, repository.SaveTransitionCalls);
    }

    [Fact]
    public async Task TransitionAsync_SaveLosesRace_ThrowsConcurrency()
    {
        var repository = new FakeEmployeeEventRepository { SaveTransitionSucceeds = false };
        repository.Events.Add(Event(500, EmployeeEventStatus.PendingApproval, version: 2));
        var service = Service(repository);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => service.TransitionAsync(
            Transition(500, EmployeeEventAction.Approve, expectedVersion: 2, Actor(CoreHrPermissions.EventApprove)),
            CancellationToken.None));

        Assert.Equal(1, repository.SaveTransitionCalls);
    }

    [Fact]
    public async Task ApplyDueEventsAsync_AppliesDueEventsAndReportsConflictedWithoutThrowing()
    {
        var repository = new FakeEmployeeEventRepository();
        repository.Events.Add(Event(600, EmployeeEventStatus.Approved, Today, new JsonObject { ["departmentId"] = 20 }, version: 3));
        repository.Events.Add(Event(601, EmployeeEventStatus.Approved, Today.AddDays(-1), new JsonObject { ["managerId"] = EmployeeId }, version: 3));
        repository.Events.Add(Event(602, EmployeeEventStatus.Approved, Today, new JsonObject { ["positionId"] = 30 }, version: 3));
        repository.Events.Add(Event(603, EmployeeEventStatus.Approved, Today.AddDays(1), new JsonObject { ["positionId"] = 31 }, version: 3));
        repository.FailApplyForEventIds.Add(602);
        var service = Service(repository);

        var result = await service.ApplyDueEventsAsync(Today, CoreHrActor.System("worker"), CancellationToken.None);

        Assert.Equal(1, result.Applied);
        Assert.Equal([601, 602], result.Conflicted.Order());
        Assert.Equal(2, repository.SaveAppliedCalls);
        Assert.Equal(20, repository.Employee!.DepartmentId);
        Assert.Equal(2, repository.Employee.Version);
        Assert.Equal(EmployeeEventStatus.Applied, repository.Events.Single(x => x.Id == 600).Status);
        Assert.Equal(EmployeeEventStatus.Approved, repository.Events.Single(x => x.Id == 603).Status);
    }

    private static EmployeeMovementService Service(FakeEmployeeEventRepository repository) =>
        new(repository, new FixedTimeProvider(Now));

    private static CoreHrActor Actor(string? permission = null, CoreHrDataScope? scope = null) => new(
        UserId: 7,
        EmployeeId: null,
        DataScope: scope ?? CoreHrDataScope.Organization,
        Permissions: permission is null ? new HashSet<string>() : new HashSet<string> { permission },
        CorrelationId: "test-correlation");

    private static EmployeeEventWrite Write(
        string eventType = "transfer",
        JsonObject? afterData = null,
        long? compensatesEventId = null) => new(
            eventType,
            new DateOnly(2026, 10, 1),
            new JsonObject { ["departmentId"] = 1 },
            afterData ?? new JsonObject { ["departmentId"] = 2 },
            "Move to product",
            compensatesEventId);

    private static TransitionEmployeeEventCommand Transition(
        long eventId,
        EmployeeEventAction action,
        long expectedVersion,
        CoreHrActor actor,
        string? reason = null) => new(eventId, action, expectedVersion, reason, actor);

    private static EmployeeEvent Event(
        long id,
        EmployeeEventStatus status,
        DateOnly? effectiveDate = null,
        JsonObject? afterData = null,
        long version = 2,
        long createdBy = 5) => new(
            id,
            EmployeeId,
            EmployeeEventType.Transfer,
            status,
            effectiveDate ?? Today,
            new JsonObject { ["departmentId"] = 10 },
            afterData ?? new JsonObject { ["departmentId"] = 20 },
            "Move to product",
            compensatesEventId: null,
            createdBy,
            approvedBy: status is EmployeeEventStatus.Approved or EmployeeEventStatus.Applied ? 9 : null,
            approvedAt: status is EmployeeEventStatus.Approved or EmployeeEventStatus.Applied ? Now.AddDays(-1) : null,
            appliedAt: status == EmployeeEventStatus.Applied ? Now.AddHours(-1) : null,
            version,
            createdAt: Now.AddDays(-2),
            updatedAt: Now.AddDays(-1));

    private sealed class FakeEmployeeEventRepository : IEmployeeEventRepository
    {
        private long _nextId = 1001;

        public EmployeeMasterData? Employee { get; set; } =
            new(EmployeeId, DepartmentId, 11, 12, "active", "a@qlns.example", 1);

        public List<EmployeeEvent> Events { get; } = [];
        public HashSet<long> FailApplyForEventIds { get; } = [];
        public bool SaveTransitionSucceeds { get; init; } = true;
        public int InsertCalls { get; private set; }
        public int SaveTransitionCalls { get; private set; }
        public int SaveAppliedCalls { get; private set; }
        public EmployeeEventStatus? LastPreviousStatus { get; private set; }
        public string? LastReason { get; private set; }

        public Task<EmployeeMasterData?> GetEmployeeMasterDataAsync(long employeeId, CancellationToken cancellationToken) =>
            Task.FromResult(Employee is not null && Employee.EmployeeId == employeeId ? Employee : null);

        public Task<PagedResult<EmployeeEvent>> ListByEmployeeAsync(long employeeId, PageRequest page, CancellationToken cancellationToken)
        {
            var items = Events.Where(x => x.EmployeeId == employeeId)
                .OrderByDescending(x => x.EffectiveDate).ThenByDescending(x => x.Id)
                .Skip(page.Skip).Take(page.PageSize).ToList();
            return Task.FromResult(new PagedResult<EmployeeEvent>(items, page.Page, page.PageSize, Events.Count(x => x.EmployeeId == employeeId)));
        }

        public Task<EmployeeEvent?> GetByIdAsync(long eventId, CancellationToken cancellationToken) =>
            Task.FromResult(Events.SingleOrDefault(x => x.Id == eventId));

        public Task<IReadOnlyList<ConflictingEvent>> FindConflictingAsync(
            long employeeId,
            DateOnly effectiveDate,
            IReadOnlyCollection<string> fields,
            long? excludeEventId,
            CancellationToken cancellationToken)
        {
            IReadOnlyList<ConflictingEvent> conflicts = Events
                .Where(x => x.EmployeeId == employeeId && x.EffectiveDate == effectiveDate &&
                    x.Status != EmployeeEventStatus.Cancelled && x.Id != excludeEventId)
                .Select(x => new ConflictingEvent(x.Id, x.ChangedFields.Intersect(fields).ToList()))
                .Where(x => x.Fields.Count > 0)
                .ToList();
            return Task.FromResult(conflicts);
        }

        public Task<EmployeeEvent> InsertAsync(EmployeeEvent employeeEvent, CoreHrActor actor, CancellationToken cancellationToken)
        {
            InsertCalls++;
            var persisted = new EmployeeEvent(
                _nextId++, employeeEvent.EmployeeId, employeeEvent.EventType, employeeEvent.Status, employeeEvent.EffectiveDate,
                employeeEvent.BeforeData, employeeEvent.AfterData, employeeEvent.Reason, employeeEvent.CompensatesEventId,
                employeeEvent.CreatedBy, employeeEvent.ApprovedBy, employeeEvent.ApprovedAt, employeeEvent.AppliedAt,
                employeeEvent.Version, employeeEvent.CreatedAt, employeeEvent.UpdatedAt);
            Events.Add(persisted);
            return Task.FromResult(persisted);
        }

        public Task<bool> SaveTransitionAsync(
            EmployeeEvent employeeEvent,
            EmployeeEventStatus previousStatus,
            long expectedVersion,
            string? reason,
            CoreHrActor actor,
            CancellationToken cancellationToken)
        {
            SaveTransitionCalls++;
            LastPreviousStatus = previousStatus;
            LastReason = reason;
            return Task.FromResult(SaveTransitionSucceeds);
        }

        public Task<IReadOnlyList<EmployeeEvent>> ListDueApprovedAsync(DateOnly today, CancellationToken cancellationToken)
        {
            IReadOnlyList<EmployeeEvent> due = Events.Where(x => x.IsDue(today)).OrderBy(x => x.EffectiveDate).ThenBy(x => x.Id).ToList();
            return Task.FromResult(due);
        }

        public Task<bool> SaveAppliedAsync(
            EmployeeEvent employeeEvent,
            long expectedEventVersion,
            EmployeeMasterData newData,
            long expectedEmployeeVersion,
            CoreHrActor actor,
            CancellationToken cancellationToken)
        {
            SaveAppliedCalls++;
            if (FailApplyForEventIds.Contains(employeeEvent.Id) || Employee is null || Employee.Version != expectedEmployeeVersion)
            {
                return Task.FromResult(false);
            }

            Employee = newData;
            return Task.FromResult(true);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
