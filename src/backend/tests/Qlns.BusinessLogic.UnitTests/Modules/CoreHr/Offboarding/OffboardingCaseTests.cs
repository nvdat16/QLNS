using Qlns.BusinessLogic.Modules.CoreHr.EmployeeEvents;
using Qlns.BusinessLogic.Modules.CoreHr.Offboarding;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Xunit;
using static Qlns.BusinessLogic.UnitTests.Modules.CoreHr.Offboarding.OffboardingTestData;

namespace Qlns.BusinessLogic.UnitTests.Modules.CoreHr.Offboarding;

public sealed class OffboardingCaseTests
{
    [Fact]
    public void Open_Valid_BuildsDraftWithTrimmedReason()
    {
        var offboardingCase = OffboardingCase.Open(
            Write(reason: "  Moving abroad  ", noticeReceivedOn: new DateOnly(2026, 9, 1)), createdBy: 7, Today, Now);

        Assert.Equal(0, offboardingCase.Id);
        Assert.Equal(EmployeeId, offboardingCase.EmployeeId);
        Assert.Equal(OffboardingCaseStatus.Draft, offboardingCase.Status);
        Assert.Equal(SeparationType.Resignation, offboardingCase.SeparationType);
        Assert.Equal(FinalSettlementStatus.Pending, offboardingCase.FinalSettlementStatus);
        Assert.Equal("Moving abroad", offboardingCase.Reason);
        Assert.Equal(new DateOnly(2026, 9, 1), offboardingCase.NoticeReceivedOn);
        Assert.Equal(LastWorkingDate, offboardingCase.LastWorkingDate);
        Assert.Equal(43, offboardingCase.HandoverToEmployeeId);
        Assert.Equal(7, offboardingCase.CreatedBy);
        Assert.Equal(1, offboardingCase.Version);
        Assert.Equal(Now, offboardingCase.CreatedAt);
        Assert.Null(offboardingCase.EmployeeEventId);
    }

    [Fact]
    public void Open_LastWorkingDateToday_IsAllowed()
    {
        var offboardingCase = OffboardingCase.Open(Write(lastWorkingDate: Today, noticeReceivedOn: Today), 7, Today, Now);

        Assert.Equal(Today, offboardingCase.LastWorkingDate);
    }

