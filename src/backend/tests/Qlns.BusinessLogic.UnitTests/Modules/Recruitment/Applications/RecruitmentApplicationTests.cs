using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Applications;
using Xunit;

namespace Qlns.BusinessLogic.UnitTests.Modules.Recruitment.Applications;

public sealed class RecruitmentApplicationTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 17, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ActiveStages_AreTheSixKanbanColumnsInOrder()
    {
        Assert.Equal(
            [
                ApplicationStage.SourcedApplied,
                ApplicationStage.AiScreening,
                ApplicationStage.TechInterview,
                ApplicationStage.ExecutiveRound,
                ApplicationStage.OfferLetter,
                ApplicationStage.HiredReady
            ],
            ApplicationStageNames.ActiveStages);
        Assert.False(ApplicationStage.Rejected.IsActive());
        Assert.False(ApplicationStage.Withdrawn.IsActive());
    }

    [Fact]
    public void Create_StartsInSourcedAppliedWithVersionOneAndNoScore()
    {
        var application = RecruitmentApplication.Create(candidateId: 10, jobPostingId: 20, resumeId: 30, source: "careers", Now);

        Assert.Equal(0, application.Id);
        Assert.Equal(10, application.CandidateId);
        Assert.Equal(20, application.JobPostingId);
        Assert.Equal(30, application.ResumeId);
        Assert.Equal(ApplicationStage.SourcedApplied, application.Stage);
        Assert.Null(application.AiScore);
        Assert.Equal("careers", application.Source);
        Assert.Equal(Now, application.AppliedAt);
        Assert.Equal(Now, application.UpdatedAt);
        Assert.Equal(1, application.Version);
    }

    [Theory]
    [InlineData(null, "direct")]
    [InlineData("", "direct")]
    [InlineData("   ", "direct")]
    [InlineData("  referral ", "referral")]
    public void NormalizeSource_DefaultsBlankAndTrims(string? source, string expected)
    {
        Assert.Equal(expected, RecruitmentApplication.NormalizeSource(source));
    }

    [Fact]
    public void NormalizeSource_TooLong_ThrowsValidationOnSource()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() => RecruitmentApplication.NormalizeSource(new string('s', 81)));

        Assert.Equal(["source"], exception.Errors.Keys);
    }

    [Theory]
    [InlineData(ApplicationStage.SourcedApplied, ApplicationStage.AiScreening)]
    [InlineData(ApplicationStage.AiScreening, ApplicationStage.TechInterview)]
    [InlineData(ApplicationStage.TechInterview, ApplicationStage.ExecutiveRound)]
    [InlineData(ApplicationStage.ExecutiveRound, ApplicationStage.OfferLetter)]
    [InlineData(ApplicationStage.OfferLetter, ApplicationStage.HiredReady)]
    public void AdvanceTo_NextStage_MovesAndReturnsPrevious(ApplicationStage current, ApplicationStage target)
    {
        var application = Create(current, version: 4);

        var previous = application.AdvanceTo(target, new AdvanceEligibility(true, true), Now);

        Assert.Equal(current, previous);
        Assert.Equal(target, application.Stage);
        Assert.Equal(5, application.Version);
        Assert.Equal(Now, application.UpdatedAt);
    }

    [Theory]
    [InlineData(ApplicationStage.SourcedApplied, ApplicationStage.TechInterview)]
    [InlineData(ApplicationStage.AiScreening, ApplicationStage.SourcedApplied)]
    [InlineData(ApplicationStage.HiredReady, ApplicationStage.HiredReady)]
    [InlineData(ApplicationStage.Rejected, ApplicationStage.SourcedApplied)]
    [InlineData(ApplicationStage.Withdrawn, ApplicationStage.AiScreening)]
    [InlineData(ApplicationStage.OfferLetter, ApplicationStage.Rejected)]
    public void AdvanceTo_SkipBackwardSameOrTerminal_ThrowsInvalidStageTransition(ApplicationStage current, ApplicationStage target)
    {
        var application = Create(current, version: 1);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() =>
            application.AdvanceTo(target, new AdvanceEligibility(true, true), Now));

        Assert.Equal(RecruitmentApplication.InvalidStageTransitionCode, exception.Code);
        Assert.Equal(current.ToContract(), exception.Details["currentStage"]);
        Assert.Equal(target.ToContract(), exception.Details["action"]);
        Assert.Equal(current, application.Stage);
        Assert.Equal(1, application.Version);
    }

    [Fact]
    public void AdvanceTo_TechInterviewWithoutScheduledInterview_ThrowsInterviewRequired()
    {
        var application = Create(ApplicationStage.AiScreening, version: 1);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() =>
            application.AdvanceTo(ApplicationStage.TechInterview, new AdvanceEligibility(false, true), Now));

        Assert.Equal(RecruitmentApplication.InterviewRequiredCode, exception.Code);
        Assert.Equal(ApplicationStage.AiScreening, application.Stage);
    }

    [Fact]
    public void AdvanceTo_OfferLetterWithoutEligibleEvaluation_ThrowsEvaluationRequired()
    {
        var application = Create(ApplicationStage.ExecutiveRound, version: 1);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() =>
            application.AdvanceTo(ApplicationStage.OfferLetter, new AdvanceEligibility(true, false), Now));

        Assert.Equal(RecruitmentApplication.EvaluationRequiredCode, exception.Code);
        Assert.Equal(ApplicationStage.ExecutiveRound, application.Stage);
    }

    [Theory]
    [InlineData(ApplicationStage.SourcedApplied)]
    [InlineData(ApplicationStage.AiScreening)]
    [InlineData(ApplicationStage.TechInterview)]
    [InlineData(ApplicationStage.ExecutiveRound)]
    [InlineData(ApplicationStage.OfferLetter)]
    [InlineData(ApplicationStage.HiredReady)]
    public void Reject_FromAnyActiveStage_MovesToRejected(ApplicationStage current)
    {
        var application = Create(current, version: 2);

        var previous = application.Reject("Failed technical", Now);

        Assert.Equal(current, previous);
        Assert.Equal(ApplicationStage.Rejected, application.Stage);
        Assert.Equal(3, application.Version);
        Assert.Equal(Now, application.UpdatedAt);
    }

    [Fact]
    public void Withdraw_FromActiveStage_MovesToWithdrawn()
    {
        var application = Create(ApplicationStage.TechInterview, version: 2);

        var previous = application.Withdraw("Accepted another offer", Now);

        Assert.Equal(ApplicationStage.TechInterview, previous);
        Assert.Equal(ApplicationStage.Withdrawn, application.Stage);
        Assert.Equal(3, application.Version);
    }

    [Theory]
    [InlineData(ApplicationStage.Rejected, ApplicationTerminalAction.Reject)]
    [InlineData(ApplicationStage.Rejected, ApplicationTerminalAction.Withdraw)]
    [InlineData(ApplicationStage.Withdrawn, ApplicationTerminalAction.Reject)]
    [InlineData(ApplicationStage.Withdrawn, ApplicationTerminalAction.Withdraw)]
    public void Terminate_FromTerminalStage_ThrowsInvalidStageTransition(ApplicationStage current, ApplicationTerminalAction action)
    {
        var application = Create(current, version: 1);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => application.Terminate(action, "reason", Now));

        Assert.Equal(RecruitmentApplication.InvalidStageTransitionCode, exception.Code);
        Assert.Equal(current.ToContract(), exception.Details["currentStage"]);
        Assert.Equal(action.ToContract(), exception.Details["action"]);
        Assert.Equal(1, application.Version);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Terminate_WithoutReason_ThrowsValidationOnReason(string? reason)
    {
        var application = Create(ApplicationStage.AiScreening, version: 1);

        var exception = Assert.Throws<CoreHrValidationException>(() => application.Reject(reason, Now));

        Assert.Equal(["reason"], exception.Errors.Keys);
        Assert.Equal(ApplicationStage.AiScreening, application.Stage);
    }

    [Fact]
    public void Terminate_ReasonTooLong_ThrowsValidationOnReason()
    {
        var application = Create(ApplicationStage.AiScreening, version: 1);

        var exception = Assert.Throws<CoreHrValidationException>(() => application.Withdraw(new string('r', 1001), Now));

        Assert.Equal(["reason"], exception.Errors.Keys);
    }

    [Fact]
    public void TerminalAction_MapsToTerminalStageAndContractValue()
    {
        Assert.Equal(ApplicationStage.Rejected, ApplicationTerminalAction.Reject.ToStage());
        Assert.Equal(ApplicationStage.Withdrawn, ApplicationTerminalAction.Withdraw.ToStage());
        Assert.True(ApplicationTerminalActionNames.TryParseContract("reject", out var reject));
        Assert.Equal(ApplicationTerminalAction.Reject, reject);
        Assert.True(ApplicationTerminalActionNames.TryParseContract("withdraw", out var withdraw));
        Assert.Equal(ApplicationTerminalAction.Withdraw, withdraw);
        Assert.False(ApplicationTerminalActionNames.TryParseContract("advance", out _));
    }

    [Fact]
    public void Constructor_InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new RecruitmentApplication(-1, 10, 20, null, ApplicationStage.SourcedApplied, null, "direct", Now, 1, Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RecruitmentApplication(1, 0, 20, null, ApplicationStage.SourcedApplied, null, "direct", Now, 1, Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RecruitmentApplication(1, 10, 0, null, ApplicationStage.SourcedApplied, null, "direct", Now, 1, Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RecruitmentApplication(1, 10, 20, 0, ApplicationStage.SourcedApplied, null, "direct", Now, 1, Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RecruitmentApplication(1, 10, 20, null, ApplicationStage.SourcedApplied, 101m, "direct", Now, 1, Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => new RecruitmentApplication(1, 10, 20, null, ApplicationStage.SourcedApplied, null, "direct", Now, 0, Now));
        Assert.Throws<ArgumentException>(() => new RecruitmentApplication(1, 10, 20, null, ApplicationStage.SourcedApplied, null, " ", Now, 1, Now));
    }

    private static RecruitmentApplication Create(ApplicationStage stage, long version) =>
        new(42, 10, 20, 30, stage, 75.5m, "direct", Now.AddDays(-1), version, Now.AddMinutes(-5));
}
