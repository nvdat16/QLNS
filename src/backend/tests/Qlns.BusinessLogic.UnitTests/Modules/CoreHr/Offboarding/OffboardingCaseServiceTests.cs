using Qlns.BusinessLogic.Modules.CoreHr.EmployeeEvents;
using Qlns.BusinessLogic.Modules.CoreHr.Offboarding;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Xunit;
using static Qlns.BusinessLogic.UnitTests.Modules.CoreHr.Offboarding.OffboardingTestData;

namespace Qlns.BusinessLogic.UnitTests.Modules.CoreHr.Offboarding;

public sealed class OffboardingCaseServiceTests
{
    private const long HandoverId = 43;

    [Fact]
    public async Task CreateAsync_Valid_InsertsDraftWithShortfallWarning()
    {
        var repository = new FakeOffboardingCaseRepository { NoticePeriodDays = 30 };
        var service = Service(repository);

        var view = await service.CreateAsync(
            new CreateOffboardingCaseCommand(Write(noticeReceivedOn: LastWorkingDate.AddDays(-10)), Writer()),
            CancellationToken.None);

        Assert.Equal(1, repository.InsertCalls);
        Assert.Equal(20, repository.LastInsertedShortfall);
        Assert.Equal(20, view.NoticePeriodShortfallDays);
        Assert.Equal(0, view.BlockingTasksOutstanding);
        Assert.Equal(1001, view.Case.Id);
        Assert.Equal(OffboardingCaseStatus.Draft, view.Case.Status);
        Assert.Equal(ActorUserId, view.Case.CreatedBy);
    }

    [Fact]
    public async Task CreateAsync_NoNoticeDate_ShortfallIsNullAndStillInserts()
    {
        var repository = new FakeOffboardingCaseRepository { NoticePeriodDays = 30 };
        var service = Service(repository);

        var view = await service.CreateAsync(new CreateOffboardingCaseCommand(Write(), Writer()), CancellationToken.None);

        Assert.Null(view.NoticePeriodShortfallDays);
        Assert.Null(repository.LastInsertedShortfall);
        Assert.Equal(1, repository.InsertCalls);
    }

    [Fact]
    public async Task CreateAsync_UnknownEmployee_ThrowsNotFoundWithoutInsert()
    {
        var repository = new FakeOffboardingCaseRepository();
        var service = Service(repository);

        var exception = await Assert.ThrowsAsync<CoreHrNotFoundException>(() => service.CreateAsync(
            new CreateOffboardingCaseCommand(Write(employeeId: 999), Writer()), CancellationToken.None));

        Assert.Equal("Employee", exception.Resource);
        Assert.Equal(0, repository.InsertCalls);
    }

    [Fact]
    public async Task CreateAsync_EmployeeOutsideScope_ThrowsNotFound()
    {
        var repository = new FakeOffboardingCaseRepository();
        var service = Service(repository);

        await Assert.ThrowsAsync<CoreHrNotFoundException>(() => service.CreateAsync(
            new CreateOffboardingCaseCommand(Write(), Writer(CoreHrDataScope.Departments(99))), CancellationToken.None));

        Assert.Equal(0, repository.InsertCalls);
    }

    [Fact]
    public async Task CreateAsync_WithoutWritePermission_ThrowsForbidden()
    {
        var repository = new FakeOffboardingCaseRepository();
        var service = Service(repository);

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() => service.CreateAsync(
            new CreateOffboardingCaseCommand(Write(), Actor(permissions: OffboardingPermissions.Read)), CancellationToken.None));