    [Fact]
    public void Open_LastWorkingDateInPast_ThrowsValidationOnLastWorkingDate()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            OffboardingCase.Open(Write(lastWorkingDate: Today.AddDays(-1)), 7, Today, Now));

        Assert.Equal(["lastWorkingDate"], exception.Errors.Keys);
    }

    [Fact]
    public void Open_NoticeAfterLastWorkingDate_ThrowsValidationOnNoticeReceivedOn()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            OffboardingCase.Open(Write(noticeReceivedOn: LastWorkingDate.AddDays(1)), 7, Today, Now));

        Assert.Equal(["noticeReceivedOn"], exception.Errors.Keys);
    }

    [Fact]
    public void Open_HandoverToSelf_ThrowsValidationOnHandover()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            OffboardingCase.Open(Write(handoverToEmployeeId: EmployeeId), 7, Today, Now));

        Assert.Equal(["handoverToEmployeeId"], exception.Errors.Keys);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("layoff")]
    public void Open_UnknownSeparationType_ThrowsValidationOnSeparationType(string? separationType)
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            OffboardingCase.Open(Write(separationType: separationType), 7, Today, Now));

        Assert.Equal(["separationType"], exception.Errors.Keys);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    public void Open_BlankReason_ThrowsValidationOnReason(string? reason)
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            OffboardingCase.Open(Write(reason: reason), 7, Today, Now));

        Assert.Equal(["reason"], exception.Errors.Keys);
    }

    [Fact]
    public void Open_TooLongReason_ThrowsValidationOnReason()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            OffboardingCase.Open(Write(reason: new string('x', OffboardingCase.ReasonMaxLength + 1)), 7, Today, Now));

        Assert.Equal(["reason"], exception.Errors.Keys);
    }

    [Fact]
    public void Open_ManyProblems_ReportsEveryField()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() => OffboardingCase.Open(
            new OffboardingCaseWrite(0, "x", LastWorkingDate.AddDays(5), Today.AddDays(-3), -1, ""), 7, Today, Now));

        Assert.Equal(
            ["employeeId", "handoverToEmployeeId", "lastWorkingDate", "noticeReceivedOn", "reason", "separationType"],
            exception.Errors.Keys.Order());
    }

    [Fact]
    public void OpenForProbationTermination_BuildsDismissalDraftOnEffectiveDate()
    {
        var offboardingCase = OffboardingCase.OpenForProbationTermination(EmployeeId, LastWorkingDate, " Not meeting targets ", 9, Now);

        Assert.Equal(SeparationType.Dismissal, offboardingCase.SeparationType);
        Assert.Equal(OffboardingCaseStatus.Draft, offboardingCase.Status);
        Assert.Equal(LastWorkingDate, offboardingCase.LastWorkingDate);
        Assert.Null(offboardingCase.NoticeReceivedOn);
        Assert.Null(offboardingCase.HandoverToEmployeeId);
        Assert.Equal("Not meeting targets", offboardingCase.Reason);
        Assert.Equal(9, offboardingCase.CreatedBy);
    }

    [Fact]
    public void OpenForProbationTermination_BlankReason_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            OffboardingCase.OpenForProbationTermination(EmployeeId, LastWorkingDate, "  ", 9, Now));
    }

    [Theory]
    [InlineData("active", true)]
    [InlineData("probation", true)]
    [InlineData("suspended", false)]
    [InlineData("terminated", false)]
    public void IsEligibleEmployeeStatus_OnlyActiveAndProbation(string status, bool expected)
    {
        Assert.Equal(expected, OffboardingCase.IsEligibleEmployeeStatus(status));
    }

    [Fact]
    public void NoticePeriodShortfallDays_UsesNoticeRule()
    {
        var offboardingCase = CreateCase(OffboardingCaseStatus.Draft, noticeReceivedOn: LastWorkingDate.AddDays(-10));

        Assert.Equal(20, offboardingCase.NoticePeriodShortfallDays(30));
        Assert.Equal(0, offboardingCase.NoticePeriodShortfallDays(10));
        Assert.Null(offboardingCase.NoticePeriodShortfallDays(null));
    }

    [Theory]
    [InlineData(OffboardingCaseStatus.Draft)]
    [InlineData(OffboardingCaseStatus.PendingApproval)]
    public void Approve_FromDraftOrPendingApproval_SetsApproverAndBumpsVersion(OffboardingCaseStatus current)
    {
        var offboardingCase = CreateCase(current, version: 3);

        offboardingCase.Approve(9, Now);

        Assert.Equal(OffboardingCaseStatus.Approved, offboardingCase.Status);
        Assert.Equal(9, offboardingCase.ApprovedBy);
        Assert.Equal(Now, offboardingCase.ApprovedAt);
        Assert.Equal(4, offboardingCase.Version);
        Assert.Equal(Now, offboardingCase.UpdatedAt);
    }

    [Theory]
    [InlineData(OffboardingCaseStatus.Approved)]
    [InlineData(OffboardingCaseStatus.InProgress)]
    [InlineData(OffboardingCaseStatus.Completed)]
    [InlineData(OffboardingCaseStatus.Cancelled)]
    public void Approve_FromOtherStatus_ThrowsInvalidTransition(OffboardingCaseStatus current)
    {
        var offboardingCase = CreateCase(current, version: 1);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => offboardingCase.Approve(9, Now));

        Assert.Equal(OffboardingCase.InvalidTransitionCode, exception.Code);
        Assert.Equal(current.ToContract(), exception.Details["currentStatus"]);
        Assert.Equal("approve", exception.Details["action"]);
        Assert.Equal(1, offboardingCase.Version);
    }

    [Fact]
    public void Start_FromApproved_MovesToInProgress()
    {
        var offboardingCase = CreateCase(OffboardingCaseStatus.Approved, version: 2);

        offboardingCase.Start(Now);

        Assert.Equal(OffboardingCaseStatus.InProgress, offboardingCase.Status);
        Assert.Equal(3, offboardingCase.Version);
    }

    [Theory]
    [InlineData(OffboardingCaseStatus.Draft)]
    [InlineData(OffboardingCaseStatus.InProgress)]
    [InlineData(OffboardingCaseStatus.Completed)]
    [InlineData(OffboardingCaseStatus.Cancelled)]
    public void Start_FromNonApproved_ThrowsInvalidTransition(OffboardingCaseStatus current)
    {
        var offboardingCase = CreateCase(current, version: 1);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => offboardingCase.Start(Now));

        Assert.Equal(OffboardingCase.InvalidTransitionCode, exception.Code);
    }

    [Fact]
    public void Complete_AllClear_CompletesAndYieldsApprovedTerminationEvent()
    {
        var offboardingCase = CreateCase(OffboardingCaseStatus.InProgress, version: 4, settlement: FinalSettlementStatus.Paid);
        var tasks = new[]
        {
            CreateTask(1, "it.devices", blocks: true, OffboardingTaskStatus.Completed),
            CreateTask(2, "hr.exit_interview", blocks: false, OffboardingTaskStatus.Pending)
        };

        var completion = offboardingCase.Complete(tasks, actorMayOverride: false, reason: null, "active", Now);

        Assert.Equal(OffboardingCaseStatus.Completed, offboardingCase.Status);
        Assert.Equal(Now, offboardingCase.CompletedAt);
        Assert.Equal(5, offboardingCase.Version);
        Assert.False(completion.Overridden);

        var terminationEvent = Assert.IsType<ApprovedEmployeeEvent>(completion.TerminationEvent);
        Assert.Equal(EmployeeId, terminationEvent.EmployeeId);
        Assert.Equal(EmployeeEventType.Termination, terminationEvent.EventType);
        Assert.Equal(LastWorkingDate, terminationEvent.EffectiveDate);
        Assert.Equal("active", terminationEvent.BeforeStatus);
        Assert.Equal(EmployeeStatusValues.Terminated, terminationEvent.AfterStatus);
        Assert.Equal("Moving abroad", terminationEvent.Reason);
        Assert.Equal("active", terminationEvent.BeforeData["status"]!.GetValue<string>());
        Assert.Equal("terminated", terminationEvent.AfterData["status"]!.GetValue<string>());
        Assert.Equal(["status"], terminationEvent.ChangedFields);
    }

    [Fact]
    public void Complete_SettlementWaived_IsAccepted()
    {
        var offboardingCase = CreateCase(OffboardingCaseStatus.InProgress, settlement: FinalSettlementStatus.Waived);

        var completion = offboardingCase.Complete([], false, null, "probation", Now);

        Assert.Equal(OffboardingCaseStatus.Completed, offboardingCase.Status);
        Assert.Equal("probation", completion.TerminationEvent!.BeforeStatus);
    }

    [Fact]
    public void Complete_AlreadyLinkedEvent_DoesNotYieldAnotherEvent()
    {
        var offboardingCase = CreateCase(OffboardingCaseStatus.InProgress, settlement: FinalSettlementStatus.Paid, employeeEventId: 900);

        var completion = offboardingCase.Complete([], false, null, "active", Now);

        Assert.Null(completion.TerminationEvent);
        Assert.Equal(900, offboardingCase.EmployeeEventId);
    }

    [Theory]
    [InlineData(OffboardingCaseStatus.Draft)]
    [InlineData(OffboardingCaseStatus.Approved)]
    [InlineData(OffboardingCaseStatus.Completed)]
    [InlineData(OffboardingCaseStatus.Cancelled)]
    public void Complete_FromNonInProgress_ThrowsInvalidTransition(OffboardingCaseStatus current)
    {
        var offboardingCase = CreateCase(current, settlement: FinalSettlementStatus.Paid);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() =>
            offboardingCase.Complete([], true, "reason", "active", Now));

        Assert.Equal(OffboardingCase.InvalidTransitionCode, exception.Code);
    }

    [Fact]
    public void Complete_BlockingTasksOutstanding_ThrowsWithTaskList()
    {
        var offboardingCase = CreateCase(OffboardingCaseStatus.InProgress, version: 4, settlement: FinalSettlementStatus.Paid);
        var tasks = new[]
        {
            CreateTask(1, "it.devices", blocks: true, OffboardingTaskStatus.Pending),
            CreateTask(2, "it.accounts", blocks: true, OffboardingTaskStatus.InProgress),
            CreateTask(3, "manager.handover", blocks: true, OffboardingTaskStatus.Completed),
            CreateTask(4, "hr.exit_interview", blocks: false, OffboardingTaskStatus.Pending)
        };

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() =>
            offboardingCase.Complete(tasks, actorMayOverride: false, reason: null, "active", Now));

        Assert.Equal(OffboardingCase.BlockingTasksOutstandingCode, exception.Code);
        var blocking = Assert.IsAssignableFrom<IReadOnlyList<BlockingTaskSummary>>(exception.Details["blockingTasks"]);
        Assert.Equal([1L, 2L], blocking.Select(t => t.Id));
        Assert.Equal(["it.devices", "it.accounts"], blocking.Select(t => t.TemplateKey));
        Assert.Equal(["pending", "in_progress"], blocking.Select(t => t.Status));
        Assert.Equal(OffboardingCaseStatus.InProgress, offboardingCase.Status);
        Assert.Equal(4, offboardingCase.Version);
    }

    [Fact]
    public void Complete_BlockingOutstanding_OverriderWithoutReason_IsRefused()
    {
        var offboardingCase = CreateCase(OffboardingCaseStatus.InProgress, settlement: FinalSettlementStatus.Paid);
        var tasks = new[] { CreateTask(1, "it.devices", blocks: true, OffboardingTaskStatus.Pending) };

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() =>
            offboardingCase.Complete(tasks, actorMayOverride: true, reason: "  ", "active", Now));

        Assert.Equal(OffboardingCase.BlockingTasksOutstandingCode, exception.Code);
    }

    [Fact]
    public void Complete_BlockingOutstanding_OverriderWithReason_CompletesAndFlagsOverride()
    {
        var offboardingCase = CreateCase(OffboardingCaseStatus.InProgress, settlement: FinalSettlementStatus.Paid);
        var tasks = new[] { CreateTask(1, "it.devices", blocks: true, OffboardingTaskStatus.Pending) };

        var completion = offboardingCase.Complete(tasks, actorMayOverride: true, reason: "Laptop written off", "active", Now);

        Assert.True(completion.Overridden);
        Assert.Equal(OffboardingCaseStatus.Completed, offboardingCase.Status);
        Assert.NotNull(completion.TerminationEvent);
    }

    [Theory]
    [InlineData(FinalSettlementStatus.Pending)]
    [InlineData(FinalSettlementStatus.Calculated)]
    public void Complete_SettlementNotSettled_ThrowsEvenForOverrider(FinalSettlementStatus settlement)
    {
        var offboardingCase = CreateCase(OffboardingCaseStatus.InProgress, version: 4, settlement: settlement);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() =>
            offboardingCase.Complete([], actorMayOverride: true, reason: "override", "active", Now));

        Assert.Equal(OffboardingCase.SettlementPendingCode, exception.Code);
        Assert.Equal(settlement.ToContract(), exception.Details["finalSettlementStatus"]);
        Assert.Equal(OffboardingCaseStatus.InProgress, offboardingCase.Status);
        Assert.Equal(4, offboardingCase.Version);
    }

    [Fact]
    public void Complete_BlockingAndSettlementProblems_ReportsBlockingFirst()
    {
        var offboardingCase = CreateCase(OffboardingCaseStatus.InProgress, settlement: FinalSettlementStatus.Pending);
        var tasks = new[] { CreateTask(1, "it.devices", blocks: true, OffboardingTaskStatus.Pending) };

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() =>
            offboardingCase.Complete(tasks, false, null, "active", Now));

        Assert.Equal(OffboardingCase.BlockingTasksOutstandingCode, exception.Code);
    }

    [Theory]
    [InlineData(OffboardingCaseStatus.Draft)]
    [InlineData(OffboardingCaseStatus.PendingApproval)]
    [InlineData(OffboardingCaseStatus.Approved)]
    [InlineData(OffboardingCaseStatus.InProgress)]
    public void Cancel_FromOpenStatus_MovesToCancelled(OffboardingCaseStatus current)
    {
        var offboardingCase = CreateCase(current, version: 2);

        offboardingCase.Cancel("Employee withdrew resignation", Now);

        Assert.Equal(OffboardingCaseStatus.Cancelled, offboardingCase.Status);
        Assert.Equal(3, offboardingCase.Version);
    }

    [Theory]
    [InlineData(OffboardingCaseStatus.Completed)]
    [InlineData(OffboardingCaseStatus.Cancelled)]
    public void Cancel_FromFinalStatus_ThrowsInvalidTransition(OffboardingCaseStatus current)
    {
        var offboardingCase = CreateCase(current);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => offboardingCase.Cancel("reason", Now));

        Assert.Equal(OffboardingCase.InvalidTransitionCode, exception.Code);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Cancel_WithoutReason_ThrowsValidationOnReason(string? reason)
    {
        var offboardingCase = CreateCase(OffboardingCaseStatus.Approved, version: 2);

        var exception = Assert.Throws<CoreHrValidationException>(() => offboardingCase.Cancel(reason, Now));

        Assert.Equal(["reason"], exception.Errors.Keys);
        Assert.Equal(OffboardingCaseStatus.Approved, offboardingCase.Status);
        Assert.Equal(2, offboardingCase.Version);
    }

    [Fact]
    public void LinkEmployeeEvent_SetsIdWithoutBumpingVersion()
    {
        var offboardingCase = CreateCase(OffboardingCaseStatus.Completed, version: 5);

        offboardingCase.LinkEmployeeEvent(901);

        Assert.Equal(901, offboardingCase.EmployeeEventId);
        Assert.Equal(5, offboardingCase.Version);
    }

    [Fact]
    public void LinkEmployeeEvent_DifferentExistingLink_Throws()
    {
        var offboardingCase = CreateCase(OffboardingCaseStatus.Completed, employeeEventId: 900);

        Assert.Throws<InvalidOperationException>(() => offboardingCase.LinkEmployeeEvent(901));
        Assert.Throws<ArgumentOutOfRangeException>(() => offboardingCase.LinkEmployeeEvent(0));
    }

    [Fact]
    public void Constructor_RejectsUnsavedNonDraftAndSelfHandover()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateCase(OffboardingCaseStatus.Approved, id: 0));
        Assert.Throws<ArgumentException>(() => CreateCase(OffboardingCaseStatus.Draft, handoverToEmployeeId: EmployeeId));
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateCase(OffboardingCaseStatus.Draft, version: 0));
    }
}
