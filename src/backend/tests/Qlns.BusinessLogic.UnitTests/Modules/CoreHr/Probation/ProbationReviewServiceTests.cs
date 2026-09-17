using Qlns.BusinessLogic.Modules.CoreHr.EmployeeEvents;
using Qlns.BusinessLogic.Modules.CoreHr.Probation;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Xunit;
using static Qlns.BusinessLogic.UnitTests.Modules.CoreHr.Probation.ProbationTestData;

namespace Qlns.BusinessLogic.UnitTests.Modules.CoreHr.Probation;

public sealed class ProbationReviewServiceTests
{
    [Fact]
    public async Task GetCurrentAsync_VisibleEmployee_ReturnsReviewWithOverdueFlag()
    {
        var repository = new FakeProbationReviewRepository
        {
            Current = CreateReview(ProbationReviewStatus.Pending, reviewDueDate: Today.AddDays(-3))
        };
        var service = Service(repository);

        var view = await service.GetCurrentAsync(EmployeeId, Reader(), CancellationToken.None);

        Assert.Equal(ReviewId, view.Review.Id);
        Assert.True(view.Overdue);
    }

    [Fact]
    public async Task GetCurrentAsync_SelfScopeOwnReview_IsVisible()
    {
        var repository = new FakeProbationReviewRepository { Current = CreateReview(ProbationReviewStatus.Pending) };
        var service = Service(repository);

        var view = await service.GetCurrentAsync(
            EmployeeId, Actor(userId: 50, scope: CoreHrDataScope.Self, employeeId: EmployeeId, permissions: ProbationPermissions.Read), CancellationToken.None);

        Assert.False(view.Overdue);
    }

    [Fact]
    public async Task GetCurrentAsync_UnknownOrOutOfScopeEmployee_ThrowsNotFoundEmployee()
    {
        var repository = new FakeProbationReviewRepository { Current = CreateReview(ProbationReviewStatus.Pending) };
        var service = Service(repository);

        var unknown = await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            service.GetCurrentAsync(999, Reader(), CancellationToken.None));
        Assert.Equal("Employee", unknown.Resource);

