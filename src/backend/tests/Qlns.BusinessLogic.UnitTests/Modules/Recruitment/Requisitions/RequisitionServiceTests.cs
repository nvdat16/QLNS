using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Requisitions;
using Xunit;

namespace Qlns.BusinessLogic.UnitTests.Modules.Recruitment.Requisitions;

public sealed class RequisitionServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 17, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 9, 17);

    private const long DepartmentId = 3;
    private const long OtherDepartmentId = 9;
    private const long CreatorUserId = 7;

    [Fact]
    public async Task SearchAsync_PassesQueryAndActorToRepository()
    {
        var repository = new FakeRequisitionRepository
        {
            SearchResult = new PagedResult<Requisition>([Create(RequisitionStatus.Draft, 1)], 1, 20, 1)
        };
        var service = CreateService(repository);
        var query = new RequisitionSearchQuery("eng", DepartmentId, RequisitionStatus.Draft, RequisitionSort.ClosingDate, PageRequest.Default);
        var actor = Actor(RequisitionPermissions.Read);

        var result = await service.SearchAsync(query, actor, CancellationToken.None);

        Assert.Same(query, repository.LastSearchQuery);
        Assert.Same(actor, repository.LastSearchActor);
        Assert.Equal(1, result.TotalItems);
    }

    [Fact]
    public async Task GetAsync_Visible_ReturnsRequisition()
    {
        var repository = new FakeRequisitionRepository { Requisition = Create(RequisitionStatus.Draft, 1) };
        var service = CreateService(repository);

        var result = await service.GetAsync(42, Actor(RequisitionPermissions.Read), CancellationToken.None);

        Assert.Equal(42, result.Id);
    }

    [Fact]
    public async Task GetAsync_MissingOrOutOfScope_ThrowsNotFound()
    {
        var service = CreateService(new FakeRequisitionRepository());

        var exception = await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            service.GetAsync(42, Actor(RequisitionPermissions.Read), CancellationToken.None));

        Assert.Equal("Requisition", exception.Resource);
        Assert.Equal(42, exception.Id);
    }

    [Fact]
    public async Task CreateAsync_WithoutWritePermission_ThrowsForbiddenWithoutInserting()
    {
        var repository = new FakeRequisitionRepository();
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() =>
            service.CreateAsync(new CreateRequisitionCommand(ValidWrite(), Actor(RequisitionPermissions.Read)), CancellationToken.None));

        Assert.Equal(RequisitionService.WriteForbiddenCode, exception.Code);
        Assert.Equal(0, repository.InsertCalls);
    }

    [Fact]
    public async Task CreateAsync_Valid_InsertsDraftCreatedByActorAndReturnsPersistedRequisition()
    {
        var repository = new FakeRequisitionRepository();
        var service = CreateService(repository);
        var actor = Actor(RequisitionPermissions.Write);

        var created = await service.CreateAsync(new CreateRequisitionCommand(ValidWrite(), actor), CancellationToken.None);

        Assert.Equal(1, repository.InsertCalls);
        Assert.Same(actor, repository.LastActor);
        Assert.NotNull(repository.LastInserted);
        Assert.Equal(0, repository.LastInserted.Id);
        Assert.Equal(RequisitionStatus.Draft, repository.LastInserted.Status);
        Assert.Equal(CreatorUserId, repository.LastInserted.CreatedBy);
        Assert.Equal(Now, repository.LastInserted.CreatedAt);
        Assert.Equal(100, created.Id);
        Assert.Equal("REQ-2026-00100", created.JobCode);
    }

    [Fact]
    public async Task CreateAsync_DepartmentScopedActorOutsideDepartment_ThrowsForbiddenWithoutInserting()
    {
        var repository = new FakeRequisitionRepository();
        var service = CreateService(repository);
        var actor = Actor(CoreHrDataScope.Departments(OtherDepartmentId), RequisitionPermissions.Write);

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() =>
            service.CreateAsync(new CreateRequisitionCommand(ValidWrite(), actor), CancellationToken.None));

        Assert.Equal(RequisitionService.DepartmentOutOfScopeCode, exception.Code);
        Assert.Equal(0, repository.InsertCalls);
        Assert.Equal(0, repository.DepartmentLookups);
    }

    [Fact]
    public async Task CreateAsync_DepartmentScopedActorInsideDepartment_Inserts()
    {
        var repository = new FakeRequisitionRepository();
        var service = CreateService(repository);
        var actor = Actor(CoreHrDataScope.Departments(OtherDepartmentId, DepartmentId), RequisitionPermissions.Write);

        await service.CreateAsync(new CreateRequisitionCommand(ValidWrite(), actor), CancellationToken.None);

        Assert.Equal(1, repository.InsertCalls);
    }

    [Fact]
    public async Task CreateAsync_SelfOnlyActor_ThrowsForbidden()
    {
        var repository = new FakeRequisitionRepository();
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() =>
            service.CreateAsync(new CreateRequisitionCommand(ValidWrite(), Actor(CoreHrDataScope.Self, RequisitionPermissions.Write)), CancellationToken.None));

        Assert.Equal(RequisitionService.DepartmentOutOfScopeCode, exception.Code);
    }

    [Fact]
    public async Task CreateAsync_UnknownDepartment_ThrowsValidationOnDepartmentId()
    {
        var repository = new FakeRequisitionRepository { DepartmentIds = [OtherDepartmentId] };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() =>
            service.CreateAsync(new CreateRequisitionCommand(ValidWrite(), Actor(RequisitionPermissions.Write)), CancellationToken.None));

        Assert.Equal(["departmentId"], exception.Errors.Keys);
        Assert.Equal(0, repository.InsertCalls);
    }

    [Fact]
    public async Task CreateAsync_UnknownPosition_ThrowsValidationOnPositionId()
    {
        var repository = new FakeRequisitionRepository { PositionIds = [] };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() =>
            service.CreateAsync(new CreateRequisitionCommand(ValidWrite(), Actor(RequisitionPermissions.Write)), CancellationToken.None));

        Assert.Equal(["positionId"], exception.Errors.Keys);
        Assert.Equal(0, repository.InsertCalls);
    }

    [Fact]
    public async Task CreateAsync_WithoutPosition_SkipsPositionLookup()
    {
        var repository = new FakeRequisitionRepository { PositionIds = [] };
        var service = CreateService(repository);

        await service.CreateAsync(
            new CreateRequisitionCommand(ValidWrite() with { PositionId = null }, Actor(RequisitionPermissions.Write)),
            CancellationToken.None);

        Assert.Equal(0, repository.PositionLookups);
        Assert.Equal(1, repository.InsertCalls);
    }

    [Fact]
    public async Task CreateAsync_InvalidPayload_ThrowsValidationBeforeAnyLookup()
    {
        var repository = new FakeRequisitionRepository();
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() =>
            service.CreateAsync(
                new CreateRequisitionCommand(ValidWrite() with { TargetHeadcount = 0, ClosingDate = Today.AddDays(-1) }, Actor(RequisitionPermissions.Write)),
                CancellationToken.None));

        Assert.True(exception.Errors.ContainsKey("targetHeadcount"));
        Assert.True(exception.Errors.ContainsKey("closingDate"));
        Assert.Equal(0, repository.DepartmentLookups);
        Assert.Equal(0, repository.InsertCalls);
    }

    [Fact]
    public async Task ReplaceAsync_Draft_SavesOnceWithChangedFieldsAndExpectedVersion()
    {
        var repository = new FakeRequisitionRepository { Requisition = Create(RequisitionStatus.Draft, version: 2) };
        var service = CreateService(repository);

        var updated = await service.ReplaceAsync(
            new ReplaceRequisitionCommand(42, 2, ValidWrite() with { Title = "Staff Engineer", TargetHeadcount = 4 }, Actor(RequisitionPermissions.Write)),
            CancellationToken.None);

        Assert.Equal(3, updated.Version);
        Assert.Equal(1, repository.ReplaceCalls);
        Assert.Equal(2, repository.LastExpectedVersion);
        Assert.Equal(["title", "targetHeadcount"], repository.LastChangedFields);
    }

    [Fact]
    public async Task ReplaceAsync_StaleVersion_ThrowsConcurrencyWithoutSaving()
    {
        var repository = new FakeRequisitionRepository { Requisition = Create(RequisitionStatus.Draft, version: 3) };
        var service = CreateService(repository);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => service.ReplaceAsync(
            new ReplaceRequisitionCommand(42, 2, ValidWrite(), Actor(RequisitionPermissions.Write)),
            CancellationToken.None));

        Assert.Equal(0, repository.ReplaceCalls);
    }

    [Fact]
    public async Task ReplaceAsync_MissingOrOutOfScope_ThrowsNotFound()
    {
        var service = CreateService(new FakeRequisitionRepository());

        var exception = await Assert.ThrowsAsync<CoreHrNotFoundException>(() => service.ReplaceAsync(
            new ReplaceRequisitionCommand(42, 1, ValidWrite(), Actor(RequisitionPermissions.Write)),
            CancellationToken.None));

        Assert.Equal("Requisition", exception.Resource);
    }

    [Fact]
    public async Task ReplaceAsync_NonDraft_ThrowsNotEditableWithoutSaving()
    {
        var repository = new FakeRequisitionRepository { Requisition = Create(RequisitionStatus.PendingApproval, version: 2) };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() => service.ReplaceAsync(
            new ReplaceRequisitionCommand(42, 2, ValidWrite(), Actor(RequisitionPermissions.Write)),
            CancellationToken.None));

        Assert.Equal(Requisition.NotEditableCode, exception.Code);
        Assert.Equal(0, repository.ReplaceCalls);
    }

    [Fact]
    public async Task ReplaceAsync_SaveLosesRace_ThrowsConcurrency()
    {
        var repository = new FakeRequisitionRepository { Requisition = Create(RequisitionStatus.Draft, version: 1), SaveSucceeds = false };
        var service = CreateService(repository);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => service.ReplaceAsync(
            new ReplaceRequisitionCommand(42, 1, ValidWrite(), Actor(RequisitionPermissions.Write)),
            CancellationToken.None));

        Assert.Equal(1, repository.ReplaceCalls);
    }

    [Fact]
    public async Task ReplaceAsync_MovingToDepartmentOutsideScope_ThrowsForbiddenWithoutSaving()
    {
        var repository = new FakeRequisitionRepository { Requisition = Create(RequisitionStatus.Draft, version: 1), DepartmentIds = [DepartmentId, OtherDepartmentId] };
        var service = CreateService(repository);
        var actor = Actor(CoreHrDataScope.Departments(DepartmentId), RequisitionPermissions.Write);

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() => service.ReplaceAsync(
            new ReplaceRequisitionCommand(42, 1, ValidWrite() with { DepartmentId = OtherDepartmentId }, actor),
            CancellationToken.None));

        Assert.Equal(RequisitionService.DepartmentOutOfScopeCode, exception.Code);
        Assert.Equal(0, repository.ReplaceCalls);
    }

    [Fact]
    public async Task ReplaceAsync_CreatorWithoutWritePermission_IsAllowed()
    {
        var repository = new FakeRequisitionRepository { Requisition = Create(RequisitionStatus.Draft, version: 1) };
        var service = CreateService(repository);

        await service.ReplaceAsync(
            new ReplaceRequisitionCommand(42, 1, ValidWrite(), Actor(RequisitionPermissions.Read)),
            CancellationToken.None);

        Assert.Equal(1, repository.ReplaceCalls);
    }

    [Fact]
    public async Task ReplaceAsync_NonCreatorWithoutWritePermission_ThrowsForbidden()
    {
        var repository = new FakeRequisitionRepository { Requisition = Create(RequisitionStatus.Draft, version: 1) };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() => service.ReplaceAsync(
            new ReplaceRequisitionCommand(42, 1, ValidWrite(), Actor(userId: 99, CoreHrDataScope.Organization, RequisitionPermissions.Read)),
            CancellationToken.None));

        Assert.Equal(RequisitionService.WriteForbiddenCode, exception.Code);
        Assert.Equal(0, repository.ReplaceCalls);
    }

    [Fact]
    public async Task TransitionAsync_SubmitByCreatorWithoutWritePermission_Saves()
    {
        var repository = new FakeRequisitionRepository { Requisition = Create(RequisitionStatus.Draft, version: 1) };
        var service = CreateService(repository);

        var updated = await service.TransitionAsync(
            Transition(RequisitionAction.Submit, 1, Actor(RequisitionPermissions.Read), reason: "ignored"),
            CancellationToken.None);

        Assert.Equal(RequisitionStatus.PendingApproval, updated.Status);
        Assert.Equal(2, updated.Version);
        Assert.Equal(1, repository.TransitionCalls);
        Assert.Equal(RequisitionStatus.Draft, repository.LastPreviousStatus);
        Assert.Equal(RequisitionAction.Submit, repository.LastAction);
        Assert.Equal(1, repository.LastExpectedVersion);
        Assert.Null(repository.LastReason);
    }

    [Fact]
    public async Task TransitionAsync_SubmitByStrangerWithoutWritePermission_ThrowsForbidden()
    {
        var repository = new FakeRequisitionRepository { Requisition = Create(RequisitionStatus.Draft, version: 1) };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() => service.TransitionAsync(
            Transition(RequisitionAction.Submit, 1, Actor(userId: 99, CoreHrDataScope.Organization, RequisitionPermissions.Approve)),
            CancellationToken.None));

        Assert.Equal(RequisitionService.WriteForbiddenCode, exception.Code);
        Assert.Equal(0, repository.TransitionCalls);
    }

    [Theory]
    [InlineData(RequisitionAction.Approve)]
    [InlineData(RequisitionAction.Reject)]
    public async Task TransitionAsync_ApproveOrRejectWithoutApprovePermission_ThrowsForbidden(RequisitionAction action)
    {
        var repository = new FakeRequisitionRepository { Requisition = Create(RequisitionStatus.PendingApproval, version: 2) };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() => service.TransitionAsync(
            Transition(action, 2, Actor(RequisitionPermissions.Write, RequisitionPermissions.Publish), "reason"),
            CancellationToken.None));

        Assert.Equal(RequisitionService.ApproveForbiddenCode, exception.Code);
        Assert.Equal(0, repository.TransitionCalls);
    }

    [Fact]
    public async Task TransitionAsync_Approve_MovesToApproved()
    {
        var repository = new FakeRequisitionRepository { Requisition = Create(RequisitionStatus.PendingApproval, version: 2) };
        var service = CreateService(repository);

        var updated = await service.TransitionAsync(
            Transition(RequisitionAction.Approve, 2, Actor(RequisitionPermissions.Approve)),
            CancellationToken.None);

        Assert.Equal(RequisitionStatus.Approved, updated.Status);
        Assert.Equal(RequisitionAction.Approve, repository.LastAction);
    }

    [Fact]
    public async Task TransitionAsync_RejectWithReason_ReturnsToDraftAndRecordsTrimmedReason()
    {
        var repository = new FakeRequisitionRepository { Requisition = Create(RequisitionStatus.PendingApproval, version: 2) };
        var service = CreateService(repository);

        var updated = await service.TransitionAsync(
            Transition(RequisitionAction.Reject, 2, Actor(RequisitionPermissions.Approve), "  Budget frozen  "),
            CancellationToken.None);

        Assert.Equal(RequisitionStatus.Draft, updated.Status);
        Assert.Equal("Budget frozen", repository.LastReason);
        Assert.Equal(RequisitionStatus.PendingApproval, repository.LastPreviousStatus);
    }

    [Fact]
    public async Task TransitionAsync_RejectWithoutReason_ThrowsValidationWithoutSaving()
    {
        var repository = new FakeRequisitionRepository { Requisition = Create(RequisitionStatus.PendingApproval, version: 2) };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() => service.TransitionAsync(
            Transition(RequisitionAction.Reject, 2, Actor(RequisitionPermissions.Approve)),
            CancellationToken.None));

        Assert.Equal(["reason"], exception.Errors.Keys);
        Assert.Equal(0, repository.TransitionCalls);
    }

    [Theory]
    [InlineData(RequisitionAction.Publish, RequisitionStatus.Approved)]
    [InlineData(RequisitionAction.Close, RequisitionStatus.ActiveRecruiting)]
    public async Task TransitionAsync_PublishOrCloseWithoutPublishPermission_ThrowsForbidden(RequisitionAction action, RequisitionStatus status)
    {
        var repository = new FakeRequisitionRepository { Requisition = Create(status, version: 3) };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() => service.TransitionAsync(
            Transition(action, 3, Actor(RequisitionPermissions.Write, RequisitionPermissions.Approve)),
            CancellationToken.None));

        Assert.Equal(RequisitionService.PublishForbiddenCode, exception.Code);
        Assert.Equal(0, repository.TransitionCalls);
    }

    [Fact]
    public async Task TransitionAsync_Publish_ActivatesSetsPublishedAtAndPassesPublishAction()
    {
        var repository = new FakeRequisitionRepository { Requisition = Create(RequisitionStatus.Approved, version: 3) };
        var service = CreateService(repository);

        var updated = await service.TransitionAsync(
            Transition(RequisitionAction.Publish, 3, Actor(RequisitionPermissions.Publish)),
            CancellationToken.None);

        Assert.Equal(RequisitionStatus.ActiveRecruiting, updated.Status);
        Assert.Equal(Now, updated.PublishedAt);
        Assert.Equal(RequisitionAction.Publish, repository.LastAction);
        Assert.Equal(0, repository.OpenOfferChecks);
    }

    [Fact]
    public async Task TransitionAsync_PublishWithClosingDateNotAfterToday_ThrowsValidation()
    {
        var repository = new FakeRequisitionRepository { Requisition = Create(RequisitionStatus.Approved, version: 3, closingDate: Today) };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() => service.TransitionAsync(
            Transition(RequisitionAction.Publish, 3, Actor(RequisitionPermissions.Publish)),
            CancellationToken.None));

        Assert.Equal(["closingDate"], exception.Errors.Keys);
        Assert.Equal(0, repository.TransitionCalls);
    }

    [Fact]
    public async Task TransitionAsync_CloseWithOpenOffers_ThrowsOpenOffersWithoutSaving()
    {
        var repository = new FakeRequisitionRepository { Requisition = Create(RequisitionStatus.ActiveRecruiting, version: 4), HasOpenOffers = true };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() => service.TransitionAsync(
            Transition(RequisitionAction.Close, 4, Actor(RequisitionPermissions.Publish)),
            CancellationToken.None));

        Assert.Equal(RequisitionService.OpenOffersCode, exception.Code);
        Assert.Equal(1, repository.OpenOfferChecks);
        Assert.Equal(0, repository.TransitionCalls);
    }

    [Fact]
    public async Task TransitionAsync_CloseWithoutOpenOffers_Saves()
    {
        var repository = new FakeRequisitionRepository { Requisition = Create(RequisitionStatus.ActiveRecruiting, version: 4) };
        var service = CreateService(repository);

        var updated = await service.TransitionAsync(
            Transition(RequisitionAction.Close, 4, Actor(RequisitionPermissions.Publish)),
            CancellationToken.None);

        Assert.Equal(RequisitionStatus.Closed, updated.Status);
        Assert.Equal(1, repository.OpenOfferChecks);
        Assert.Equal(1, repository.TransitionCalls);
    }

    [Fact]
    public async Task TransitionAsync_CloseFromNonActive_ReportsInvalidTransitionWithoutOfferLookup()
    {
        var repository = new FakeRequisitionRepository { Requisition = Create(RequisitionStatus.Draft, version: 1), HasOpenOffers = true };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() => service.TransitionAsync(
            Transition(RequisitionAction.Close, 1, Actor(RequisitionPermissions.Publish)),
            CancellationToken.None));

        Assert.Equal(Requisition.InvalidTransitionCode, exception.Code);
        Assert.Equal(0, repository.OpenOfferChecks);
    }

    [Theory]
    [InlineData(RequisitionPermissions.Write)]
    [InlineData(RequisitionPermissions.Approve)]
    public async Task TransitionAsync_CancelWithWriteOrApprove_SavesWithReason(string permission)
    {
        var repository = new FakeRequisitionRepository { Requisition = Create(RequisitionStatus.Approved, version: 3) };
        var service = CreateService(repository);

        var updated = await service.TransitionAsync(
            Transition(RequisitionAction.Cancel, 3, Actor(userId: 99, CoreHrDataScope.Organization, permission), "Position withdrawn"),
            CancellationToken.None);

        Assert.Equal(RequisitionStatus.Cancelled, updated.Status);
        Assert.Equal("Position withdrawn", repository.LastReason);
    }

    [Fact]
    public async Task TransitionAsync_CancelWithPublishOnly_ThrowsForbidden()
    {
        var repository = new FakeRequisitionRepository { Requisition = Create(RequisitionStatus.Approved, version: 3) };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() => service.TransitionAsync(
            Transition(RequisitionAction.Cancel, 3, Actor(userId: 99, CoreHrDataScope.Organization, RequisitionPermissions.Publish), "reason"),
            CancellationToken.None));

        Assert.Equal(RequisitionService.CancelForbiddenCode, exception.Code);
        Assert.Equal(0, repository.TransitionCalls);
    }

    [Fact]
    public async Task TransitionAsync_InvalidWorkflowStep_ThrowsBusinessRuleWithoutSaving()
    {
        var repository = new FakeRequisitionRepository { Requisition = Create(RequisitionStatus.Draft, version: 1) };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() => service.TransitionAsync(
            Transition(RequisitionAction.Approve, 1, Actor(RequisitionPermissions.Approve)),
            CancellationToken.None));

        Assert.Equal(Requisition.InvalidTransitionCode, exception.Code);
        Assert.Equal("draft", exception.Details["currentStatus"]);
        Assert.Equal("approve", exception.Details["action"]);
        Assert.Equal(0, repository.TransitionCalls);
    }

    [Fact]
    public async Task TransitionAsync_StaleVersion_ThrowsConcurrencyBeforePermissionCheck()
    {
        var repository = new FakeRequisitionRepository { Requisition = Create(RequisitionStatus.PendingApproval, version: 3) };
        var service = CreateService(repository);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => service.TransitionAsync(
            Transition(RequisitionAction.Approve, 2, Actor(RequisitionPermissions.Read)),
            CancellationToken.None));

        Assert.Equal(0, repository.TransitionCalls);
    }

    [Fact]
    public async Task TransitionAsync_SaveLosesRace_ThrowsConcurrency()
    {
        var repository = new FakeRequisitionRepository { Requisition = Create(RequisitionStatus.Draft, version: 1), SaveSucceeds = false };
        var service = CreateService(repository);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => service.TransitionAsync(
            Transition(RequisitionAction.Submit, 1, Actor(RequisitionPermissions.Write)),
            CancellationToken.None));

        Assert.Equal(1, repository.TransitionCalls);
    }

    [Fact]
    public async Task TransitionAsync_MissingOrOutOfScope_ThrowsNotFound()
    {
        var repository = new FakeRequisitionRepository();
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrNotFoundException>(() => service.TransitionAsync(
            Transition(RequisitionAction.Submit, 1, Actor(RequisitionPermissions.Write)),
            CancellationToken.None));

        Assert.Equal("Requisition", exception.Resource);
        Assert.Equal(42, exception.Id);
        Assert.Equal(0, repository.TransitionCalls);
    }

    private static RequisitionService CreateService(FakeRequisitionRepository repository) =>
        new(repository, new FixedTimeProvider(Now));

    private static TransitionRequisitionCommand Transition(
        RequisitionAction action,
        long expectedVersion,
        CoreHrActor actor,
        string? reason = null) => new(42, action, expectedVersion, reason, actor);

    private static CoreHrActor Actor(params string[] permissions) =>
        Actor(CreatorUserId, CoreHrDataScope.Organization, permissions);

    private static CoreHrActor Actor(CoreHrDataScope scope, params string[] permissions) =>
        Actor(CreatorUserId, scope, permissions);

    private static CoreHrActor Actor(long userId, CoreHrDataScope scope, params string[] permissions) => new(
        UserId: userId,
        EmployeeId: null,
        DataScope: scope,
        Permissions: permissions.ToHashSet(StringComparer.Ordinal),
        CorrelationId: "test-correlation");

    private static RequisitionWrite ValidWrite() => new(
        Title: "Senior Engineer",
        DepartmentId: DepartmentId,
        PositionId: 10,
        Description: "Build the platform",
        Requirements: null,
        Location: "Hanoi",
        EmploymentType: "full_time",
        SalaryMin: 1000m,
        SalaryMax: 2000m,
        TargetHeadcount: 2,
        ClosingDate: null);

    private static Requisition Create(RequisitionStatus status, long version, DateOnly? closingDate = null) => new(
        id: 42,
        jobCode: "REQ-2026-00042",
        title: "Senior Engineer",
        departmentId: DepartmentId,
        positionId: 10,
        description: "Build the platform",
        requirements: null,
        location: "Hanoi",
        EmploymentType.FullTime,
        salaryMin: 1000m,
        salaryMax: 2000m,
        targetHeadcount: 2,
        status,
        closingDate,
        publishedAt: null,
        createdBy: CreatorUserId,
        version,
        createdAt: Now.AddDays(-2),
        updatedAt: Now.AddMinutes(-5));

    private sealed class FakeRequisitionRepository : IRequisitionRepository
    {
        public Requisition? Requisition { get; init; }
        public HashSet<long> DepartmentIds { get; init; } = [DepartmentId, OtherDepartmentId];
        public HashSet<long> PositionIds { get; init; } = [10];
        public bool HasOpenOffers { get; init; }
        public bool SaveSucceeds { get; init; } = true;
        public PagedResult<Requisition>? SearchResult { get; init; }

        public int InsertCalls { get; private set; }
        public int ReplaceCalls { get; private set; }
        public int TransitionCalls { get; private set; }
        public int OpenOfferChecks { get; private set; }
        public int DepartmentLookups { get; private set; }
        public int PositionLookups { get; private set; }
        public Requisition? LastInserted { get; private set; }
        public CoreHrActor? LastActor { get; private set; }
        public long? LastExpectedVersion { get; private set; }
        public IReadOnlyList<string>? LastChangedFields { get; private set; }
        public RequisitionStatus? LastPreviousStatus { get; private set; }
        public RequisitionAction? LastAction { get; private set; }
        public string? LastReason { get; private set; }
        public RequisitionSearchQuery? LastSearchQuery { get; private set; }
        public CoreHrActor? LastSearchActor { get; private set; }

        public Task<PagedResult<Requisition>> SearchAsync(RequisitionSearchQuery query, CoreHrActor actor, CancellationToken cancellationToken)
        {
            LastSearchQuery = query;
            LastSearchActor = actor;
            return Task.FromResult(SearchResult ?? PagedResult<Requisition>.Empty(query.Page));
        }

        public Task<Requisition?> GetByIdAsync(long requisitionId, CoreHrActor actor, CancellationToken cancellationToken) =>
            Task.FromResult(Requisition?.Id == requisitionId ? Requisition : null);

        public Task<bool> DepartmentExistsAsync(long departmentId, CancellationToken cancellationToken)
        {
            DepartmentLookups++;
            return Task.FromResult(DepartmentIds.Contains(departmentId));
        }

        public Task<bool> PositionExistsAsync(long positionId, CancellationToken cancellationToken)
        {
            PositionLookups++;
            return Task.FromResult(PositionIds.Contains(positionId));
        }

        public Task<bool> HasOpenOffersAsync(long requisitionId, CancellationToken cancellationToken)
        {
            OpenOfferChecks++;
            return Task.FromResult(HasOpenOffers);
        }

        public Task<Requisition> InsertAsync(Requisition draft, CoreHrActor actor, CancellationToken cancellationToken)
        {
            InsertCalls++;
            LastInserted = draft;
            LastActor = actor;
            return Task.FromResult(new Requisition(
                100,
                Qlns.BusinessLogic.Modules.Recruitment.Requisitions.Requisition.BuildJobCode(draft.CreatedAt.Year, 100),
                draft.Title,
                draft.DepartmentId,
                draft.PositionId,
                draft.Description,
                draft.Requirements,
                draft.Location,
                draft.EmploymentType,
                draft.SalaryMin,
                draft.SalaryMax,
                draft.TargetHeadcount,
                draft.Status,
                draft.ClosingDate,
                draft.PublishedAt,
                draft.CreatedBy,
                draft.Version,
                draft.CreatedAt,
                draft.UpdatedAt));
        }

        public Task<bool> ReplaceAsync(Requisition requisition, long expectedVersion, IReadOnlyList<string> changedFields, CoreHrActor actor, CancellationToken cancellationToken)
        {
            ReplaceCalls++;
            LastExpectedVersion = expectedVersion;
            LastChangedFields = changedFields;
            LastActor = actor;
            return Task.FromResult(SaveSucceeds);
        }

        public Task<bool> SaveTransitionAsync(Requisition requisition, RequisitionStatus previousStatus, RequisitionAction action, long expectedVersion, string? reason, CoreHrActor actor, CancellationToken cancellationToken)
        {
            TransitionCalls++;
            LastPreviousStatus = previousStatus;
            LastAction = action;
            LastExpectedVersion = expectedVersion;
            LastReason = reason;
            LastActor = actor;
            return Task.FromResult(SaveSucceeds);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
