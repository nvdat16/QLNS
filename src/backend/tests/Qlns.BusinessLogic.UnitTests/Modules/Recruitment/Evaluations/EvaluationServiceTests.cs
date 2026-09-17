using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Evaluations;
using Qlns.BusinessLogic.Modules.Recruitment.Interviews;
using Xunit;

namespace Qlns.BusinessLogic.UnitTests.Modules.Recruitment.Evaluations;

public sealed class EvaluationServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 7, 0, 0, TimeSpan.Zero);
    private static readonly EvaluationInterview CompletedInterview = new(5, InterviewStatus.Completed, [7, 8]);

    [Fact]
    public async Task ListAsync_InterviewNotVisible_ThrowsNotFound()
    {
        var service = CreateService(new FakeEvaluationRepository { Interview = null });

        var exception = await Assert.ThrowsAsync<CoreHrNotFoundException>(() => service.ListAsync(5, Actor(7), CancellationToken.None));

        Assert.Equal("Interview", exception.Resource);
        Assert.Equal(5, exception.Id);
    }

    [Fact]
    public async Task ListAsync_PanelistWithoutOwnSubmission_SeesNothing()
    {
        var repository = new FakeEvaluationRepository
        {
            Interview = CompletedInterview,
            Evaluations = [EvaluationTests.CreateEvaluation(id: 2, version: 1, evaluatorUserId: 8)]
        };

        var visible = await CreateService(repository).ListAsync(5, Actor(7, EvaluationPermissions.Read), CancellationToken.None);

        Assert.Empty(visible);
    }

    [Fact]
    public async Task ListAsync_ReadAll_SeesEverything()
    {
        var repository = new FakeEvaluationRepository
        {
            Interview = CompletedInterview,
            Evaluations =
            [
                EvaluationTests.CreateEvaluation(id: 2, version: 1, evaluatorUserId: 8),
                EvaluationTests.CreateEvaluation(id: 3, version: 2, evaluatorUserId: 8)
            ]
        };

        var visible = await CreateService(repository).ListAsync(5, Actor(99, EvaluationPermissions.ReadAll), CancellationToken.None);

        Assert.Equal(2, visible.Count);
    }

    [Fact]
    public async Task SubmitAsync_FirstSubmission_InsertsVersionOne()
    {
        var repository = new FakeEvaluationRepository { Interview = CompletedInterview };
        var service = CreateService(repository);

        var created = await service.SubmitAsync(new SubmitEvaluationCommand(5, EvaluationTests.ValidWrite(), Actor(7)), CancellationToken.None);

        Assert.Equal(500, created.Id);
        Assert.Equal(1, created.Version);
        Assert.Equal(7, created.EvaluatorUserId);
        Assert.Equal(4.3m, created.OverallScore);
        Assert.Equal(Now, created.SubmittedAt);
        Assert.Equal(1, repository.InsertSubmissionCalls);
        Assert.Equal(0, repository.LastInserted?.Id);
    }

    [Fact]
    public async Task SubmitAsync_InterviewNotVisible_ThrowsNotFound()
    {
        var repository = new FakeEvaluationRepository { Interview = null };

        var exception = await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            CreateService(repository).SubmitAsync(new SubmitEvaluationCommand(5, EvaluationTests.ValidWrite(), Actor(7)), CancellationToken.None));

        Assert.Equal("Interview", exception.Resource);
        Assert.Equal(0, repository.InsertSubmissionCalls);
    }

    [Fact]
    public async Task SubmitAsync_NotPanelist_ThrowsForbidden()
    {
        var repository = new FakeEvaluationRepository { Interview = CompletedInterview };

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() =>
            CreateService(repository).SubmitAsync(new SubmitEvaluationCommand(5, EvaluationTests.ValidWrite(), Actor(99)), CancellationToken.None));

        Assert.Equal(EvaluationService.NotPanelistCode, exception.Code);
        Assert.Equal(0, repository.InsertSubmissionCalls);
    }

    [Theory]
    [InlineData(InterviewStatus.Scheduled)]
    [InlineData(InterviewStatus.Cancelled)]
    [InlineData(InterviewStatus.NoShow)]
    public async Task SubmitAsync_InterviewNotCompleted_ThrowsBusinessRule(InterviewStatus status)
    {
        var repository = new FakeEvaluationRepository { Interview = CompletedInterview with { Status = status } };

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            CreateService(repository).SubmitAsync(new SubmitEvaluationCommand(5, EvaluationTests.ValidWrite(), Actor(7)), CancellationToken.None));

        Assert.Equal(EvaluationService.InterviewNotCompletedCode, exception.Code);
        Assert.Equal(0, repository.InsertSubmissionCalls);
    }

    [Fact]
    public async Task SubmitAsync_LatestVersionLocked_ThrowsAlreadySubmittedWithoutInserting()
    {
        var repository = new FakeEvaluationRepository
        {
            Interview = CompletedInterview,
            Evaluations = [EvaluationTests.CreateEvaluation(id: 1, version: 1, evaluatorUserId: 7)]
        };

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            CreateService(repository).SubmitAsync(new SubmitEvaluationCommand(5, EvaluationTests.ValidWrite(), Actor(7)), CancellationToken.None));

        Assert.Equal(EvaluationService.AlreadySubmittedCode, exception.Code);
        Assert.Equal(1L, exception.Details["evaluationId"]);
        Assert.Equal(1, exception.Details["version"]);
        Assert.Equal(0, repository.InsertSubmissionCalls);
    }

    [Fact]
    public async Task SubmitAsync_AfterUnlock_InsertsNextVersionWithFreshScores()
    {
        var repository = new FakeEvaluationRepository
        {
            Interview = CompletedInterview,
            Evaluations =
            [
                EvaluationTests.CreateEvaluation(id: 1, version: 1, evaluatorUserId: 7),
                EvaluationTests.CreateEvaluation(id: 2, version: 2, evaluatorUserId: 7, unlockedAt: Now.AddHours(-1))
            ]
        };
        var write = new EvaluationWrite(2m, 2m, 2m, 2m, "no_hire", "Reassessed");

        var created = await CreateService(repository).SubmitAsync(new SubmitEvaluationCommand(5, write, Actor(7)), CancellationToken.None);

        Assert.Equal(3, created.Version);
        Assert.Equal(2m, created.OverallScore);
        Assert.Equal(Recommendation.NoHire, created.Recommendation);
        Assert.False(created.IsUnlocked);
        Assert.Equal(1, repository.InsertSubmissionCalls);
    }

    [Fact]
    public async Task SubmitAsync_OtherPanelistSubmission_DoesNotBlock()
    {
        var repository = new FakeEvaluationRepository
        {
            Interview = CompletedInterview,
            Evaluations = [EvaluationTests.CreateEvaluation(id: 1, version: 1, evaluatorUserId: 8)]
        };

        var created = await CreateService(repository).SubmitAsync(new SubmitEvaluationCommand(5, EvaluationTests.ValidWrite(), Actor(7)), CancellationToken.None);

        Assert.Equal(1, created.Version);
        Assert.Equal(7, created.EvaluatorUserId);
    }

    [Fact]
    public async Task SubmitAsync_InvalidScores_ThrowsValidationWithoutInserting()
    {
        var repository = new FakeEvaluationRepository { Interview = CompletedInterview };
        var write = EvaluationTests.ValidWrite() with { TeamworkScore = 4.2m };

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() =>
            CreateService(repository).SubmitAsync(new SubmitEvaluationCommand(5, write, Actor(7)), CancellationToken.None));

        Assert.True(exception.Errors.ContainsKey("teamworkScore"));
        Assert.Equal(0, repository.InsertSubmissionCalls);
    }

    [Fact]
    public async Task SubmitAsync_InsertLosesRace_ThrowsAlreadySubmitted()
    {
        var repository = new FakeEvaluationRepository { Interview = CompletedInterview, InsertSucceeds = false };

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            CreateService(repository).SubmitAsync(new SubmitEvaluationCommand(5, EvaluationTests.ValidWrite(), Actor(7)), CancellationToken.None));

        Assert.Equal(EvaluationService.AlreadySubmittedCode, exception.Code);
        Assert.Equal(1, repository.InsertSubmissionCalls);
    }

    [Fact]
    public async Task UnlockAsync_LatestLockedVersion_InsertsUnlockVersion()
    {
        var repository = new FakeEvaluationRepository
        {
            Interview = CompletedInterview,
            Evaluations = [EvaluationTests.CreateEvaluation(id: 1, version: 1, evaluatorUserId: 7)]
        };

        var unlocked = await CreateService(repository).UnlockAsync(
            new UnlockEvaluationCommand(1, 1, "Wrong candidate", Actor(1, EvaluationPermissions.Unlock)),
            CancellationToken.None);

        Assert.Equal(501, unlocked.Id);
        Assert.Equal(2, unlocked.Version);
        Assert.True(unlocked.IsUnlocked);
        Assert.Equal(1, unlocked.UnlockedBy);
        Assert.Equal("Wrong candidate", unlocked.UnlockReason);
        Assert.Equal(Now, unlocked.UnlockedAt);
        Assert.Equal(1, repository.InsertUnlockCalls);
        Assert.Equal(1, repository.LastPreviousVersion);
    }

    [Fact]
    public async Task UnlockAsync_EvaluationNotVisible_ThrowsNotFound()
    {
        var repository = new FakeEvaluationRepository { Interview = CompletedInterview };

        var exception = await Assert.ThrowsAsync<CoreHrNotFoundException>(() => CreateService(repository).UnlockAsync(
            new UnlockEvaluationCommand(404, 1, "reason", Actor(1, EvaluationPermissions.Unlock)),
            CancellationToken.None));

        Assert.Equal("Evaluation", exception.Resource);
        Assert.Equal(404, exception.Id);
    }

    [Fact]
    public async Task UnlockAsync_WithoutPermission_ThrowsForbiddenWithoutInserting()
    {
        var repository = new FakeEvaluationRepository
        {
            Interview = CompletedInterview,
            Evaluations = [EvaluationTests.CreateEvaluation(id: 1, version: 1, evaluatorUserId: 7)]
        };

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() => CreateService(repository).UnlockAsync(
            new UnlockEvaluationCommand(1, 1, "reason", Actor(1, EvaluationPermissions.ReadAll)),
            CancellationToken.None));

        Assert.Equal(EvaluationService.UnlockForbiddenCode, exception.Code);
        Assert.Equal(0, repository.InsertUnlockCalls);
    }

    [Fact]
    public async Task UnlockAsync_StaleVersion_ThrowsConcurrencyWithoutInserting()
    {
        var repository = new FakeEvaluationRepository
        {
            Interview = CompletedInterview,
            Evaluations = [EvaluationTests.CreateEvaluation(id: 1, version: 2, evaluatorUserId: 7)]
        };

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => CreateService(repository).UnlockAsync(
            new UnlockEvaluationCommand(1, 1, "reason", Actor(1, EvaluationPermissions.Unlock)),
            CancellationToken.None));

        Assert.Equal(0, repository.InsertUnlockCalls);
    }

    [Fact]
    public async Task UnlockAsync_NotLatestVersion_ThrowsBusinessRule()
    {
        var repository = new FakeEvaluationRepository
        {
            Interview = CompletedInterview,
            Evaluations =
            [
                EvaluationTests.CreateEvaluation(id: 1, version: 1, evaluatorUserId: 7),
                EvaluationTests.CreateEvaluation(id: 2, version: 2, evaluatorUserId: 7)
            ]
        };

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() => CreateService(repository).UnlockAsync(
            new UnlockEvaluationCommand(1, 1, "reason", Actor(1, EvaluationPermissions.Unlock)),
            CancellationToken.None));

        Assert.Equal(EvaluationService.NotLatestVersionCode, exception.Code);
        Assert.Equal(2L, exception.Details["latestEvaluationId"]);
        Assert.Equal(0, repository.InsertUnlockCalls);
    }

    [Fact]
    public async Task UnlockAsync_AlreadyUnlocked_ThrowsBusinessRule()
    {
        var repository = new FakeEvaluationRepository
        {
            Interview = CompletedInterview,
            Evaluations = [EvaluationTests.CreateEvaluation(id: 2, version: 2, evaluatorUserId: 7, unlockedAt: Now.AddHours(-1))]
        };

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() => CreateService(repository).UnlockAsync(
            new UnlockEvaluationCommand(2, 2, "reason", Actor(1, EvaluationPermissions.Unlock)),
            CancellationToken.None));

        Assert.Equal(EvaluationService.AlreadyUnlockedCode, exception.Code);
        Assert.Equal(0, repository.InsertUnlockCalls);
    }

    [Fact]
    public async Task UnlockAsync_WithoutReason_ThrowsValidation()
    {
        var repository = new FakeEvaluationRepository
        {
            Interview = CompletedInterview,
            Evaluations = [EvaluationTests.CreateEvaluation(id: 1, version: 1, evaluatorUserId: 7)]
        };

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() => CreateService(repository).UnlockAsync(
            new UnlockEvaluationCommand(1, 1, "  ", Actor(1, EvaluationPermissions.Unlock)),
            CancellationToken.None));

        Assert.True(exception.Errors.ContainsKey("reason"));
        Assert.Equal(0, repository.InsertUnlockCalls);
    }

    [Fact]
    public async Task UnlockAsync_InsertLosesRace_ThrowsConcurrency()
    {
        var repository = new FakeEvaluationRepository
        {
            Interview = CompletedInterview,
            Evaluations = [EvaluationTests.CreateEvaluation(id: 1, version: 1, evaluatorUserId: 7)],
            InsertSucceeds = false
        };

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => CreateService(repository).UnlockAsync(
            new UnlockEvaluationCommand(1, 1, "reason", Actor(1, EvaluationPermissions.Unlock)),
            CancellationToken.None));

        Assert.Equal(1, repository.InsertUnlockCalls);
    }

    private static EvaluationService CreateService(FakeEvaluationRepository repository) =>
        new(repository, new EqualWeightScoringPolicy(), new FixedTimeProvider(Now));

    private static CoreHrActor Actor(long userId, params string[] permissions) => new(
        userId,
        EmployeeId: null,
        CoreHrDataScope.Self,
        permissions.Length == 0
            ? new HashSet<string> { EvaluationPermissions.Read, EvaluationPermissions.Submit }
            : permissions.ToHashSet(StringComparer.Ordinal),
        "test-correlation");

    private sealed class FakeEvaluationRepository : IEvaluationRepository
    {
        public EvaluationInterview? Interview { get; init; }
        public List<Evaluation> Evaluations { get; init; } = [];
        public bool InsertSucceeds { get; init; } = true;

        public int InsertSubmissionCalls { get; private set; }
        public int InsertUnlockCalls { get; private set; }
        public Evaluation? LastInserted { get; private set; }
        public int? LastPreviousVersion { get; private set; }

        public Task<EvaluationInterview?> GetInterviewAsync(long interviewId, CoreHrActor actor, CancellationToken cancellationToken) =>
            Task.FromResult(Interview?.Id == interviewId ? Interview : null);

        public Task<IReadOnlyList<Evaluation>> ListByInterviewAsync(long interviewId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Evaluation>>(Evaluations.Where(e => e.InterviewId == interviewId).ToList());

        public Task<Evaluation?> GetByIdAsync(long evaluationId, CoreHrActor actor, CancellationToken cancellationToken) =>
            Task.FromResult(Evaluations.FirstOrDefault(e => e.Id == evaluationId));

        public Task<Evaluation?> GetLatestAsync(long interviewId, long evaluatorUserId, CancellationToken cancellationToken) =>
            Task.FromResult(Evaluations
                .Where(e => e.InterviewId == interviewId && e.EvaluatorUserId == evaluatorUserId)
                .MaxBy(e => e.Version));

        public Task<Evaluation?> InsertSubmissionAsync(Evaluation evaluation, CoreHrActor actor, CancellationToken cancellationToken)
        {
            InsertSubmissionCalls++;
            LastInserted = evaluation;
            return Task.FromResult(InsertSucceeds ? WithId(evaluation, 500) : null);
        }

        public Task<Evaluation?> InsertUnlockedVersionAsync(Evaluation evaluation, int previousVersion, CoreHrActor actor, CancellationToken cancellationToken)
        {
            InsertUnlockCalls++;
            LastInserted = evaluation;
            LastPreviousVersion = previousVersion;
            return Task.FromResult(InsertSucceeds ? WithId(evaluation, 501) : null);
        }

        private static Evaluation WithId(Evaluation evaluation, long id) => new(
            id,
            evaluation.InterviewId,
            evaluation.EvaluatorUserId,
            evaluation.Scores,
            evaluation.OverallScore,
            evaluation.Recommendation,
            evaluation.Feedback,
            evaluation.SubmittedAt,
            evaluation.UnlockedAt,
            evaluation.UnlockedBy,
            evaluation.UnlockReason,
            evaluation.Version);
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
