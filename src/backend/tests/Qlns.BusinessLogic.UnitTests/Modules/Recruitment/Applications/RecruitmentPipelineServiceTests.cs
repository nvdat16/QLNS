using Qlns.BusinessLogic.Modules.Recruitment.Applications;
using Xunit;

namespace Qlns.BusinessLogic.UnitTests;

public sealed class RecruitmentPipelineServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 7, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task AdvanceAsync_ValidNextStage_PersistsNewVersion()
    {
        var repository = new FakeRepository(ApplicationStage.SourcedApplied, version: 4);
        var service = new RecruitmentPipelineService(repository, new FixedTimeProvider(Now));

        var result = await service.AdvanceAsync(
            Command(ApplicationStage.AiScreening, expectedVersion: 4),
            CancellationToken.None);

        Assert.Equal(ApplicationStage.AiScreening, result.Stage);
        Assert.Equal(5, result.Version);
        Assert.Equal(Now, result.UpdatedAt);
        Assert.Equal(1, repository.SaveCalls);
    }

    [Fact]
    public async Task AdvanceAsync_SkippedStage_ThrowsWithoutSaving()
    {
        var repository = new FakeRepository(ApplicationStage.SourcedApplied, version: 1);
        var service = new RecruitmentPipelineService(repository, new FixedTimeProvider(Now));

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => service.AdvanceAsync(
            Command(ApplicationStage.TechInterview, expectedVersion: 1),
            CancellationToken.None));

        Assert.Equal("recruitment.invalid_stage_transition", exception.Code);
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task AdvanceAsync_TechInterviewWithoutSchedule_ThrowsWithoutSaving()
    {
        var repository = new FakeRepository(ApplicationStage.AiScreening, version: 2)
        {
            Eligibility = new AdvanceEligibility(false, true)
        };
        var service = new RecruitmentPipelineService(repository, new FixedTimeProvider(Now));

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => service.AdvanceAsync(
            Command(ApplicationStage.TechInterview, expectedVersion: 2),
            CancellationToken.None));

        Assert.Equal("recruitment.interview_required", exception.Code);
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task AdvanceAsync_OfferWithoutEligibleEvaluation_ThrowsWithoutSaving()
    {
        var repository = new FakeRepository(ApplicationStage.ExecutiveRound, version: 3)
        {
            Eligibility = new AdvanceEligibility(true, false)
        };
        var service = new RecruitmentPipelineService(repository, new FixedTimeProvider(Now));

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => service.AdvanceAsync(
            Command(ApplicationStage.OfferLetter, expectedVersion: 3),
            CancellationToken.None));

        Assert.Equal("recruitment.evaluation_required", exception.Code);
        Assert.Equal(0, repository.SaveCalls);
    }

    [Theory]
    [InlineData(ApplicationStage.AiScreening, ApplicationStage.SourcedApplied)]
    [InlineData(ApplicationStage.HiredReady, ApplicationStage.HiredReady)]
    [InlineData(ApplicationStage.Rejected, ApplicationStage.HiredReady)]
    public async Task AdvanceAsync_BackwardSameOrTerminalTransition_Throws(
        ApplicationStage current,
        ApplicationStage target)
    {
        var repository = new FakeRepository(current, version: 1);
        var service = new RecruitmentPipelineService(repository, new FixedTimeProvider(Now));

        var exception = await Assert.ThrowsAsync<BusinessRuleException>(() => service.AdvanceAsync(
            Command(target, expectedVersion: 1),
            CancellationToken.None));

        Assert.Equal("recruitment.invalid_stage_transition", exception.Code);
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task AdvanceAsync_StaleVersion_ThrowsBeforeEligibilityAndSave()
    {
        var repository = new FakeRepository(ApplicationStage.AiScreening, version: 3);
        var service = new RecruitmentPipelineService(repository, new FixedTimeProvider(Now));

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => service.AdvanceAsync(
            Command(ApplicationStage.TechInterview, expectedVersion: 2),
            CancellationToken.None));

        Assert.Equal(0, repository.EligibilityCalls);
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task AdvanceAsync_ConcurrentWriteDuringSave_ThrowsConflict()
    {
        var repository = new FakeRepository(ApplicationStage.SourcedApplied, version: 1)
        {
            SaveSucceeds = false
        };
        var service = new RecruitmentPipelineService(repository, new FixedTimeProvider(Now));

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() => service.AdvanceAsync(
            Command(ApplicationStage.AiScreening, expectedVersion: 1),
            CancellationToken.None));

        Assert.Equal(1, repository.SaveCalls);
    }

    private static AdvanceApplicationCommand Command(ApplicationStage target, long expectedVersion) => new(
        ApplicationId: 42,
        TargetStage: target,
        ExpectedVersion: expectedVersion,
        ActorUserId: 7,
        DataScope: RecruitmentDataScope.Organization,
        CorrelationId: "test-correlation",
        Reason: null);

    private sealed class FakeRepository(ApplicationStage stage, long version) : IRecruitmentApplicationRepository
    {
        private readonly RecruitmentApplication _application = new(42, 10, 20, stage, version, Now.AddMinutes(-5));

        public AdvanceEligibility Eligibility { get; init; } = new(true, true);
        public bool SaveSucceeds { get; init; } = true;
        public int EligibilityCalls { get; private set; }
        public int SaveCalls { get; private set; }

        public Task<RecruitmentApplication?> GetByIdAsync(
            long applicationId,
            RecruitmentDataScope dataScope,
            CancellationToken cancellationToken) =>
            Task.FromResult<RecruitmentApplication?>(_application);

        public Task<AdvanceEligibility> GetAdvanceEligibilityAsync(
            long applicationId,
            ApplicationStage targetStage,
            CancellationToken cancellationToken)
        {
            EligibilityCalls++;
            return Task.FromResult(Eligibility);
        }

        public Task<bool> SaveAdvanceAsync(
            RecruitmentApplication application,
            ApplicationStage previousStage,
            long expectedVersion,
            long actorUserId,
            string correlationId,
            string? reason,
            CancellationToken cancellationToken)
        {
            SaveCalls++;
            return Task.FromResult(SaveSucceeds);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