        var outOfScope = await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            service.GetCurrentAsync(EmployeeId, Reader(CoreHrDataScope.Departments(99)), CancellationToken.None));
        Assert.Equal("Employee", outOfScope.Resource);
    }

    [Fact]
    public async Task GetCurrentAsync_NoReview_ThrowsNotFoundProbationReview()
    {
        var service = Service(new FakeProbationReviewRepository { Current = null });

        var exception = await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            service.GetCurrentAsync(EmployeeId, Reader(), CancellationToken.None));

        Assert.Equal(ProbationReviewService.ResourceName, exception.Resource);
        Assert.Equal(EmployeeId, exception.Id);
    }

    [Fact]
    public async Task SubmitAsync_ByAssignedReviewer_SavesOnce()
    {
        var repository = new FakeProbationReviewRepository { Current = CreateReview(ProbationReviewStatus.Pending, version: 1) };
        var service = Service(repository);

        var view = await service.SubmitAsync(
            new SubmitProbationReviewCommand(EmployeeId, 1, Write(), Reader(userId: ReviewerUserId)), CancellationToken.None);

        Assert.Equal(ProbationReviewStatus.InReview, view.Review.Status);
        Assert.Equal(2, view.Review.Version);
        Assert.Equal(1, repository.SaveAssessmentCalls);
        Assert.Equal(ProbationReviewStatus.Pending, repository.LastPreviousStatus);
        Assert.Equal(1, repository.LastExpectedVersion);
    }

    [Fact]
    public async Task SubmitAsync_ByManagePermissionHolder_SavesOnce()
    {
        var repository = new FakeProbationReviewRepository { Current = CreateReview(ProbationReviewStatus.Pending, version: 1) };
        var service = Service(repository);

        await service.SubmitAsync(
            new SubmitProbationReviewCommand(EmployeeId, 1, Write(), Actor(userId: 99, permissions: [ProbationPermissions.Read, ProbationPermissions.Manage])),
            CancellationToken.None);

        Assert.Equal(1, repository.SaveAssessmentCalls);
    }

    [Fact]
    public async Task SubmitAsync_ByOtherUser_ThrowsNotReviewerWithoutSave()
    {
        var repository = new FakeProbationReviewRepository { Current = CreateReview(ProbationReviewStatus.Pending, version: 1) };
        var service = Service(repository);

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() => service.SubmitAsync(
            new SubmitProbationReviewCommand(EmployeeId, 1, Write(), Reader(userId: 99)), CancellationToken.None));

        Assert.Equal(ProbationReview.NotReviewerCode, exception.Code);
        Assert.Equal(0, repository.SaveAssessmentCalls);
    }

    [Fact]
    public async Task SubmitAsync_StaleVersion_ThrowsConcurrencyWithoutSave()
    {
        var repository = new FakeProbationReviewRepository { Current = CreateReview(ProbationReviewStatus.Pending, version: 3) };
        var service = Service(repository);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => service.SubmitAsync(
            new SubmitProbationReviewCommand(EmployeeId, 2, Write(), Reader(userId: ReviewerUserId)), CancellationToken.None));

        Assert.Equal(0, repository.SaveAssessmentCalls);
    }

    [Fact]
    public async Task SubmitAsync_SaveLosesRace_ThrowsConcurrency()
    {
        var repository = new FakeProbationReviewRepository { Current = CreateReview(ProbationReviewStatus.Pending, version: 1), SaveSucceeds = false };
        var service = Service(repository);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => service.SubmitAsync(
            new SubmitProbationReviewCommand(EmployeeId, 1, Write(), Reader(userId: ReviewerUserId)), CancellationToken.None));

        Assert.Equal(1, repository.SaveAssessmentCalls);
    }

    [Fact]
    public async Task SubmitAsync_InvalidPayload_ThrowsValidationWithoutSave()
    {
        var repository = new FakeProbationReviewRepository { Current = CreateReview(ProbationReviewStatus.Pending, version: 1) };
        var service = Service(repository);

        await Assert.ThrowsAsync<CoreHrValidationException>(() => service.SubmitAsync(
            new SubmitProbationReviewCommand(EmployeeId, 1, Write(improvements: null, recommendedOutcome: "terminated"), Reader(userId: ReviewerUserId)),
            CancellationToken.None));

        Assert.Equal(0, repository.SaveAssessmentCalls);
    }

    [Fact]
    public async Task SearchAsync_PassesTodayAndComputesOverdue()
    {
        var repository = new FakeProbationReviewRepository();
        repository.Reviews.Add(CreateReview(ProbationReviewStatus.Pending, id: 1, reviewDueDate: Today.AddDays(-1)));
        repository.Reviews.Add(CreateReview(ProbationReviewStatus.InReview, id: 2, reviewDueDate: Today.AddDays(5)));
        repository.Reviews.Add(CreateReview(ProbationReviewStatus.Decided, id: 3, reviewDueDate: Today.AddDays(-10)));
        var service = Service(repository);
        var query = new ProbationReviewSearchQuery(null, null, null, PageRequest.Default);

        var result = await service.SearchAsync(query, Reader(), CancellationToken.None);

        Assert.Same(query, repository.LastSearchQuery);
        Assert.Equal(Today, repository.LastSearchToday);
        Assert.Equal(3, result.TotalItems);
        Assert.Equal([true, false, false], result.Items.Select(view => view.Overdue));
    }

    [Fact]
    public async Task Decide_WithPermission_SavesDecisionPlanOnce()
    {
        var repository = new FakeProbationReviewRepository();
        repository.Reviews.Add(CreateReview(ProbationReviewStatus.InReview, version: 2));
        var service = Service(repository);

        var view = await service.TransitionAsync(
            Transition(ProbationReviewAction.Decide, 2, Decider(), Decision("confirmed")), CancellationToken.None);

        Assert.Equal(ProbationReviewStatus.Decided, view.Review.Status);
        Assert.Equal(3, view.Review.Version);
        Assert.Equal(HrManagerUserId, view.Review.DecidedBy);
        Assert.Equal(2001, view.Review.EmployeeEventId);
        Assert.False(view.Overdue);
        Assert.Equal(1, repository.SaveDecisionCalls);
        Assert.Equal(2, repository.LastExpectedVersion);
        Assert.Equal(EmployeeEventType.ProbationConfirmation, repository.LastPlan!.EmployeeEvent.EventType);
        Assert.Null(repository.LastPlan.OffboardingCase);
    }

    [Fact]
    public async Task Decide_Terminated_PlanCarriesOffboardingCase()
    {
        var repository = new FakeProbationReviewRepository();
        repository.Reviews.Add(CreateReview(ProbationReviewStatus.InReview, version: 2, improvements: "Missed targets"));
        var service = Service(repository);

        await service.TransitionAsync(
            Transition(ProbationReviewAction.Decide, 2, Decider(), Decision("terminated")), CancellationToken.None);

        Assert.Equal(EmployeeEventType.Termination, repository.LastPlan!.EmployeeEvent.EventType);
        Assert.NotNull(repository.LastPlan.OffboardingCase);
        Assert.Equal("Missed targets", repository.LastPlan.OffboardingCase.Reason);
    }

    [Fact]
    public async Task Decide_WithoutDecidePermission_ThrowsForbiddenWithoutSave()
    {
        var repository = new FakeProbationReviewRepository();
        repository.Reviews.Add(CreateReview(ProbationReviewStatus.InReview, version: 2));
        var service = Service(repository);

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() => service.TransitionAsync(
            Transition(ProbationReviewAction.Decide, 2, Manager(), Decision()), CancellationToken.None));

        Assert.Equal(ProbationReviewService.DecideForbiddenCode, exception.Code);
        Assert.Equal(0, repository.SaveDecisionCalls);
    }

    [Fact]
    public async Task Decide_AlreadyDecided_ReturnsCurrentStateWithoutSavingEvenWithStaleVersion()
    {
        var repository = new FakeProbationReviewRepository();
        repository.Reviews.Add(CreateReview(ProbationReviewStatus.Decided, version: 4, employeeEventId: 900));
        var service = Service(repository);

        var view = await service.TransitionAsync(
            Transition(ProbationReviewAction.Decide, 3, Decider(), Decision("terminated")), CancellationToken.None);

        Assert.Equal(ProbationReviewStatus.Decided, view.Review.Status);
        Assert.Equal(ProbationOutcome.Confirmed, view.Review.Outcome);
        Assert.Equal(900, view.Review.EmployeeEventId);
        Assert.Equal(4, view.Review.Version);
        Assert.Equal(0, repository.SaveDecisionCalls);
    }

    [Fact]
    public async Task Decide_Pending_ThrowsNotReviewedWithoutSave()
    {
        var repository = new FakeProbationReviewRepository();
        repository.Reviews.Add(CreateReview(ProbationReviewStatus.Pending, version: 1));
        var service = Service(repository);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() => service.TransitionAsync(
            Transition(ProbationReviewAction.Decide, 1, Decider(), Decision()), CancellationToken.None));

        Assert.Equal(ProbationReview.NotReviewedCode, exception.Code);
        Assert.Equal(0, repository.SaveDecisionCalls);
    }

    [Fact]
    public async Task Decide_MissingEffectiveDate_ThrowsValidationWithoutSave()
    {
        var repository = new FakeProbationReviewRepository();
        repository.Reviews.Add(CreateReview(ProbationReviewStatus.InReview, version: 2));
        var service = Service(repository);

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() => service.TransitionAsync(
            Transition(ProbationReviewAction.Decide, 2, Decider(), new ProbationDecision("confirmed", null, null)), CancellationToken.None));

        Assert.Equal(["effectiveDate"], exception.Errors.Keys);
        Assert.Equal(0, repository.SaveDecisionCalls);
    }

    [Fact]
    public async Task Decide_StaleVersion_ThrowsConcurrencyWithoutSave()
    {
        var repository = new FakeProbationReviewRepository();
        repository.Reviews.Add(CreateReview(ProbationReviewStatus.InReview, version: 3));
        var service = Service(repository);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => service.TransitionAsync(
            Transition(ProbationReviewAction.Decide, 2, Decider(), Decision()), CancellationToken.None));

        Assert.Equal(0, repository.SaveDecisionCalls);
    }

    [Fact]
    public async Task Decide_SaveLosesRace_ThrowsConcurrency()
    {
        var repository = new FakeProbationReviewRepository { SaveSucceeds = false };
        repository.Reviews.Add(CreateReview(ProbationReviewStatus.InReview, version: 2));
        var service = Service(repository);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => service.TransitionAsync(
            Transition(ProbationReviewAction.Decide, 2, Decider(), Decision()), CancellationToken.None));

        Assert.Equal(1, repository.SaveDecisionCalls);
    }

    [Fact]
    public async Task Transition_MissingOrOutOfScopeReview_ThrowsNotFound()
    {
        var repository = new FakeProbationReviewRepository();
        repository.Reviews.Add(CreateReview(ProbationReviewStatus.InReview, version: 2));
        var service = Service(repository);

        var missing = await Assert.ThrowsAsync<CoreHrNotFoundException>(() => service.TransitionAsync(
            new TransitionProbationReviewCommand(999, ProbationReviewAction.Decide, 2, Decision(), Decider()), CancellationToken.None));
        Assert.Equal(ProbationReviewService.ResourceName, missing.Resource);

        await Assert.ThrowsAsync<CoreHrNotFoundException>(() => service.TransitionAsync(
            Transition(ProbationReviewAction.Decide, 2, Decider(CoreHrDataScope.Departments(99)), Decision()), CancellationToken.None));
    }

    [Fact]
    public async Task Cancel_WithManage_SavesTrimmedReason()
    {
        var repository = new FakeProbationReviewRepository();
        repository.Reviews.Add(CreateReview(ProbationReviewStatus.InReview, version: 2));
        var service = Service(repository);

        var view = await service.TransitionAsync(
            Transition(ProbationReviewAction.Cancel, 2, Manager(), new ProbationDecision(null, null, "  Contract withdrawn ")), CancellationToken.None);

        Assert.Equal(ProbationReviewStatus.Cancelled, view.Review.Status);
        Assert.Equal(1, repository.SaveCancellationCalls);
        Assert.Equal("Contract withdrawn", repository.LastReason);
        Assert.Equal(ProbationReviewStatus.InReview, repository.LastPreviousStatus);
    }

    [Fact]
    public async Task Cancel_WithoutManage_ThrowsForbiddenWithoutSave()
    {
        var repository = new FakeProbationReviewRepository();
        repository.Reviews.Add(CreateReview(ProbationReviewStatus.InReview, version: 2));
        var service = Service(repository);

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() => service.TransitionAsync(
            Transition(ProbationReviewAction.Cancel, 2, Decider(), new ProbationDecision(null, null, "reason")), CancellationToken.None));

        Assert.Equal(ProbationReviewService.ManageForbiddenCode, exception.Code);
        Assert.Equal(0, repository.SaveCancellationCalls);
    }

    [Fact]
    public async Task Cancel_WithoutReason_ThrowsValidationWithoutSave()
    {
        var repository = new FakeProbationReviewRepository();
        repository.Reviews.Add(CreateReview(ProbationReviewStatus.Pending, version: 1));
        var service = Service(repository);

        await Assert.ThrowsAsync<CoreHrValidationException>(() => service.TransitionAsync(
            Transition(ProbationReviewAction.Cancel, 1, Manager(), ProbationDecision.Empty), CancellationToken.None));

        Assert.Equal(0, repository.SaveCancellationCalls);
    }

    [Fact]
    public async Task Unlock_UnappliedEvent_SavesUnlockWithEventId()
    {
        var repository = new FakeProbationReviewRepository();
        repository.Reviews.Add(CreateReview(ProbationReviewStatus.Decided, version: 4, employeeEventId: 900));
        repository.EventStatuses[900] = EmployeeEventStatus.Approved;
        var service = Service(repository);

        var view = await service.TransitionAsync(
            Transition(ProbationReviewAction.Unlock, 4, Decider(), new ProbationDecision(null, null, " Wrong date ")), CancellationToken.None);

        Assert.Equal(ProbationReviewStatus.InReview, view.Review.Status);
        Assert.Null(view.Review.EmployeeEventId);
        Assert.Equal(1, repository.SaveUnlockCalls);
        Assert.Equal(900, repository.LastUnlockedEventId);
        Assert.Equal("Wrong date", repository.LastReason);
        Assert.Equal(4, repository.LastExpectedVersion);
    }

    [Fact]
    public async Task Unlock_AppliedEvent_ThrowsEventAppliedWithoutSave()
    {
        var repository = new FakeProbationReviewRepository();
        repository.Reviews.Add(CreateReview(ProbationReviewStatus.Decided, version: 4, employeeEventId: 900));
        repository.EventStatuses[900] = EmployeeEventStatus.Applied;
        var service = Service(repository);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() => service.TransitionAsync(
            Transition(ProbationReviewAction.Unlock, 4, Decider(), new ProbationDecision(null, null, "reason")), CancellationToken.None));

        Assert.Equal(ProbationReview.EventAppliedCode, exception.Code);
        Assert.Equal(0, repository.SaveUnlockCalls);
    }

    [Fact]
    public async Task Unlock_WithoutDecidePermission_ThrowsForbidden()
    {
        var repository = new FakeProbationReviewRepository();
        repository.Reviews.Add(CreateReview(ProbationReviewStatus.Decided, version: 4));
        var service = Service(repository);

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() => service.TransitionAsync(
            Transition(ProbationReviewAction.Unlock, 4, Manager(), new ProbationDecision(null, null, "reason")), CancellationToken.None));

        Assert.Equal(ProbationReviewService.DecideForbiddenCode, exception.Code);
        Assert.Equal(0, repository.SaveUnlockCalls);
    }

    [Fact]
    public async Task Unlock_SaveLosesRace_ThrowsConcurrency()
    {
        var repository = new FakeProbationReviewRepository { SaveSucceeds = false };
        repository.Reviews.Add(CreateReview(ProbationReviewStatus.Decided, version: 4, employeeEventId: 900));
        repository.EventStatuses[900] = EmployeeEventStatus.Approved;
        var service = Service(repository);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => service.TransitionAsync(
            Transition(ProbationReviewAction.Unlock, 4, Decider(), new ProbationDecision(null, null, "reason")), CancellationToken.None));

        Assert.Equal(1, repository.SaveUnlockCalls);
    }

    private static ProbationReviewService Service(FakeProbationReviewRepository repository) =>
        new(repository, new FixedTimeProvider(Now));

    private static CoreHrActor Reader(CoreHrDataScope? scope = null, long userId = HrManagerUserId) =>
        Actor(userId, scope, permissions: ProbationPermissions.Read);

    private static CoreHrActor Manager() =>
        Actor(permissions: [ProbationPermissions.Read, ProbationPermissions.Manage]);

    private static CoreHrActor Decider(CoreHrDataScope? scope = null) =>
        Actor(scope: scope, permissions: [ProbationPermissions.Read, ProbationPermissions.Decide]);

    private static TransitionProbationReviewCommand Transition(
        ProbationReviewAction action,
        long expectedVersion,
        CoreHrActor actor,
        ProbationDecision decision) => new(ReviewId, action, expectedVersion, decision, actor);

    private sealed class FakeProbationReviewRepository : IProbationReviewRepository
    {
        private long _nextEventId = 2001;

        public Dictionary<long, long> EmployeeDepartments { get; } = new() { [EmployeeId] = DepartmentId };
        public ProbationReview? Current { get; init; }
        public List<ProbationReview> Reviews { get; } = [];
        public Dictionary<long, EmployeeEventStatus> EventStatuses { get; } = [];
        public bool SaveSucceeds { get; init; } = true;

        public int SaveAssessmentCalls { get; private set; }
        public int SaveDecisionCalls { get; private set; }
        public int SaveCancellationCalls { get; private set; }
        public int SaveUnlockCalls { get; private set; }
        public ProbationReviewStatus? LastPreviousStatus { get; private set; }
        public long? LastExpectedVersion { get; private set; }
        public string? LastReason { get; private set; }
        public long? LastUnlockedEventId { get; private set; }
        public ProbationDecisionPlan? LastPlan { get; private set; }
        public ProbationReviewSearchQuery? LastSearchQuery { get; private set; }
        public DateOnly? LastSearchToday { get; private set; }

        public Task<long?> GetEmployeeDepartmentAsync(long employeeId, CancellationToken cancellationToken) =>
            Task.FromResult(EmployeeDepartments.TryGetValue(employeeId, out var department) ? department : (long?)null);

        public Task<ProbationReviewEntry?> GetCurrentForEmployeeAsync(long employeeId, CancellationToken cancellationToken) =>
            Task.FromResult(Current is not null && Current.EmployeeId == employeeId
                ? new ProbationReviewEntry(Current, EmployeeDepartments[employeeId])
                : null);

        public Task<ProbationReviewEntry?> GetByIdAsync(long reviewId, CancellationToken cancellationToken)
        {
            var review = Reviews.SingleOrDefault(r => r.Id == reviewId);
            return Task.FromResult(review is null ? null : new ProbationReviewEntry(review, EmployeeDepartments[review.EmployeeId]));
        }

        public Task<PagedResult<ProbationReview>> SearchAsync(
            ProbationReviewSearchQuery query,
            CoreHrActor actor,
            DateOnly today,
            CancellationToken cancellationToken)
        {
            LastSearchQuery = query;
            LastSearchToday = today;
            return Task.FromResult(new PagedResult<ProbationReview>(Reviews, query.Page.Page, query.Page.PageSize, Reviews.Count));
        }

        public Task<EmployeeEventStatus?> GetEmployeeEventStatusAsync(long employeeEventId, CancellationToken cancellationToken) =>
            Task.FromResult(EventStatuses.TryGetValue(employeeEventId, out var status) ? status : (EmployeeEventStatus?)null);

        public Task<bool> SaveAssessmentAsync(
            ProbationReview review,
            ProbationReviewStatus previousStatus,
            long expectedVersion,
            CoreHrActor actor,
            CancellationToken cancellationToken)
        {
            SaveAssessmentCalls++;
            LastPreviousStatus = previousStatus;
            LastExpectedVersion = expectedVersion;
            return Task.FromResult(SaveSucceeds);
        }

        public Task<bool> SaveDecisionAsync(
            ProbationReview review,
            ProbationDecisionPlan plan,
            long expectedVersion,
            CoreHrActor actor,
            CancellationToken cancellationToken)
        {
            SaveDecisionCalls++;
            LastPlan = plan;
            LastExpectedVersion = expectedVersion;
            if (SaveSucceeds)
            {
                review.LinkEmployeeEvent(_nextEventId++);
            }

            return Task.FromResult(SaveSucceeds);
        }

        public Task<bool> SaveCancellationAsync(
            ProbationReview review,
            ProbationReviewStatus previousStatus,
            long expectedVersion,
            string reason,
            CoreHrActor actor,
            CancellationToken cancellationToken)
        {
            SaveCancellationCalls++;
            LastPreviousStatus = previousStatus;
            LastExpectedVersion = expectedVersion;
            LastReason = reason;
            return Task.FromResult(SaveSucceeds);
        }

        public Task<bool> SaveUnlockAsync(
            ProbationReview review,
            long employeeEventId,
            long expectedVersion,
            string reason,
            CoreHrActor actor,
            CancellationToken cancellationToken)
        {
            SaveUnlockCalls++;
            LastUnlockedEventId = employeeEventId;
            LastExpectedVersion = expectedVersion;
            LastReason = reason;
            return Task.FromResult(SaveSucceeds);
        }
    }
}