        Assert.Equal(OffboardingCaseService.WriteForbiddenCode, exception.Code);
        Assert.Equal(0, repository.InsertCalls);
    }

    [Theory]
    [InlineData("suspended")]
    [InlineData("terminated")]
    public async Task CreateAsync_IneligibleEmployeeStatus_ThrowsBusinessRule(string status)
    {
        var repository = new FakeOffboardingCaseRepository();
        repository.Employees[EmployeeId] = new OffboardingEmployee(EmployeeId, DepartmentId, status, ManagerUserId);
        var service = Service(repository);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() => service.CreateAsync(
            new CreateOffboardingCaseCommand(Write(), Writer()), CancellationToken.None));

        Assert.Equal(OffboardingCase.EmployeeNotEligibleCode, exception.Code);
        Assert.Equal(status, exception.Details["employeeStatus"]);
        Assert.Equal(0, repository.InsertCalls);
    }

    [Fact]
    public async Task CreateAsync_OpenCaseExists_ThrowsCaseOpen()
    {
        var repository = new FakeOffboardingCaseRepository { HasOpenCase = true };
        var service = Service(repository);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() => service.CreateAsync(
            new CreateOffboardingCaseCommand(Write(), Writer()), CancellationToken.None));

        Assert.Equal(OffboardingCase.CaseOpenCode, exception.Code);
        Assert.Equal(0, repository.InsertCalls);
    }

    [Fact]
    public async Task CreateAsync_RepositoryReportsUniqueViolation_PropagatesCaseOpen()
    {
        var repository = new FakeOffboardingCaseRepository { InsertThrowsCaseOpen = true };
        var service = Service(repository);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() => service.CreateAsync(
            new CreateOffboardingCaseCommand(Write(), Writer()), CancellationToken.None));

        Assert.Equal(OffboardingCase.CaseOpenCode, exception.Code);
    }

    [Fact]
    public async Task CreateAsync_UnknownHandoverEmployee_ThrowsValidation()
    {
        var repository = new FakeOffboardingCaseRepository();
        var service = Service(repository);

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() => service.CreateAsync(
            new CreateOffboardingCaseCommand(Write(handoverToEmployeeId: 777), Writer()), CancellationToken.None));

        Assert.Equal(["handoverToEmployeeId"], exception.Errors.Keys);
        Assert.Equal(0, repository.InsertCalls);
    }

    [Fact]
    public async Task CreateAsync_InactiveHandoverEmployee_ThrowsValidation()
    {
        var repository = new FakeOffboardingCaseRepository();
        repository.Employees[HandoverId] = new OffboardingEmployee(HandoverId, DepartmentId, "probation", null);
        var service = Service(repository);

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() => service.CreateAsync(
            new CreateOffboardingCaseCommand(Write(), Writer()), CancellationToken.None));

        Assert.Equal(["handoverToEmployeeId"], exception.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_WithoutHandover_SkipsHandoverLookup()
    {
        var repository = new FakeOffboardingCaseRepository();
        var service = Service(repository);

        var view = await service.CreateAsync(
            new CreateOffboardingCaseCommand(Write(handoverToEmployeeId: null), Writer()), CancellationToken.None);

        Assert.Null(view.Case.HandoverToEmployeeId);
        Assert.Equal(1, repository.InsertCalls);
    }

    [Fact]
    public async Task CreateAsync_InvalidPayload_ThrowsValidationBeforeEligibilityChecks()
    {
        var repository = new FakeOffboardingCaseRepository { HasOpenCase = true };
        var service = Service(repository);

        await Assert.ThrowsAsync<CoreHrValidationException>(() => service.CreateAsync(
            new CreateOffboardingCaseCommand(Write(lastWorkingDate: Today.AddDays(-1)), Writer()), CancellationToken.None));

        Assert.Equal(0, repository.HasOpenCaseCalls);
    }

    [Fact]
    public async Task GetAsync_Visible_ReturnsViewWithDerivedFields()
    {
        var repository = new FakeOffboardingCaseRepository { NoticePeriodDays = 30 };
        repository.Cases.Add(CreateCase(OffboardingCaseStatus.InProgress, noticeReceivedOn: LastWorkingDate.AddDays(-20)));
        repository.BlockingOutstanding = 3;
        var service = Service(repository);

        var view = await service.GetAsync(CaseId, Actor(permissions: OffboardingPermissions.Read), CancellationToken.None);

        Assert.Equal(3, view.BlockingTasksOutstanding);
        Assert.Equal(10, view.NoticePeriodShortfallDays);
    }

    [Fact]
    public async Task GetAsync_SelfScopeOwnCase_IsVisible()
    {
        var repository = new FakeOffboardingCaseRepository();
        repository.Cases.Add(CreateCase(OffboardingCaseStatus.Draft));
        var service = Service(repository);

        var view = await service.GetAsync(CaseId, Actor(CoreHrDataScope.Self, employeeId: EmployeeId), CancellationToken.None);

        Assert.Equal(CaseId, view.Case.Id);
    }

    [Fact]
    public async Task GetAsync_MissingOrOutOfScope_ThrowsNotFound()
    {
        var repository = new FakeOffboardingCaseRepository();
        repository.Cases.Add(CreateCase(OffboardingCaseStatus.Draft));
        var service = Service(repository);

        var missing = await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            service.GetAsync(999, Actor(), CancellationToken.None));
        Assert.Equal(OffboardingCaseService.ResourceName, missing.Resource);

        await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            service.GetAsync(CaseId, Actor(CoreHrDataScope.Departments(99)), CancellationToken.None));
    }

    [Fact]
    public async Task SearchAsync_MapsEntriesToViews()
    {
        var repository = new FakeOffboardingCaseRepository { NoticePeriodDays = 30 };
        repository.Cases.Add(CreateCase(OffboardingCaseStatus.Draft, noticeReceivedOn: LastWorkingDate.AddDays(-5)));
        var service = Service(repository);
        var query = new OffboardingCaseSearchQuery(null, null, PageRequest.Default);

        var result = await service.SearchAsync(query, Actor(), CancellationToken.None);

        Assert.Same(query, repository.LastSearchQuery);
        Assert.Equal(1, result.TotalItems);
        Assert.Equal(25, result.Items[0].NoticePeriodShortfallDays);
    }

    [Fact]
    public async Task Approve_WithPermission_GeneratesChecklistAndSavesOnce()
    {
        var repository = new FakeOffboardingCaseRepository();
        repository.Cases.Add(CreateCase(OffboardingCaseStatus.Draft, version: 2));
        repository.ExistingTemplateKeys.Add("it.devices");
        var service = Service(repository);

        var view = await service.TransitionAsync(
            Transition(OffboardingCaseAction.Approve, 2, Approver()), CancellationToken.None);

        Assert.Equal(OffboardingCaseStatus.Approved, view.Case.Status);
        Assert.Equal(ActorUserId, view.Case.ApprovedBy);
        Assert.Equal(3, view.Case.Version);
        Assert.Equal(1, repository.SaveApprovalCalls);
        Assert.Equal(2, repository.LastExpectedVersion);
        Assert.Equal(OffboardingCaseStatus.Draft, repository.LastPreviousStatus);
        Assert.Equal(OffboardingChecklistTemplate.Items.Count - 1, repository.LastGeneratedTasks!.Count);
        Assert.DoesNotContain(repository.LastGeneratedTasks, task => task.TemplateKey == "it.devices");
        Assert.Equal(ManagerUserId, repository.LastGeneratedTasks.Single(task => task.TemplateKey == "manager.handover").AssignedToUserId);
        Assert.Equal(4, view.BlockingTasksOutstanding);
    }

    [Fact]
    public async Task Approve_WithoutApprovePermission_ThrowsForbiddenWithoutSave()
    {
        var repository = new FakeOffboardingCaseRepository();
        repository.Cases.Add(CreateCase(OffboardingCaseStatus.Draft, version: 2));
        var service = Service(repository);

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() => service.TransitionAsync(
            Transition(OffboardingCaseAction.Approve, 2, Writer()), CancellationToken.None));

        Assert.Equal(OffboardingCaseService.ApproveForbiddenCode, exception.Code);
        Assert.Equal(0, repository.SaveApprovalCalls);
    }

    [Fact]
    public async Task Start_WithWrite_SavesTransition()
    {
        var repository = new FakeOffboardingCaseRepository();
        repository.Cases.Add(CreateCase(OffboardingCaseStatus.Approved, version: 3));
        var service = Service(repository);

        var view = await service.TransitionAsync(
            Transition(OffboardingCaseAction.Start, 3, Writer()), CancellationToken.None);

        Assert.Equal(OffboardingCaseStatus.InProgress, view.Case.Status);
        Assert.Equal(1, repository.SaveTransitionCalls);
        Assert.Equal(OffboardingCaseStatus.Approved, repository.LastPreviousStatus);
        Assert.Null(repository.LastReason);
    }

    [Fact]
    public async Task Start_WithOnlyApprovePermission_ThrowsForbidden()
    {
        var repository = new FakeOffboardingCaseRepository();
        repository.Cases.Add(CreateCase(OffboardingCaseStatus.Approved, version: 3));
        var service = Service(repository);

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() => service.TransitionAsync(
            Transition(OffboardingCaseAction.Start, 3, Actor(permissions: OffboardingPermissions.Approve)), CancellationToken.None));

        Assert.Equal(OffboardingCaseService.WriteForbiddenCode, exception.Code);
        Assert.Equal(0, repository.SaveTransitionCalls);
    }

    [Fact]
    public async Task Complete_AllClear_SavesCompletionWithTerminationEvent()
    {
        var repository = new FakeOffboardingCaseRepository();
        repository.Cases.Add(CreateCase(OffboardingCaseStatus.InProgress, version: 4, settlement: FinalSettlementStatus.Paid));
        repository.BlockingTasks.Add(CreateTask(1, "it.devices", blocks: true, OffboardingTaskStatus.Completed));
        var service = Service(repository);

        var view = await service.TransitionAsync(
            Transition(OffboardingCaseAction.Complete, 4, Writer()), CancellationToken.None);

        Assert.Equal(OffboardingCaseStatus.Completed, view.Case.Status);
        Assert.Equal(1, repository.SaveCompletionCalls);
        Assert.Null(repository.LastOverrideReason);
        var terminationEvent = Assert.IsType<ApprovedEmployeeEvent>(repository.LastTerminationEvent);
        Assert.Equal(EmployeeEventType.Termination, terminationEvent.EventType);
        Assert.Equal("active", terminationEvent.BeforeStatus);
        Assert.Equal(LastWorkingDate, terminationEvent.EffectiveDate);
        Assert.Equal(2001, view.Case.EmployeeEventId);
    }

    [Fact]
    public async Task Complete_BlockingOutstanding_WithoutApprove_ThrowsWithoutSave()
    {
        var repository = new FakeOffboardingCaseRepository();
        repository.Cases.Add(CreateCase(OffboardingCaseStatus.InProgress, version: 4, settlement: FinalSettlementStatus.Paid));
        repository.BlockingTasks.Add(CreateTask(1, "it.devices", blocks: true, OffboardingTaskStatus.Pending));
        var service = Service(repository);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() => service.TransitionAsync(
            Transition(OffboardingCaseAction.Complete, 4, Writer(), "override please"), CancellationToken.None));

        Assert.Equal(OffboardingCase.BlockingTasksOutstandingCode, exception.Code);
        Assert.Equal(0, repository.SaveCompletionCalls);
    }

    [Fact]
    public async Task Complete_BlockingOutstanding_ManagerOverrideWithReason_SavesOverrideReason()
    {
        var repository = new FakeOffboardingCaseRepository();
        repository.Cases.Add(CreateCase(OffboardingCaseStatus.InProgress, version: 4, settlement: FinalSettlementStatus.Waived));
        repository.BlockingTasks.Add(CreateTask(1, "it.devices", blocks: true, OffboardingTaskStatus.Pending));
        var service = Service(repository);

        var view = await service.TransitionAsync(
            Transition(OffboardingCaseAction.Complete, 4, Actor(permissions: [OffboardingPermissions.Write, OffboardingPermissions.Approve]), "  Laptop written off  "),
            CancellationToken.None);

        Assert.Equal(OffboardingCaseStatus.Completed, view.Case.Status);
        Assert.Equal("Laptop written off", repository.LastOverrideReason);
    }

    [Fact]
    public async Task Complete_SettlementPending_ThrowsWithoutSave()
    {
        var repository = new FakeOffboardingCaseRepository();
        repository.Cases.Add(CreateCase(OffboardingCaseStatus.InProgress, version: 4, settlement: FinalSettlementStatus.Calculated));
        var service = Service(repository);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() => service.TransitionAsync(
            Transition(OffboardingCaseAction.Complete, 4, Writer()), CancellationToken.None));

        Assert.Equal(OffboardingCase.SettlementPendingCode, exception.Code);
        Assert.Equal(0, repository.SaveCompletionCalls);
    }

    [Fact]
    public async Task Complete_AlreadyLinkedEvent_SavesWithoutNewEvent()
    {
        var repository = new FakeOffboardingCaseRepository();
        repository.Cases.Add(CreateCase(OffboardingCaseStatus.InProgress, version: 4, settlement: FinalSettlementStatus.Paid, employeeEventId: 900));
        var service = Service(repository);

        var view = await service.TransitionAsync(
            Transition(OffboardingCaseAction.Complete, 4, Writer()), CancellationToken.None);

        Assert.Equal(1, repository.SaveCompletionCalls);
        Assert.Null(repository.LastTerminationEvent);
        Assert.Equal(900, view.Case.EmployeeEventId);
    }

    [Fact]
    public async Task Cancel_WithReason_SavesTrimmedReason()
    {
        var repository = new FakeOffboardingCaseRepository();
        repository.Cases.Add(CreateCase(OffboardingCaseStatus.Approved, version: 3));
        var service = Service(repository);

        var view = await service.TransitionAsync(
            Transition(OffboardingCaseAction.Cancel, 3, Writer(), "  Resignation withdrawn "), CancellationToken.None);

        Assert.Equal(OffboardingCaseStatus.Cancelled, view.Case.Status);
        Assert.Equal("Resignation withdrawn", repository.LastReason);
        Assert.Equal(OffboardingCaseStatus.Approved, repository.LastPreviousStatus);
    }

    [Fact]
    public async Task Cancel_WithoutReason_ThrowsValidationWithoutSave()
    {
        var repository = new FakeOffboardingCaseRepository();
        repository.Cases.Add(CreateCase(OffboardingCaseStatus.Approved, version: 3));
        var service = Service(repository);

        await Assert.ThrowsAsync<CoreHrValidationException>(() => service.TransitionAsync(
            Transition(OffboardingCaseAction.Cancel, 3, Writer()), CancellationToken.None));

        Assert.Equal(0, repository.SaveTransitionCalls);
    }

    [Fact]
    public async Task Transition_InvalidWorkflowStep_ThrowsBusinessRuleWithoutSave()
    {
        var repository = new FakeOffboardingCaseRepository();
        repository.Cases.Add(CreateCase(OffboardingCaseStatus.Draft, version: 1));
        var service = Service(repository);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() => service.TransitionAsync(
            Transition(OffboardingCaseAction.Start, 1, Writer()), CancellationToken.None));

        Assert.Equal(OffboardingCase.InvalidTransitionCode, exception.Code);
        Assert.Equal(0, repository.SaveTransitionCalls);
    }

    [Fact]
    public async Task Transition_StaleVersion_ThrowsConcurrencyBeforeAnyWork()
    {
        var repository = new FakeOffboardingCaseRepository();
        repository.Cases.Add(CreateCase(OffboardingCaseStatus.Draft, version: 3));
        var service = Service(repository);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => service.TransitionAsync(
            Transition(OffboardingCaseAction.Approve, 2, Approver()), CancellationToken.None));

        Assert.Equal(0, repository.SaveApprovalCalls);
        Assert.Equal(0, repository.ListTemplateKeysCalls);
    }

    [Fact]
    public async Task Transition_SaveLosesRace_ThrowsConcurrency()
    {
        var repository = new FakeOffboardingCaseRepository { SaveSucceeds = false };
        repository.Cases.Add(CreateCase(OffboardingCaseStatus.Approved, version: 3));
        var service = Service(repository);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => service.TransitionAsync(
            Transition(OffboardingCaseAction.Start, 3, Writer()), CancellationToken.None));

        Assert.Equal(1, repository.SaveTransitionCalls);
    }

    [Fact]
    public async Task Transition_CaseOutsideScope_ThrowsNotFound()
    {
        var repository = new FakeOffboardingCaseRepository();
        repository.Cases.Add(CreateCase(OffboardingCaseStatus.Approved, version: 3));
        var service = Service(repository);

        await Assert.ThrowsAsync<CoreHrNotFoundException>(() => service.TransitionAsync(
            Transition(OffboardingCaseAction.Start, 3, Writer(CoreHrDataScope.Departments(99))), CancellationToken.None));

        Assert.Equal(0, repository.SaveTransitionCalls);
    }

    private static OffboardingCaseService Service(FakeOffboardingCaseRepository repository) =>
        new(repository, new FixedTimeProvider(Now));

    private static CoreHrActor Writer(CoreHrDataScope? scope = null) =>
        Actor(scope, permissions: [OffboardingPermissions.Read, OffboardingPermissions.Write]);

    private static CoreHrActor Approver() =>
        Actor(permissions: [OffboardingPermissions.Read, OffboardingPermissions.Approve]);

    private static TransitionOffboardingCaseCommand Transition(
        OffboardingCaseAction action,
        long expectedVersion,
        CoreHrActor actor,
        string? reason = null) => new(CaseId, action, expectedVersion, reason, actor);

    private sealed class FakeOffboardingCaseRepository : IOffboardingCaseRepository
    {
        private long _nextCaseId = 1001;
        private long _nextEventId = 2001;

        public Dictionary<long, OffboardingEmployee> Employees { get; } = new()
        {
            [EmployeeId] = new OffboardingEmployee(EmployeeId, DepartmentId, "active", ManagerUserId),
            [HandoverId] = new OffboardingEmployee(HandoverId, DepartmentId, "active", null)
        };

        public List<OffboardingCase> Cases { get; } = [];
        public List<OffboardingTask> BlockingTasks { get; } = [];
        public HashSet<string> ExistingTemplateKeys { get; } = new(StringComparer.Ordinal);
        public int? NoticePeriodDays { get; init; }
        public bool HasOpenCase { get; init; }
        public bool InsertThrowsCaseOpen { get; init; }
        public bool SaveSucceeds { get; init; } = true;
        public int BlockingOutstanding { get; set; }

        public int InsertCalls { get; private set; }
        public int HasOpenCaseCalls { get; private set; }
        public int ListTemplateKeysCalls { get; private set; }
        public int SaveApprovalCalls { get; private set; }
        public int SaveTransitionCalls { get; private set; }
        public int SaveCompletionCalls { get; private set; }
        public int? LastInsertedShortfall { get; private set; }
        public long? LastExpectedVersion { get; private set; }
        public IReadOnlyList<OffboardingTask>? LastGeneratedTasks { get; private set; }
        public OffboardingCaseStatus? LastPreviousStatus { get; private set; }
        public string? LastReason { get; private set; }
        public string? LastOverrideReason { get; private set; }
        public ApprovedEmployeeEvent? LastTerminationEvent { get; private set; }
        public OffboardingCaseSearchQuery? LastSearchQuery { get; private set; }

        public Task<OffboardingEmployee?> GetEmployeeAsync(long employeeId, CancellationToken cancellationToken) =>
            Task.FromResult(Employees.GetValueOrDefault(employeeId));

        public Task<int?> GetNoticePeriodDaysAsync(long employeeId, CancellationToken cancellationToken) =>
            Task.FromResult(NoticePeriodDays);

        public Task<bool> HasOpenCaseAsync(long employeeId, CancellationToken cancellationToken)
        {
            HasOpenCaseCalls++;
            return Task.FromResult(HasOpenCase);
        }

        public Task<PagedResult<OffboardingCaseEntry>> SearchAsync(
            OffboardingCaseSearchQuery query,
            CoreHrActor actor,
            CancellationToken cancellationToken)
        {
            LastSearchQuery = query;
            var entries = Cases.Select(Entry).ToList();
            return Task.FromResult(new PagedResult<OffboardingCaseEntry>(entries, query.Page.Page, query.Page.PageSize, entries.Count));
        }

        public Task<OffboardingCaseEntry?> GetByIdAsync(long caseId, CancellationToken cancellationToken)
        {
            var found = Cases.SingleOrDefault(c => c.Id == caseId);
            return Task.FromResult(found is null ? null : Entry(found));
        }

        public Task<IReadOnlyList<OffboardingTask>> ListBlockingTasksAsync(long caseId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<OffboardingTask>>(BlockingTasks);

        public Task<IReadOnlySet<string>> ListTemplateKeysAsync(long caseId, CancellationToken cancellationToken)
        {
            ListTemplateKeysCalls++;
            return Task.FromResult<IReadOnlySet<string>>(ExistingTemplateKeys);
        }

        public Task<OffboardingCase> InsertAsync(
            OffboardingCase offboardingCase,
            int? noticePeriodShortfallDays,
            CoreHrActor actor,
            CancellationToken cancellationToken)
        {
            InsertCalls++;
            LastInsertedShortfall = noticePeriodShortfallDays;
            if (InsertThrowsCaseOpen)
            {
                throw new CoreHrBusinessRuleException(OffboardingCase.CaseOpenCode, "open case");
            }

            var persisted = new OffboardingCase(
                _nextCaseId++, offboardingCase.EmployeeId, offboardingCase.EmployeeEventId, offboardingCase.SeparationType,
                offboardingCase.NoticeReceivedOn, offboardingCase.LastWorkingDate, offboardingCase.HandoverToEmployeeId,
                offboardingCase.ExitInterviewAt, offboardingCase.FinalSettlementStatus, offboardingCase.Status, offboardingCase.Reason,
                offboardingCase.CreatedBy, offboardingCase.ApprovedBy, offboardingCase.ApprovedAt, offboardingCase.CompletedAt,
                offboardingCase.Version, offboardingCase.CreatedAt, offboardingCase.UpdatedAt);
            Cases.Add(persisted);
            return Task.FromResult(persisted);
        }

        public Task<bool> SaveApprovalAsync(
            OffboardingCase offboardingCase,
            OffboardingCaseStatus previousStatus,
            IReadOnlyList<OffboardingTask> generatedTasks,
            long expectedVersion,
            CoreHrActor actor,
            CancellationToken cancellationToken)
        {
            SaveApprovalCalls++;
            LastPreviousStatus = previousStatus;
            LastExpectedVersion = expectedVersion;
            LastGeneratedTasks = generatedTasks;
            return Task.FromResult(SaveSucceeds);
        }

        public Task<bool> SaveTransitionAsync(
            OffboardingCase offboardingCase,
            OffboardingCaseStatus previousStatus,
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

        public Task<bool> SaveCompletionAsync(
            OffboardingCase offboardingCase,
            ApprovedEmployeeEvent? terminationEvent,
            long expectedVersion,
            string? overrideReason,
            CoreHrActor actor,
            CancellationToken cancellationToken)
        {
            SaveCompletionCalls++;
            LastExpectedVersion = expectedVersion;
            LastTerminationEvent = terminationEvent;
            LastOverrideReason = overrideReason;
            if (SaveSucceeds && terminationEvent is not null)
            {
                offboardingCase.LinkEmployeeEvent(_nextEventId++);
            }

            return Task.FromResult(SaveSucceeds);
        }

        private OffboardingCaseEntry Entry(OffboardingCase offboardingCase) => new(
            offboardingCase,
            Employees[offboardingCase.EmployeeId],
            BlockingOutstanding,
            NoticePeriodDays);
    }
}
