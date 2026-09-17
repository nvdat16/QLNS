using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Interviews;
using Xunit;

namespace Qlns.BusinessLogic.UnitTests.Modules.Recruitment.Interviews;

public sealed class InterviewServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 7, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Tomorrow = Now.AddDays(1);

    private static readonly InterviewApplication VisibleApplication = new(10, 20, 30, 4, "tech_interview");

    [Fact]
    public async Task ScheduleAsync_Valid_InsertsAndReturnsPersistedInterview()
    {
        var repository = new FakeInterviewRepository { Application = VisibleApplication };
        var service = new InterviewService(repository, new FixedTimeProvider(Now));

        var created = await service.ScheduleAsync(new ScheduleInterviewCommand(ValidWrite(), Actor()), CancellationToken.None);

        Assert.Equal(100, created.Id);
        Assert.Equal(InterviewStatus.Scheduled, created.Status);
        Assert.Equal([7L, 8L], created.PanelUserIds);
        Assert.Equal(1, repository.InsertCalls);
        Assert.NotNull(repository.Inserted);
        Assert.Equal(0, repository.Inserted.Id);
        Assert.Equal(10, repository.Inserted.ApplicationId);
        Assert.Null(repository.LastExcludeInterviewId);
    }

    [Fact]
    public async Task ScheduleAsync_ApplicationNotVisible_ThrowsNotFoundWithoutInserting()
    {
        var repository = new FakeInterviewRepository { Application = null };
        var service = new InterviewService(repository, new FixedTimeProvider(Now));

        var exception = await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            service.ScheduleAsync(new ScheduleInterviewCommand(ValidWrite(), Actor()), CancellationToken.None));

        Assert.Equal("Application", exception.Resource);
        Assert.Equal(10, exception.Id);
        Assert.Equal(0, repository.InsertCalls);
    }

    [Fact]
    public async Task ScheduleAsync_WithoutManagePermission_ThrowsForbidden()
    {
        var repository = new FakeInterviewRepository { Application = VisibleApplication };
        var service = new InterviewService(repository, new FixedTimeProvider(Now));

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() =>
            service.ScheduleAsync(new ScheduleInterviewCommand(ValidWrite(), Actor(1, InterviewPermissions.Read)), CancellationToken.None));

        Assert.Equal(InterviewService.ManageForbiddenCode, exception.Code);
        Assert.Equal(0, repository.InsertCalls);
    }

    [Theory]
    [InlineData("sourced_applied")]
    [InlineData("offer_letter")]
    [InlineData("hired_ready")]
    [InlineData("rejected")]
    public async Task ScheduleAsync_StageNotInterviewable_ThrowsBusinessRule(string stage)
    {
        var repository = new FakeInterviewRepository { Application = VisibleApplication with { Stage = stage } };
        var service = new InterviewService(repository, new FixedTimeProvider(Now));

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            service.ScheduleAsync(new ScheduleInterviewCommand(ValidWrite(), Actor()), CancellationToken.None));

        Assert.Equal(InterviewService.StageNotInterviewableCode, exception.Code);
        Assert.Equal(stage, exception.Details["currentStage"]);
        Assert.Equal(0, repository.InsertCalls);
    }

    [Fact]
    public async Task ScheduleAsync_InvalidPayload_ThrowsValidationBeforeUserLookup()
    {
        var repository = new FakeInterviewRepository { Application = VisibleApplication };
        var service = new InterviewService(repository, new FixedTimeProvider(Now));

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() =>
            service.ScheduleAsync(new ScheduleInterviewCommand(ValidWrite() with { EndsAt = Tomorrow.AddHours(-1) }, Actor()), CancellationToken.None));

        Assert.True(exception.Errors.ContainsKey("endsAt"));
        Assert.Equal(0, repository.FindActiveUsersCalls);
        Assert.Equal(0, repository.InsertCalls);
    }

    [Fact]
    public async Task ScheduleAsync_UnknownOrInactiveInterviewer_ThrowsValidation()
    {
        var repository = new FakeInterviewRepository { Application = VisibleApplication, ActiveUserIds = [7] };
        var service = new InterviewService(repository, new FixedTimeProvider(Now));

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() =>
            service.ScheduleAsync(new ScheduleInterviewCommand(ValidWrite(), Actor()), CancellationToken.None));

        Assert.True(exception.Errors.ContainsKey("interviewerUserIds"));
        Assert.Contains("8", exception.Errors["interviewerUserIds"][0]);
        Assert.Equal(0, repository.InsertCalls);
    }

    [Fact]
    public async Task ScheduleAsync_InterviewerDoubleBooked_ThrowsConflictWithDetails()
    {
        var repository = new FakeInterviewRepository
        {
            Application = VisibleApplication,
            Overlapping = [new ScheduledInterviewSummary(55, new InterviewSlot(Tomorrow.AddMinutes(-30), Tomorrow.AddMinutes(30)), "Room Z", [8, 9])]
        };
        var service = new InterviewService(repository, new FixedTimeProvider(Now));

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            service.ScheduleAsync(new ScheduleInterviewCommand(ValidWrite(), Actor()), CancellationToken.None));

        Assert.Equal(InterviewConflict.InterviewerConflictCode, exception.Code);
        Assert.Equal(8L, exception.Details["interviewerUserId"]);
        Assert.Equal(55L, exception.Details["conflictingInterviewId"]);
        Assert.Equal(Tomorrow.AddMinutes(-30), exception.Details["startsAt"]);
        Assert.Equal(Tomorrow.AddMinutes(30), exception.Details["endsAt"]);
        Assert.Equal(0, repository.InsertCalls);
    }

    [Fact]
    public async Task ScheduleAsync_LocationDoubleBooked_ThrowsLocationConflict()
    {
        var repository = new FakeInterviewRepository
        {
            Application = VisibleApplication,
            Overlapping = [new ScheduledInterviewSummary(56, new InterviewSlot(Tomorrow, Tomorrow.AddHours(1)), "room a", [9])]
        };
        var service = new InterviewService(repository, new FixedTimeProvider(Now));

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            service.ScheduleAsync(new ScheduleInterviewCommand(ValidWrite(), Actor()), CancellationToken.None));

        Assert.Equal(InterviewConflict.LocationConflictCode, exception.Code);
        Assert.Equal(56L, exception.Details["conflictingInterviewId"]);
        Assert.Equal(0, repository.InsertCalls);
    }

    [Fact]
    public async Task TransitionAsync_Complete_ByPanelistWithoutManage_Succeeds()
    {
        var interview = InterviewTests.CreateInterview(InterviewStatus.Scheduled, version: 2, startsAt: Now.AddHours(-1));
        var repository = new FakeInterviewRepository { Interview = interview };
        var service = new InterviewService(repository, new FixedTimeProvider(Now));

        var updated = await service.TransitionAsync(
            new TransitionInterviewCommand(42, InterviewAction.Complete, 2, null, Actor(8, InterviewPermissions.Read)),
            CancellationToken.None);

        Assert.Equal(InterviewStatus.Completed, updated.Status);
        Assert.Equal(3, updated.Version);
        Assert.Equal(1, repository.SaveTransitionCalls);
        Assert.Equal(InterviewAction.Complete, repository.LastAction);
        Assert.Equal(2, repository.LastExpectedVersion);
        Assert.Equal(InterviewStatus.Scheduled, repository.LastBefore?.Status);
        Assert.Equal(2, repository.LastBefore?.Version);
    }

    [Fact]
    public async Task TransitionAsync_Complete_ByNonPanelistWithoutManage_ThrowsForbidden()
    {
        var interview = InterviewTests.CreateInterview(InterviewStatus.Scheduled, version: 1, startsAt: Now.AddHours(-1));
        var repository = new FakeInterviewRepository { Interview = interview };
        var service = new InterviewService(repository, new FixedTimeProvider(Now));

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() => service.TransitionAsync(
            new TransitionInterviewCommand(42, InterviewAction.Complete, 1, null, Actor(99, InterviewPermissions.Read)),
            CancellationToken.None));

        Assert.Equal(InterviewService.ManageForbiddenCode, exception.Code);
        Assert.Equal(0, repository.SaveTransitionCalls);
    }

    [Fact]
    public async Task TransitionAsync_Complete_BeforeStart_ThrowsNotStartedWithoutSaving()
    {
        var repository = new FakeInterviewRepository { Interview = InterviewTests.CreateInterview(InterviewStatus.Scheduled, version: 1) };
        var service = new InterviewService(repository, new FixedTimeProvider(Now));

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() => service.TransitionAsync(
            new TransitionInterviewCommand(42, InterviewAction.Complete, 1, null, Actor()),
            CancellationToken.None));

        Assert.Equal(Interview.NotStartedCode, exception.Code);
        Assert.Equal(0, repository.SaveTransitionCalls);
    }

    [Theory]
    [InlineData(InterviewAction.Reschedule)]
    [InlineData(InterviewAction.Cancel)]
    public async Task TransitionAsync_RescheduleOrCancel_ByPanelistWithoutManage_ThrowsForbidden(InterviewAction action)
    {
        var repository = new FakeInterviewRepository { Interview = InterviewTests.CreateInterview(InterviewStatus.Scheduled, version: 1) };
        var service = new InterviewService(repository, new FixedTimeProvider(Now));
        var change = new InterviewChange(Tomorrow.AddDays(2), Tomorrow.AddDays(2).AddHours(1), null, null, null, "reason");

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() => service.TransitionAsync(
            new TransitionInterviewCommand(42, action, 1, change, Actor(7, InterviewPermissions.Read)),
            CancellationToken.None));

        Assert.Equal(InterviewService.ManageForbiddenCode, exception.Code);
        Assert.Equal(0, repository.SaveTransitionCalls);
    }

    [Fact]
    public async Task TransitionAsync_Reschedule_RerunsConflictCheckExcludingItselfAndPassesPreviousSlot()
    {
        var interview = InterviewTests.CreateInterview(InterviewStatus.Scheduled, version: 3);
        var previousSlot = interview.Slot;
        var repository = new FakeInterviewRepository { Interview = interview };
        var service = new InterviewService(repository, new FixedTimeProvider(Now));
        var newStart = Tomorrow.AddDays(3);

        var updated = await service.TransitionAsync(
            new TransitionInterviewCommand(42, InterviewAction.Reschedule, 3, new InterviewChange(newStart, newStart.AddHours(1), null, null, null, null), Actor()),
            CancellationToken.None);

        Assert.Equal(newStart, updated.StartsAt);
        Assert.Equal(4, updated.Version);
        Assert.Equal(42, repository.LastExcludeInterviewId);
        Assert.Equal(InterviewAction.Reschedule, repository.LastAction);
        Assert.Equal(previousSlot, repository.LastBefore?.Slot);
        Assert.Equal(1, repository.SaveTransitionCalls);
    }

    [Fact]
    public async Task TransitionAsync_Reschedule_IntoConflict_ThrowsWithoutSaving()
    {
        var interview = InterviewTests.CreateInterview(InterviewStatus.Scheduled, version: 1);
        var newStart = Tomorrow.AddDays(3);
        var repository = new FakeInterviewRepository
        {
            Interview = interview,
            Overlapping = [new ScheduledInterviewSummary(77, new InterviewSlot(newStart, newStart.AddHours(1)), null, [7])]
        };
        var service = new InterviewService(repository, new FixedTimeProvider(Now));

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() => service.TransitionAsync(
            new TransitionInterviewCommand(42, InterviewAction.Reschedule, 1, new InterviewChange(newStart, newStart.AddHours(1), null, null, null, null), Actor()),
            CancellationToken.None));

        Assert.Equal(InterviewConflict.InterviewerConflictCode, exception.Code);
        Assert.Equal(0, repository.SaveTransitionCalls);
    }

    [Fact]
    public async Task TransitionAsync_Cancel_WithoutReason_ThrowsValidationWithoutSaving()
    {
        var repository = new FakeInterviewRepository { Interview = InterviewTests.CreateInterview(InterviewStatus.Scheduled, version: 1) };
        var service = new InterviewService(repository, new FixedTimeProvider(Now));

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() => service.TransitionAsync(
            new TransitionInterviewCommand(42, InterviewAction.Cancel, 1, null, Actor()),
            CancellationToken.None));

        Assert.True(exception.Errors.ContainsKey("reason"));
        Assert.Equal(0, repository.SaveTransitionCalls);
    }

    [Fact]
    public async Task TransitionAsync_Cancel_WithReason_SavesCancelledInterview()
    {
        var repository = new FakeInterviewRepository { Interview = InterviewTests.CreateInterview(InterviewStatus.Scheduled, version: 1) };
        var service = new InterviewService(repository, new FixedTimeProvider(Now));

        var updated = await service.TransitionAsync(
            new TransitionInterviewCommand(42, InterviewAction.Cancel, 1, new InterviewChange(null, null, null, null, null, " Candidate withdrew "), Actor()),
            CancellationToken.None);

        Assert.Equal(InterviewStatus.Cancelled, updated.Status);
        Assert.Equal("Candidate withdrew", updated.CancellationReason);
        Assert.Equal(InterviewAction.Cancel, repository.LastAction);
        Assert.Equal(1, repository.SaveTransitionCalls);
    }

    [Fact]
    public async Task TransitionAsync_StaleVersion_ThrowsConcurrencyWithoutSaving()
    {
        var repository = new FakeInterviewRepository { Interview = InterviewTests.CreateInterview(InterviewStatus.Scheduled, version: 3) };
        var service = new InterviewService(repository, new FixedTimeProvider(Now));

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => service.TransitionAsync(
            new TransitionInterviewCommand(42, InterviewAction.Cancel, 2, new InterviewChange(null, null, null, null, null, "reason"), Actor()),
            CancellationToken.None));

        Assert.Equal(0, repository.SaveTransitionCalls);
    }

    [Fact]
    public async Task TransitionAsync_SaveLosesRace_ThrowsConcurrency()
    {
        var repository = new FakeInterviewRepository
        {
            Interview = InterviewTests.CreateInterview(InterviewStatus.Scheduled, version: 1),
            SaveSucceeds = false
        };
        var service = new InterviewService(repository, new FixedTimeProvider(Now));

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => service.TransitionAsync(
            new TransitionInterviewCommand(42, InterviewAction.Cancel, 1, new InterviewChange(null, null, null, null, null, "reason"), Actor()),
            CancellationToken.None));

        Assert.Equal(1, repository.SaveTransitionCalls);
    }

    [Fact]
    public async Task TransitionAsync_InterviewOutsideScopeOrMissing_ThrowsNotFound()
    {
        var repository = new FakeInterviewRepository { Interview = null };
        var service = new InterviewService(repository, new FixedTimeProvider(Now));

        var exception = await Assert.ThrowsAsync<CoreHrNotFoundException>(() => service.TransitionAsync(
            new TransitionInterviewCommand(42, InterviewAction.Complete, 1, null, Actor()),
            CancellationToken.None));

        Assert.Equal("Interview", exception.Resource);
        Assert.Equal(42, exception.Id);
    }

    [Fact]
    public async Task SearchAsync_PassesQueryAndActorToRepository()
    {
        var repository = new FakeInterviewRepository
        {
            SearchResult = new PagedResult<Interview>([InterviewTests.CreateInterview(InterviewStatus.Scheduled, 1)], 1, 20, 1)
        };
        var service = new InterviewService(repository, new FixedTimeProvider(Now));
        var query = new InterviewSearchQuery(10, 7, Now, Tomorrow, InterviewStatus.Scheduled, PageRequest.Default);

        var result = await service.SearchAsync(query, Actor(), CancellationToken.None);

        Assert.Same(query, repository.LastSearchQuery);
        Assert.Equal(1, result.TotalItems);
    }

    private static InterviewWrite ValidWrite() => new(
        ApplicationId: 10,
        InterviewType: "tech",
        StartsAt: Tomorrow,
        EndsAt: Tomorrow.AddHours(1),
        Timezone: "Asia/Ho_Chi_Minh",
        InterviewerUserIds: [7, 8],
        Location: "Room A",
        MeetingUrl: null);

    private static CoreHrActor Actor(long userId = 1, params string[] permissions) => new(
        UserId: userId,
        EmployeeId: null,
        DataScope: CoreHrDataScope.Organization,
        Permissions: permissions.Length == 0
            ? new HashSet<string> { InterviewPermissions.Read, InterviewPermissions.Manage }
            : permissions.ToHashSet(StringComparer.Ordinal),
        CorrelationId: "test-correlation");

    private sealed class FakeInterviewRepository : IInterviewRepository
    {
        public InterviewApplication? Application { get; init; }
        public Interview? Interview { get; init; }
        public HashSet<long> ActiveUserIds { get; init; } = [7, 8, 9];
        public List<ScheduledInterviewSummary> Overlapping { get; init; } = [];
        public bool SaveSucceeds { get; init; } = true;
        public PagedResult<Interview>? SearchResult { get; init; }

        public int InsertCalls { get; private set; }
        public int SaveTransitionCalls { get; private set; }
        public int FindActiveUsersCalls { get; private set; }
        public Interview? Inserted { get; private set; }
        public InterviewAction? LastAction { get; private set; }
        public InterviewSnapshot? LastBefore { get; private set; }
        public long? LastExpectedVersion { get; private set; }
        public long? LastExcludeInterviewId { get; private set; }
        public InterviewSearchQuery? LastSearchQuery { get; private set; }

        public Task<PagedResult<Interview>> SearchAsync(InterviewSearchQuery query, CoreHrActor actor, CancellationToken cancellationToken)
        {
            LastSearchQuery = query;
            return Task.FromResult(SearchResult ?? PagedResult<Interview>.Empty(query.Page));
        }

        public Task<Interview?> GetByIdAsync(long interviewId, CoreHrActor actor, CancellationToken cancellationToken) =>
            Task.FromResult(Interview?.Id == interviewId ? Interview : null);

        public Task<InterviewApplication?> GetApplicationAsync(long applicationId, CoreHrActor actor, CancellationToken cancellationToken) =>
            Task.FromResult(Application?.Id == applicationId ? Application : null);

        public Task<IReadOnlySet<long>> FindActiveUserIdsAsync(IReadOnlyCollection<long> userIds, CancellationToken cancellationToken)
        {
            FindActiveUsersCalls++;
            return Task.FromResult<IReadOnlySet<long>>(userIds.Where(ActiveUserIds.Contains).ToHashSet());
        }

        public Task<IReadOnlyList<ScheduledInterviewSummary>> ListOverlappingScheduledAsync(
            InterviewSlot slot,
            IReadOnlyCollection<long> panelUserIds,
            string? location,
            long? excludeInterviewId,
            CancellationToken cancellationToken)
        {
            LastExcludeInterviewId = excludeInterviewId;
            return Task.FromResult<IReadOnlyList<ScheduledInterviewSummary>>(Overlapping);
        }

        public Task<Interview> InsertAsync(Interview interview, CoreHrActor actor, CancellationToken cancellationToken)
        {
            InsertCalls++;
            Inserted = interview;
            return Task.FromResult(new Interview(
                100,
                interview.ApplicationId,
                interview.InterviewType,
                interview.Slot,
                interview.Timezone,
                interview.PanelUserIds,
                interview.Location,
                interview.MeetingUrl,
                interview.Status,
                interview.CancellationReason,
                interview.Version,
                interview.CreatedAt,
                interview.UpdatedAt));
        }

        public Task<bool> SaveTransitionAsync(
            Interview interview,
            InterviewAction action,
            InterviewSnapshot before,
            long expectedVersion,
            CoreHrActor actor,
            CancellationToken cancellationToken)
        {
            SaveTransitionCalls++;
            LastAction = action;
            LastBefore = before;
            LastExpectedVersion = expectedVersion;
            return Task.FromResult(SaveSucceeds);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
