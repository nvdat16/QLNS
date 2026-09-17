using Qlns.BusinessLogic.Modules.CoreHr.EmployeeEvents;
using Qlns.BusinessLogic.Modules.CoreHr.Offboarding;
using Qlns.BusinessLogic.Modules.CoreHr.Probation;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Xunit;
using static Qlns.BusinessLogic.UnitTests.Modules.CoreHr.Probation.ProbationTestData;

namespace Qlns.BusinessLogic.UnitTests.Modules.CoreHr.Probation;

public sealed class ProbationReviewTests
{
    [Theory]
    [InlineData(ProbationReviewStatus.Pending, true)]
    [InlineData(ProbationReviewStatus.InReview, true)]
    [InlineData(ProbationReviewStatus.Decided, false)]
    [InlineData(ProbationReviewStatus.Cancelled, false)]
    public void IsOverdue_PastDueDate_OnlyWhileOpen(ProbationReviewStatus status, bool expected)
    {
        var review = CreateReview(status, reviewDueDate: Today.AddDays(-1));

        Assert.Equal(expected, review.IsOverdue(Today));
    }

    [Fact]
    public void IsOverdue_DueTodayOrLater_IsFalse()
    {
        Assert.False(CreateReview(ProbationReviewStatus.Pending, reviewDueDate: Today).IsOverdue(Today));
        Assert.False(CreateReview(ProbationReviewStatus.Pending, reviewDueDate: Today.AddDays(1)).IsOverdue(Today));
    }

    [Fact]
    public void CanBeAssessedBy_ReviewerOrManagePermissionOnly()
    {
        var review = CreateReview(ProbationReviewStatus.Pending);

        Assert.True(review.CanBeAssessedBy(Actor(userId: ReviewerUserId)));
        Assert.True(review.CanBeAssessedBy(Actor(userId: 99, permissions: ProbationPermissions.Manage)));
        Assert.False(review.CanBeAssessedBy(Actor(userId: 99, permissions: [ProbationPermissions.Read, ProbationPermissions.Decide])));
        Assert.False(CreateReview(ProbationReviewStatus.Pending, reviewerUserId: null).CanBeAssessedBy(Actor(userId: ReviewerUserId)));
    }

    [Fact]
    public void Submit_FromPending_StoresAssessmentAndRecommendationAsOutcome()
    {
        var review = CreateReview(ProbationReviewStatus.Pending, version: 1);

        review.Submit(Write(score: 4.5m, strengths: "  Fast learner ", improvements: "  Time management ", recommendedOutcome: "extended"), Now);

        Assert.Equal(ProbationReviewStatus.InReview, review.Status);
        Assert.Equal(4.5m, review.OverallScore);
        Assert.Equal("Fast learner", review.Strengths);
        Assert.Equal("Time management", review.Improvements);
        Assert.Equal(ProbationOutcome.Extended, review.Outcome);
        Assert.Equal(2, review.Version);
        Assert.Equal(Now, review.UpdatedAt);
        Assert.Null(review.EffectiveDate);
        Assert.Null(review.DecidedBy);
    }

    [Fact]
    public void Submit_FromInReview_ReplacesAssessment()
    {
        var review = CreateReview(ProbationReviewStatus.InReview, version: 2, improvements: "Old notes");

        review.Submit(Write(score: 3m, improvements: null, recommendedOutcome: "confirmed"), Now);

        Assert.Equal(ProbationReviewStatus.InReview, review.Status);
        Assert.Equal(3.0m, review.OverallScore);
        Assert.Null(review.Improvements);
        Assert.Equal(3, review.Version);
    }

    [Theory]
    [InlineData(ProbationReviewStatus.Decided)]
    [InlineData(ProbationReviewStatus.Cancelled)]
    public void Submit_FromClosedStatus_ThrowsInvalidTransition(ProbationReviewStatus status)
    {
        var review = CreateReview(status, version: 3);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => review.Submit(Write(), Now));

