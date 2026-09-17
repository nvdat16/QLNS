using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Applications;
using Qlns.BusinessLogic.Modules.Recruitment.Intake;
using Xunit;

namespace Qlns.BusinessLogic.UnitTests.Modules.Recruitment.Applications;

public sealed class RecruitmentPipelineServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 7, 0, 0, TimeSpan.Zero);

    private const long RequisitionId = 20;

    [Fact]
    public async Task GetAsync_Visible_ReturnsApplication()
    {
        var repository = new FakeRepository(ApplicationStage.AiScreening, version: 2);
        var service = CreateService(repository);

        var result = await service.GetAsync(42, Actor(ApplicationPermissions.Read), CancellationToken.None);

        Assert.Equal(42, result.Id);
        Assert.Equal(ApplicationStage.AiScreening, result.Stage);
    }

    [Fact]
    public async Task GetAsync_MissingOrOutOfScope_ThrowsNotFound()
    {
        var repository = new FakeRepository(application: null);
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            service.GetAsync(42, Actor(ApplicationPermissions.Read), CancellationToken.None));

        Assert.Equal("Application", exception.Resource);
        Assert.Equal(42, exception.Id);
    }

    [Fact]
    public async Task GetPipelineAsync_RequisitionNotVisible_ThrowsNotFoundWithoutLoadingColumns()
    {
        var repository = new FakeRepository(application: null) { RequisitionVisible = false };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            service.GetPipelineAsync(Query(), Actor(ApplicationPermissions.Read), CancellationToken.None));

        Assert.Equal("Requisition", exception.Resource);
        Assert.Equal(RequisitionId, exception.Id);
        Assert.Empty(repository.ColumnRequests);
    }

    [Fact]
    public async Task GetPipelineAsync_WithoutStage_ReturnsTheSixActiveColumnsInOrder()
    {
        var repository = new FakeRepository(application: null);
        var service = CreateService(repository);
        var query = Query();

        var pipeline = await service.GetPipelineAsync(query, Actor(ApplicationPermissions.Read), CancellationToken.None);

        Assert.Equal(RequisitionId, pipeline.RequisitionId);
        Assert.Equal(ApplicationStageNames.ActiveStages, pipeline.Columns.Select(column => column.Stage));
        Assert.Equal(ApplicationStageNames.ActiveStages, repository.ColumnRequests.Select(request => request.Stage));
        Assert.All(repository.ColumnRequests, request => Assert.Same(query, request.Query));
    }

    [Fact]
    public async Task GetPipelineAsync_WithStage_ReturnsOnlyThatColumn()
    {
        var repository = new FakeRepository(application: null);
        var service = CreateService(repository);

        var pipeline = await service.GetPipelineAsync(
            Query(stage: ApplicationStage.Rejected),
            Actor(ApplicationPermissions.Read),
            CancellationToken.None);

        var column = Assert.Single(pipeline.Columns);
        Assert.Equal(ApplicationStage.Rejected, column.Stage);
        Assert.Equal([ApplicationStage.Rejected], repository.ColumnRequests.Select(request => request.Stage));
    }

    [Fact]
    public async Task AdvanceAsync_ValidNextStage_PersistsNewVersionWithTrimmedReason()
    {
        var repository = new FakeRepository(ApplicationStage.SourcedApplied, version: 4);
        var service = CreateService(repository);

        var result = await service.AdvanceAsync(
            Command(ApplicationStage.AiScreening, expectedVersion: 4, reason: "  strong CV  "),
            CancellationToken.None);

        Assert.Equal(ApplicationStage.AiScreening, result.Stage);
        Assert.Equal(5, result.Version);
        Assert.Equal(Now, result.UpdatedAt);
        Assert.Equal(1, repository.SaveAdvanceCalls);
        Assert.Equal(ApplicationStage.SourcedApplied, repository.LastPreviousStage);
        Assert.Equal(4, repository.LastExpectedVersion);
        Assert.Equal("strong CV", repository.LastReason);
    }

    [Fact]
    public async Task AdvanceAsync_WithoutAdvancePermission_ThrowsForbiddenWithoutLoading()
    {
        var repository = new FakeRepository(ApplicationStage.SourcedApplied, version: 1);
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() => service.AdvanceAsync(
            new AdvanceApplicationCommand(42, ApplicationStage.AiScreening, 1, null, Actor(ApplicationPermissions.Read)),
            CancellationToken.None));

        Assert.Equal(RecruitmentPipelineService.AdvanceForbiddenCode, exception.Code);
        Assert.Equal(0, repository.GetCalls);
        Assert.Equal(0, repository.SaveAdvanceCalls);
    }

    [Fact]
    public async Task AdvanceAsync_SkippedStage_ThrowsWithoutSaving()
    {
        var repository = new FakeRepository(ApplicationStage.SourcedApplied, version: 1);
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() => service.AdvanceAsync(
            Command(ApplicationStage.TechInterview, expectedVersion: 1),
            CancellationToken.None));

        Assert.Equal(RecruitmentApplication.InvalidStageTransitionCode, exception.Code);
        Assert.Equal(0, repository.SaveAdvanceCalls);
    }

    [Fact]
    public async Task AdvanceAsync_TechInterviewWithoutSchedule_ThrowsWithoutSaving()
    {
        var repository = new FakeRepository(ApplicationStage.AiScreening, version: 2)
        {
            Eligibility = new AdvanceEligibility(false, true)
        };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() => service.AdvanceAsync(
            Command(ApplicationStage.TechInterview, expectedVersion: 2),
            CancellationToken.None));

        Assert.Equal(RecruitmentApplication.InterviewRequiredCode, exception.Code);
        Assert.Equal(0, repository.SaveAdvanceCalls);
    }

    [Fact]
    public async Task AdvanceAsync_OfferWithoutEligibleEvaluation_ThrowsWithoutSaving()
    {
        var repository = new FakeRepository(ApplicationStage.ExecutiveRound, version: 3)
        {
            Eligibility = new AdvanceEligibility(true, false)
        };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() => service.AdvanceAsync(
            Command(ApplicationStage.OfferLetter, expectedVersion: 3),
            CancellationToken.None));

        Assert.Equal(RecruitmentApplication.EvaluationRequiredCode, exception.Code);
        Assert.Equal(0, repository.SaveAdvanceCalls);
    }

    [Theory]
    [InlineData(ApplicationStage.AiScreening, ApplicationStage.SourcedApplied)]
    [InlineData(ApplicationStage.HiredReady, ApplicationStage.HiredReady)]
    [InlineData(ApplicationStage.Rejected, ApplicationStage.HiredReady)]
    public async Task AdvanceAsync_BackwardSameOrTerminalTransition_Throws(ApplicationStage current, ApplicationStage target)
    {
        var repository = new FakeRepository(current, version: 1);
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() => service.AdvanceAsync(
            Command(target, expectedVersion: 1),
            CancellationToken.None));

        Assert.Equal(RecruitmentApplication.InvalidStageTransitionCode, exception.Code);
        Assert.Equal(0, repository.SaveAdvanceCalls);
    }

    [Fact]
    public async Task AdvanceAsync_StaleVersion_ThrowsBeforeEligibilityAndSave()
    {
        var repository = new FakeRepository(ApplicationStage.AiScreening, version: 3);
        var service = CreateService(repository);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => service.AdvanceAsync(
            Command(ApplicationStage.TechInterview, expectedVersion: 2),
            CancellationToken.None));

        Assert.Equal(0, repository.EligibilityCalls);
        Assert.Equal(0, repository.SaveAdvanceCalls);
    }

    [Fact]
    public async Task AdvanceAsync_ConcurrentWriteDuringSave_ThrowsConflict()
    {
        var repository = new FakeRepository(ApplicationStage.SourcedApplied, version: 1)
        {
            SaveSucceeds = false
        };
        var service = CreateService(repository);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => service.AdvanceAsync(
            Command(ApplicationStage.AiScreening, expectedVersion: 1),
            CancellationToken.None));

        Assert.Equal(1, repository.SaveAdvanceCalls);
    }

    [Fact]
    public async Task AdvanceAsync_MissingOrOutOfScope_ThrowsNotFound()
    {
        var repository = new FakeRepository(application: null);
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrNotFoundException>(() => service.AdvanceAsync(
            Command(ApplicationStage.AiScreening, expectedVersion: 1),
            CancellationToken.None));

        Assert.Equal("Application", exception.Resource);
        Assert.Equal(0, repository.SaveAdvanceCalls);
    }

    [Theory]
    [InlineData(ApplicationTerminalAction.Reject, ApplicationStage.Rejected)]
    [InlineData(ApplicationTerminalAction.Withdraw, ApplicationStage.Withdrawn)]
    public async Task TerminateAsync_FromActiveStage_SavesTerminationWithTrimmedReason(ApplicationTerminalAction action, ApplicationStage expected)
    {
        var repository = new FakeRepository(ApplicationStage.TechInterview, version: 5);
        var service = CreateService(repository);

        var result = await service.TerminateAsync(
            Terminate(action, expectedVersion: 5, reason: "  Failed technical  "),
            CancellationToken.None);

        Assert.Equal(expected, result.Stage);
        Assert.Equal(6, result.Version);
        Assert.Equal(1, repository.SaveTerminationCalls);
        Assert.Equal(ApplicationStage.TechInterview, repository.LastPreviousStage);
        Assert.Equal(5, repository.LastExpectedVersion);
        Assert.Equal("Failed technical", repository.LastReason);
        Assert.Equal(0, repository.SaveAdvanceCalls);
    }

    [Fact]
    public async Task TerminateAsync_WithoutTerminatePermission_ThrowsForbiddenWithoutLoading()
    {
        var repository = new FakeRepository(ApplicationStage.TechInterview, version: 5);
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() => service.TerminateAsync(
            new TerminateApplicationCommand(42, ApplicationTerminalAction.Reject, 5, "reason", Actor(ApplicationPermissions.Advance)),
            CancellationToken.None));

        Assert.Equal(RecruitmentPipelineService.TerminateForbiddenCode, exception.Code);
        Assert.Equal(0, repository.GetCalls);
        Assert.Equal(0, repository.SaveTerminationCalls);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public async Task TerminateAsync_BlankReason_ThrowsValidationWithoutSaving(string? reason)
    {
        var repository = new FakeRepository(ApplicationStage.TechInterview, version: 5);
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() => service.TerminateAsync(
            Terminate(ApplicationTerminalAction.Reject, expectedVersion: 5, reason),
            CancellationToken.None));

        Assert.Equal(["reason"], exception.Errors.Keys);
        Assert.Equal(0, repository.SaveTerminationCalls);
    }

    [Theory]
    [InlineData(ApplicationStage.Rejected)]
    [InlineData(ApplicationStage.Withdrawn)]
    public async Task TerminateAsync_FromTerminalStage_ThrowsInvalidStageTransition(ApplicationStage current)
    {
        var repository = new FakeRepository(current, version: 2);
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() => service.TerminateAsync(
            Terminate(ApplicationTerminalAction.Withdraw, expectedVersion: 2, "reason"),
            CancellationToken.None));

        Assert.Equal(RecruitmentApplication.InvalidStageTransitionCode, exception.Code);
        Assert.Equal(current.ToContract(), exception.Details["currentStage"]);
        Assert.Equal(0, repository.SaveTerminationCalls);
    }

    [Fact]
    public async Task TerminateAsync_StaleVersion_ThrowsConcurrencyWithoutSaving()
    {
        var repository = new FakeRepository(ApplicationStage.TechInterview, version: 5);
        var service = CreateService(repository);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => service.TerminateAsync(
            Terminate(ApplicationTerminalAction.Reject, expectedVersion: 4, "reason"),
            CancellationToken.None));

        Assert.Equal(0, repository.SaveTerminationCalls);
    }

    [Fact]
    public async Task TerminateAsync_SaveLosesRace_ThrowsConcurrency()
    {
        var repository = new FakeRepository(ApplicationStage.TechInterview, version: 5) { SaveSucceeds = false };
        var service = CreateService(repository);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => service.TerminateAsync(
            Terminate(ApplicationTerminalAction.Reject, expectedVersion: 5, "reason"),
            CancellationToken.None));

        Assert.Equal(1, repository.SaveTerminationCalls);
    }

    [Fact]
    public async Task TerminateAsync_MissingOrOutOfScope_ThrowsNotFound()
    {
        var repository = new FakeRepository(application: null);
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrNotFoundException>(() => service.TerminateAsync(
            Terminate(ApplicationTerminalAction.Reject, expectedVersion: 1, "reason"),
            CancellationToken.None));

        Assert.Equal("Application", exception.Resource);
    }

    private static RecruitmentPipelineService CreateService(FakeRepository repository) =>
        new(repository, new FixedTimeProvider(Now));

    private static RecruitmentPipelineQuery Query(ApplicationStage? stage = null) =>
        new(RequisitionId, null, stage, null, PageRequest.Default);

    private static AdvanceApplicationCommand Command(ApplicationStage target, long expectedVersion, string? reason = null) => new(
        ApplicationId: 42,
        TargetStage: target,
        ExpectedVersion: expectedVersion,
        Reason: reason,
        Actor: Actor(ApplicationPermissions.Advance));

    private static TerminateApplicationCommand Terminate(ApplicationTerminalAction action, long expectedVersion, string? reason) => new(
        ApplicationId: 42,
        Action: action,
        ExpectedVersion: expectedVersion,
        Reason: reason,
        Actor: Actor(ApplicationPermissions.Terminate));

    private static CoreHrActor Actor(params string[] permissions) => new(
        UserId: 7,
        EmployeeId: null,
        DataScope: CoreHrDataScope.Organization,
        Permissions: permissions.ToHashSet(StringComparer.Ordinal),
        CorrelationId: "test-correlation");

    private sealed class FakeRepository : IRecruitmentApplicationRepository
    {
        private readonly RecruitmentApplication? _application;

        public FakeRepository(ApplicationStage stage, long version)
            : this(new RecruitmentApplication(42, 10, RequisitionId, 30, stage, 80m, "direct", Now.AddDays(-1), version, Now.AddMinutes(-5)))
        {
        }

        public FakeRepository(RecruitmentApplication? application)
        {
            _application = application;
        }

        public AdvanceEligibility Eligibility { get; init; } = new(true, true);
        public bool SaveSucceeds { get; init; } = true;
        public bool RequisitionVisible { get; init; } = true;

        public int GetCalls { get; private set; }
        public int EligibilityCalls { get; private set; }
        public int SaveAdvanceCalls { get; private set; }
        public int SaveTerminationCalls { get; private set; }
        public ApplicationStage? LastPreviousStage { get; private set; }
        public long? LastExpectedVersion { get; private set; }
        public string? LastReason { get; private set; }
        public List<(RecruitmentPipelineQuery Query, ApplicationStage Stage)> ColumnRequests { get; } = [];

        public Task<RecruitmentApplication?> GetByIdAsync(long applicationId, CoreHrActor actor, CancellationToken cancellationToken)
        {
            GetCalls++;
            return Task.FromResult(_application);
        }

        public Task<bool> IsRequisitionVisibleAsync(long requisitionId, CoreHrActor actor, CancellationToken cancellationToken) =>
            Task.FromResult(RequisitionVisible);

        public Task<PipelineColumn> GetPipelineColumnAsync(RecruitmentPipelineQuery query, ApplicationStage stage, CancellationToken cancellationToken)
        {
            ColumnRequests.Add((query, stage));
            var card = new PipelineCard(
                new RecruitmentApplication(1, 10, RequisitionId, null, stage, 50m, "direct", Now, 1, Now),
                new CandidateSummary(10, "An", "Nguyen", "an@example.com", null, null, null));
            return Task.FromResult(new PipelineColumn(stage, 1, 50m, [card]));
        }

        public Task<AdvanceEligibility> GetAdvanceEligibilityAsync(long applicationId, ApplicationStage targetStage, CancellationToken cancellationToken)
        {
            EligibilityCalls++;
            return Task.FromResult(Eligibility);
        }

        public Task<bool> SaveAdvanceAsync(RecruitmentApplication application, ApplicationStage previousStage, long expectedVersion, string? reason, CoreHrActor actor, CancellationToken cancellationToken)
        {
            SaveAdvanceCalls++;
            LastPreviousStage = previousStage;
            LastExpectedVersion = expectedVersion;
            LastReason = reason;
            return Task.FromResult(SaveSucceeds);
        }

        public Task<bool> SaveTerminationAsync(RecruitmentApplication application, ApplicationStage previousStage, long expectedVersion, string reason, CoreHrActor actor, CancellationToken cancellationToken)
        {
            SaveTerminationCalls++;
            LastPreviousStage = previousStage;
            LastExpectedVersion = expectedVersion;
            LastReason = reason;
            return Task.FromResult(SaveSucceeds);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
