using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Evaluations;
using Xunit;

namespace Qlns.BusinessLogic.UnitTests.Modules.Recruitment.Evaluations;

public sealed class EvaluationTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 7, 0, 0, TimeSpan.Zero);
    private static readonly IEvaluationScoringPolicy Policy = new EqualWeightScoringPolicy();

    [Fact]
    public void Submit_Valid_BuildsUnsavedVersionWithComputedOverallAndTrimmedFeedback()
    {
        var evaluation = Evaluation.Submit(5, 7, ValidWrite() with { Feedback = "  Strong fundamentals  " }, 1, Policy, Now);

        Assert.Equal(0, evaluation.Id);
        Assert.Equal(5, evaluation.InterviewId);
        Assert.Equal(7, evaluation.EvaluatorUserId);
        Assert.Equal(1, evaluation.Version);
        Assert.Equal(new EvaluationScores(4m, 3.5m, 4.5m, 5m), evaluation.Scores);
        Assert.Equal(4.3m, evaluation.OverallScore);
        Assert.Equal(Recommendation.Hire, evaluation.Recommendation);
        Assert.Equal("Strong fundamentals", evaluation.Feedback);
        Assert.Equal(Now, evaluation.SubmittedAt);
        Assert.False(evaluation.IsUnlocked);
        Assert.Null(evaluation.UnlockedBy);
        Assert.Null(evaluation.UnlockReason);
    }

    [Theory]
    [InlineData(4.3)]
    [InlineData(5.5)]
    [InlineData(-0.5)]
    [InlineData(0.25)]
    public void Submit_InvalidTechnicalScore_ThrowsOnTechnicalScore(double score)
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Evaluation.Submit(5, 7, ValidWrite() with { TechnicalScore = (decimal)score }, 1, Policy, Now));

        Assert.True(exception.Errors.ContainsKey("technicalScore"));
        Assert.False(exception.Errors.ContainsKey("communicationScore"));
    }

    [Fact]
    public void Submit_SeveralInvalidScores_ReportsEachField()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Evaluation.Submit(5, 7, new EvaluationWrite(6m, 1.1m, 2.2m, -1m, "hire", "ok"), 1, Policy, Now));

        Assert.Equal(["communicationScore", "problemSolvingScore", "teamworkScore", "technicalScore"], exception.Errors.Keys.Order());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("maybe")]
    [InlineData("Hire")]
    public void Submit_UnknownRecommendation_ThrowsOnRecommendation(string? recommendation)
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Evaluation.Submit(5, 7, ValidWrite() with { Recommendation = recommendation }, 1, Policy, Now));

        Assert.True(exception.Errors.ContainsKey("recommendation"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Submit_BlankFeedback_ThrowsOnFeedback(string? feedback)
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Evaluation.Submit(5, 7, ValidWrite() with { Feedback = feedback }, 1, Policy, Now));

        Assert.True(exception.Errors.ContainsKey("feedback"));
    }

    [Fact]
    public void Submit_TooLongFeedback_ThrowsOnFeedback()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Evaluation.Submit(5, 7, ValidWrite() with { Feedback = new string('f', Evaluation.FeedbackMaxLength + 1) }, 1, Policy, Now));

        Assert.True(exception.Errors.ContainsKey("feedback"));
    }

    [Fact]
    public void Submit_AllRecommendationsRoundTrip()
    {
        foreach (var recommendation in Enum.GetValues<Recommendation>())
        {
            var evaluation = Evaluation.Submit(5, 7, ValidWrite() with { Recommendation = recommendation.ToContract() }, 1, Policy, Now);
            Assert.Equal(recommendation, evaluation.Recommendation);
        }
    }

    [Fact]
    public void CreateUnlockedVersion_CopiesScoresAndSetsUnlockMetadata()
    {
        var original = CreateEvaluation(id: 9, version: 2);
        var unlockedAt = Now.AddHours(1);

        var unlocked = original.CreateUnlockedVersion(unlockedBy: 1, "  Score entered for wrong candidate  ", unlockedAt);

        Assert.Equal(0, unlocked.Id);
        Assert.Equal(3, unlocked.Version);
        Assert.Equal(original.Scores, unlocked.Scores);
        Assert.Equal(original.OverallScore, unlocked.OverallScore);
        Assert.Equal(original.Recommendation, unlocked.Recommendation);
        Assert.Equal(original.Feedback, unlocked.Feedback);
        Assert.Equal(original.SubmittedAt, unlocked.SubmittedAt);
        Assert.True(unlocked.IsUnlocked);
        Assert.Equal(unlockedAt, unlocked.UnlockedAt);
        Assert.Equal(1, unlocked.UnlockedBy);
        Assert.Equal("Score entered for wrong candidate", unlocked.UnlockReason);
        Assert.False(original.IsUnlocked);
        Assert.Equal(2, original.Version);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void CreateUnlockedVersion_WithoutReason_ThrowsValidationOnReason(string? reason)
    {
        var original = CreateEvaluation(id: 9, version: 1);

        var exception = Assert.Throws<CoreHrValidationException>(() => original.CreateUnlockedVersion(1, reason, Now));

        Assert.True(exception.Errors.ContainsKey("reason"));
    }

    [Fact]
    public void CreateUnlockedVersion_TooLongReason_ThrowsValidationOnReason()
    {
        var original = CreateEvaluation(id: 9, version: 1);

        var exception = Assert.Throws<CoreHrValidationException>(() =>
            original.CreateUnlockedVersion(1, new string('r', Evaluation.UnlockReasonMaxLength + 1), Now));

        Assert.True(exception.Errors.ContainsKey("reason"));
    }

    [Fact]
    public void CreateUnlockedVersion_AlreadyUnlocked_ThrowsAlreadyUnlocked()
    {
        var unlocked = CreateEvaluation(id: 9, version: 2, unlockedAt: Now.AddDays(-1));

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => unlocked.CreateUnlockedVersion(1, "again", Now));

        Assert.Equal(EvaluationService.AlreadyUnlockedCode, exception.Code);
    }

    [Fact]
    public void Constructor_InvalidArguments_Throws()
    {
        var scores = new EvaluationScores(4m, 4m, 4m, 4m);

        Assert.Throws<ArgumentOutOfRangeException>(() => new Evaluation(-1, 5, 7, scores, 4m, Recommendation.Hire, "ok", Now, null, null, null, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Evaluation(1, 0, 7, scores, 4m, Recommendation.Hire, "ok", Now, null, null, null, 1));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Evaluation(1, 5, 7, scores, 4m, Recommendation.Hire, "ok", Now, null, null, null, 0));
        Assert.Throws<ArgumentException>(() => new Evaluation(1, 5, 7, scores, 4m, Recommendation.Hire, " ", Now, null, null, null, 1));
    }

    internal static EvaluationWrite ValidWrite() => new(4m, 3.5m, 4.5m, 5m, "hire", "Strong fundamentals");

    internal static Evaluation CreateEvaluation(
        long id,
        int version,
        long interviewId = 5,
        long evaluatorUserId = 7,
        DateTimeOffset? unlockedAt = null) => new(
        id,
        interviewId,
        evaluatorUserId,
        new EvaluationScores(4m, 3.5m, 4.5m, 5m),
        4.3m,
        Recommendation.Hire,
        "Strong fundamentals",
        submittedAt: Now.AddDays(-1),
        unlockedAt,
        unlockedBy: unlockedAt is null ? null : 1,
        unlockReason: unlockedAt is null ? null : "Correction requested",
        version);
}