        Assert.Equal(ProbationReview.InvalidTransitionCode, exception.Code);
        Assert.Equal(status.ToContract(), exception.Details["currentStatus"]);
        Assert.Equal(3, review.Version);
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(5.1)]
    [InlineData(3.25)]
    public void Submit_ScoreOutOfRangeOrTooPrecise_ThrowsValidationOnOverallScore(double score)
    {
        var review = CreateReview(ProbationReviewStatus.Pending);

        var exception = Assert.Throws<CoreHrValidationException>(() => review.Submit(Write(score: (decimal)score), Now));

        Assert.Equal(["overallScore"], exception.Errors.Keys);
        Assert.Equal(ProbationReviewStatus.Pending, review.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(2.5)]
    public void Submit_BoundaryScores_AreAccepted(double score)
    {
        var review = CreateReview(ProbationReviewStatus.Pending);

        review.Submit(Write(score: (decimal)score), Now);

        Assert.Equal((decimal)score, review.OverallScore);
    }

    [Fact]
    public void Submit_MissingScore_ThrowsValidationOnOverallScore()
    {
        var review = CreateReview(ProbationReviewStatus.Pending);

        var exception = Assert.Throws<CoreHrValidationException>(() => review.Submit(Write(score: null), Now));

        Assert.Equal(["overallScore"], exception.Errors.Keys);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Submit_BlankStrengths_ThrowsValidationOnStrengths(string? strengths)
    {
        var review = CreateReview(ProbationReviewStatus.Pending);

        var exception = Assert.Throws<CoreHrValidationException>(() => review.Submit(Write(strengths: strengths), Now));

        Assert.Equal(["strengths"], exception.Errors.Keys);
    }

    [Fact]
    public void Submit_TooLongTexts_ReportsBothFields()
    {
        var review = CreateReview(ProbationReviewStatus.Pending);
        var tooLong = new string('a', ProbationReview.TextMaxLength + 1);

        var exception = Assert.Throws<CoreHrValidationException>(() =>
            review.Submit(Write(strengths: tooLong, improvements: tooLong), Now));

        Assert.Equal(["improvements", "strengths"], exception.Errors.Keys.Order());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("rejected")]
    public void Submit_UnknownRecommendation_ThrowsValidationOnRecommendedOutcome(string? recommended)
    {
        var review = CreateReview(ProbationReviewStatus.Pending);

        var exception = Assert.Throws<CoreHrValidationException>(() => review.Submit(Write(recommendedOutcome: recommended), Now));

        Assert.Equal(["recommendedOutcome"], exception.Errors.Keys);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void Submit_TerminatedWithoutImprovements_ThrowsValidationOnImprovements(string? improvements)
    {
        var review = CreateReview(ProbationReviewStatus.Pending);

        var exception = Assert.Throws<CoreHrValidationException>(() =>
            review.Submit(Write(improvements: improvements, recommendedOutcome: "terminated"), Now));

        Assert.Equal(["improvements"], exception.Errors.Keys);
    }

    [Fact]
    public void Submit_TerminatedWithImprovements_IsAccepted()
    {
        var review = CreateReview(ProbationReviewStatus.Pending);

        review.Submit(Write(improvements: "Missed all targets", recommendedOutcome: "terminated"), Now);

        Assert.Equal(ProbationOutcome.Terminated, review.Outcome);
    }

    [Fact]
    public void Decide_Confirmed_SetsDecisionAndPlansProbationConfirmationEvent()
    {
        var review = CreateReview(ProbationReviewStatus.InReview, version: 2);
        var effectiveDate = new DateOnly(2026, 10, 1);

        var plan = review.Decide(Decision("confirmed", effectiveDate), HrManagerUserId, Today, Now);

        Assert.Equal(ProbationReviewStatus.Decided, review.Status);
        Assert.Equal(ProbationOutcome.Confirmed, review.Outcome);
        Assert.Equal(effectiveDate, review.EffectiveDate);
        Assert.Equal(HrManagerUserId, review.DecidedBy);
        Assert.Equal(Now, review.DecidedAt);
        Assert.Equal(3, review.Version);
        Assert.Null(review.EmployeeEventId);

        Assert.Equal(EmployeeId, plan.EmployeeEvent.EmployeeId);
        Assert.Equal(EmployeeEventType.ProbationConfirmation, plan.EmployeeEvent.EventType);
        Assert.Equal(effectiveDate, plan.EmployeeEvent.EffectiveDate);
        Assert.Equal(EmployeeStatusValues.Probation, plan.EmployeeEvent.BeforeStatus);
        Assert.Equal(EmployeeStatusValues.Active, plan.EmployeeEvent.AfterStatus);
        Assert.Equal(ProbationReview.DefaultDecisionReason, plan.EmployeeEvent.Reason);
        Assert.Null(plan.OffboardingCase);
    }

    [Fact]
    public void Decide_Extended_PlansProbationExtensionKeepingProbationStatus()
    {
        var review = CreateReview(ProbationReviewStatus.InReview);

        var plan = review.Decide(Decision("extended", reason: "  Extend by two months "), HrManagerUserId, Today, Now);

        Assert.Equal(EmployeeEventType.ProbationExtension, plan.EmployeeEvent.EventType);
        Assert.Equal(EmployeeStatusValues.Probation, plan.EmployeeEvent.AfterStatus);
        Assert.Equal("Extend by two months", plan.EmployeeEvent.Reason);
        Assert.Null(plan.OffboardingCase);
    }

    [Fact]
    public void Decide_Terminated_PlansTerminationEventAndDismissalCaseFromImprovements()
    {
        var review = CreateReview(ProbationReviewStatus.InReview, outcome: ProbationOutcome.Terminated, improvements: "Missed all targets");
        var effectiveDate = new DateOnly(2026, 10, 1);

        var plan = review.Decide(Decision("terminated", effectiveDate), HrManagerUserId, Today, Now);

        Assert.Equal(EmployeeEventType.Termination, plan.EmployeeEvent.EventType);
        Assert.Equal(EmployeeStatusValues.Terminated, plan.EmployeeEvent.AfterStatus);

        var offboardingCase = Assert.IsType<OffboardingCase>(plan.OffboardingCase);
        Assert.Equal(0, offboardingCase.Id);
        Assert.Equal(EmployeeId, offboardingCase.EmployeeId);
        Assert.Equal(OffboardingCaseStatus.Draft, offboardingCase.Status);
        Assert.Equal(SeparationType.Dismissal, offboardingCase.SeparationType);
        Assert.Equal(effectiveDate, offboardingCase.LastWorkingDate);
        Assert.Equal("Missed all targets", offboardingCase.Reason);
        Assert.Equal(HrManagerUserId, offboardingCase.CreatedBy);
        Assert.Equal(FinalSettlementStatus.Pending, offboardingCase.FinalSettlementStatus);
    }

    [Fact]
    public void Decide_TerminatedWithoutImprovements_UsesDecisionReasonForCase()
    {
        var review = CreateReview(ProbationReviewStatus.InReview, improvements: null);

        var plan = review.Decide(Decision("terminated", reason: "Position discontinued"), HrManagerUserId, Today, Now);

        Assert.Equal("Position discontinued", plan.OffboardingCase!.Reason);
    }

    [Fact]
    public void Decide_OverridesTheRecommendation()
    {
        var review = CreateReview(ProbationReviewStatus.InReview, outcome: ProbationOutcome.Extended);

        review.Decide(Decision("confirmed"), HrManagerUserId, Today, Now);

        Assert.Equal(ProbationOutcome.Confirmed, review.Outcome);
    }

    [Fact]
    public void Decide_FromPending_ThrowsNotReviewed()
    {
        var review = CreateReview(ProbationReviewStatus.Pending, version: 1);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => review.Decide(Decision(), HrManagerUserId, Today, Now));

        Assert.Equal(ProbationReview.NotReviewedCode, exception.Code);
        Assert.Equal(1, review.Version);
    }

    [Theory]
    [InlineData(ProbationReviewStatus.Decided)]
    [InlineData(ProbationReviewStatus.Cancelled)]
    public void Decide_FromClosedStatus_ThrowsInvalidTransition(ProbationReviewStatus status)
    {
        var review = CreateReview(status);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => review.Decide(Decision(), HrManagerUserId, Today, Now));

        Assert.Equal(ProbationReview.InvalidTransitionCode, exception.Code);
    }

    [Fact]
    public void Decide_MissingOutcomeAndEffectiveDate_ReportsBothFields()
    {
        var review = CreateReview(ProbationReviewStatus.InReview, version: 2);

        var exception = Assert.Throws<CoreHrValidationException>(() =>
            review.Decide(ProbationDecision.Empty, HrManagerUserId, Today, Now));

        Assert.Equal(["effectiveDate", "outcome"], exception.Errors.Keys.Order());
        Assert.Equal(ProbationReviewStatus.InReview, review.Status);
        Assert.Equal(2, review.Version);
    }

    [Fact]
    public void Decide_UnknownOutcome_ThrowsValidationOnOutcome()
    {
        var review = CreateReview(ProbationReviewStatus.InReview);

        var exception = Assert.Throws<CoreHrValidationException>(() =>
            review.Decide(Decision("rejected"), HrManagerUserId, Today, Now));

        Assert.Equal(["outcome"], exception.Errors.Keys);
    }

    [Fact]
    public void Decide_EffectiveDateInPast_ThrowsValidationOnEffectiveDate()
    {
        var review = CreateReview(ProbationReviewStatus.InReview);

        var exception = Assert.Throws<CoreHrValidationException>(() =>
            review.Decide(Decision(effectiveDate: Today.AddDays(-1)), HrManagerUserId, Today, Now));

        Assert.Equal(["effectiveDate"], exception.Errors.Keys);
    }

    [Fact]
    public void Decide_EffectiveDateToday_IsAccepted()
    {
        var review = CreateReview(ProbationReviewStatus.InReview);

        var plan = review.Decide(Decision(effectiveDate: Today), HrManagerUserId, Today, Now);

        Assert.Equal(Today, plan.EmployeeEvent.EffectiveDate);
    }

    [Fact]
    public void Decide_TooLongReason_ThrowsValidationOnReason()
    {
        var review = CreateReview(ProbationReviewStatus.InReview);

        var exception = Assert.Throws<CoreHrValidationException>(() =>
            review.Decide(Decision(reason: new string('r', 1001)), HrManagerUserId, Today, Now));

        Assert.Equal(["reason"], exception.Errors.Keys);
    }

    [Theory]
    [InlineData(ProbationReviewStatus.Pending)]
    [InlineData(ProbationReviewStatus.InReview)]
    public void Cancel_FromOpenStatus_MovesToCancelled(ProbationReviewStatus status)
    {
        var review = CreateReview(status, version: 2);

        review.Cancel("Contract withdrawn", Now);

        Assert.Equal(ProbationReviewStatus.Cancelled, review.Status);
        Assert.Equal(3, review.Version);
    }

    [Theory]
    [InlineData(ProbationReviewStatus.Decided)]
    [InlineData(ProbationReviewStatus.Cancelled)]
    public void Cancel_FromClosedStatus_ThrowsInvalidTransition(ProbationReviewStatus status)
    {
        var review = CreateReview(status);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => review.Cancel("reason", Now));

        Assert.Equal(ProbationReview.InvalidTransitionCode, exception.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void Cancel_WithoutReason_ThrowsValidationOnReason(string? reason)
    {
        var review = CreateReview(ProbationReviewStatus.Pending, version: 1);

        var exception = Assert.Throws<CoreHrValidationException>(() => review.Cancel(reason, Now));

        Assert.Equal(["reason"], exception.Errors.Keys);
        Assert.Equal(1, review.Version);
    }

    [Theory]
    [InlineData(EmployeeEventStatus.Approved)]
    [InlineData(EmployeeEventStatus.PendingApproval)]
    [InlineData(null)]
    public void Unlock_DecidedWithUnappliedEvent_ReturnsToInReviewKeepingRecommendation(EmployeeEventStatus? linkedStatus)
    {
        var review = CreateReview(ProbationReviewStatus.Decided, version: 3, outcome: ProbationOutcome.Extended, employeeEventId: 900);

        var eventId = review.Unlock("Wrong effective date", linkedStatus, Now);

        Assert.Equal(900, eventId);
        Assert.Equal(ProbationReviewStatus.InReview, review.Status);
        Assert.Equal(ProbationOutcome.Extended, review.Outcome);
        Assert.Null(review.EmployeeEventId);
        Assert.Null(review.DecidedBy);
        Assert.Null(review.DecidedAt);
        Assert.Null(review.EffectiveDate);
        Assert.Equal(4.0m, review.OverallScore);
        Assert.Equal(4, review.Version);
    }

    [Fact]
    public void Unlock_AppliedEvent_ThrowsEventApplied()
    {
        var review = CreateReview(ProbationReviewStatus.Decided, version: 3, employeeEventId: 900);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() =>
            review.Unlock("Wrong effective date", EmployeeEventStatus.Applied, Now));

        Assert.Equal(ProbationReview.EventAppliedCode, exception.Code);
        Assert.Equal(900L, exception.Details["employeeEventId"]);
        Assert.Equal(ProbationReviewStatus.Decided, review.Status);
        Assert.Equal(3, review.Version);
    }

    [Theory]
    [InlineData(ProbationReviewStatus.Pending)]
    [InlineData(ProbationReviewStatus.InReview)]
    [InlineData(ProbationReviewStatus.Cancelled)]
    public void Unlock_FromNonDecided_ThrowsInvalidTransition(ProbationReviewStatus status)
    {
        var review = CreateReview(status);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => review.Unlock("reason", null, Now));

        Assert.Equal(ProbationReview.InvalidTransitionCode, exception.Code);
    }

    [Fact]
    public void Unlock_WithoutReason_ThrowsValidationOnReason()
    {
        var review = CreateReview(ProbationReviewStatus.Decided);

        var exception = Assert.Throws<CoreHrValidationException>(() => review.Unlock(" ", EmployeeEventStatus.Approved, Now));

        Assert.Equal(["reason"], exception.Errors.Keys);
    }

    [Fact]
    public void Unlock_DecidedWithoutLinkedEvent_IsDataCorruption()
    {
        var review = new ProbationReview(
            ReviewId, EmployeeId, ContractId, DueDate, ReviewerUserId, ProbationReviewStatus.Decided, ProbationOutcome.Confirmed,
            4m, "ok", null, new DateOnly(2026, 10, 1), HrManagerUserId, Now, employeeEventId: null, 3, Now, Now);

        Assert.Throws<InvalidOperationException>(() => review.Unlock("reason", null, Now));
    }

    [Fact]
    public void LinkEmployeeEvent_SetsIdWithoutBumpingVersion()
    {
        var review = CreateReview(ProbationReviewStatus.InReview, version: 3);

        review.LinkEmployeeEvent(901);

        Assert.Equal(901, review.EmployeeEventId);
        Assert.Equal(3, review.Version);
        Assert.Throws<InvalidOperationException>(() => review.LinkEmployeeEvent(902));
        Assert.Throws<ArgumentOutOfRangeException>(() => review.LinkEmployeeEvent(0));
    }

    [Fact]
    public void Constructor_RejectsNonPositiveIdentifiersAndVersion()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateReview(ProbationReviewStatus.Pending, id: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateReview(ProbationReviewStatus.Pending, version: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateReview(ProbationReviewStatus.Pending, reviewerUserId: 0));
    }
}
