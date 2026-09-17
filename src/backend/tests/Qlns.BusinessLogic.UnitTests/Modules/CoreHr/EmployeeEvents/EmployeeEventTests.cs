using System.Text.Json.Nodes;
using Qlns.BusinessLogic.Modules.CoreHr.EmployeeEvents;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Xunit;

namespace Qlns.BusinessLogic.UnitTests.Modules.CoreHr.EmployeeEvents;

public sealed class EmployeeEventTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 7, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 9, 15);

    [Fact]
    public void CreateDraft_ValidTransfer_ReturnsDraftWithVersionOne()
    {
        var draft = EmployeeEvent.CreateDraft(42, Write(afterData: new JsonObject { ["departmentId"] = 7 }), createdBy: 5, Now);

        Assert.Equal(0, draft.Id);
        Assert.Equal(42, draft.EmployeeId);
        Assert.Equal(EmployeeEventType.Transfer, draft.EventType);
        Assert.Equal(EmployeeEventStatus.Draft, draft.Status);
        Assert.Equal(1, draft.Version);
        Assert.Equal(5, draft.CreatedBy);
        Assert.Equal(Now, draft.CreatedAt);
        Assert.Equal("Move to product", draft.Reason);
        Assert.Equal(["departmentId"], draft.ChangedFields);
    }

    [Theory]
    [InlineData("probation_confirmation")]
    [InlineData("suspension")]
    [InlineData("bogus")]
    [InlineData(null)]
    public void CreateDraft_NonWritableOrUnknownType_FailsOnEventType(string? eventType)
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            EmployeeEvent.CreateDraft(42, Write(eventType: eventType), 5, Now));

        Assert.Contains("eventType", exception.Errors.Keys);
    }

    [Fact]
    public void CreateDraft_EmptyAfterData_FailsOnAfterData()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            EmployeeEvent.CreateDraft(42, Write(afterData: new JsonObject()), 5, Now));

        Assert.Contains("afterData", exception.Errors.Keys);
    }

    [Fact]
    public void CreateDraft_UnknownAfterDataKey_FailsOnThatKey()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            EmployeeEvent.CreateDraft(42, Write(afterData: new JsonObject { ["nickname"] = "x" }), 5, Now));

        Assert.Contains("afterData.nickname", exception.Errors.Keys);
    }

    [Fact]
    public void CreateDraft_InvalidStatusValue_FailsOnAfterDataStatus()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            EmployeeEvent.CreateDraft(42, Write(afterData: new JsonObject { ["status"] = "fired" }), 5, Now));

        Assert.Contains("afterData.status", exception.Errors.Keys);
    }

    [Fact]
    public void CreateDraft_TerminationWithoutTerminatedStatus_FailsOnAfterDataStatus()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            EmployeeEvent.CreateDraft(42, Write(eventType: "termination", afterData: new JsonObject { ["positionId"] = 3 }), 5, Now));

        Assert.Contains("afterData.status", exception.Errors.Keys);
    }

    [Fact]
    public void CreateDraft_CorrectionWithoutCompensatesEventId_FailsOnCompensatesEventId()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            EmployeeEvent.CreateDraft(42, Write(eventType: "correction"), 5, Now));

        Assert.Contains("compensatesEventId", exception.Errors.Keys);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void CreateDraft_BlankReason_FailsOnReason(string? reason)
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            EmployeeEvent.CreateDraft(42, Write(reason: reason), 5, Now));

        Assert.Contains("reason", exception.Errors.Keys);
    }

    [Fact]
    public void CreateDraft_MissingEffectiveDate_FailsOnEffectiveDate()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            EmployeeEvent.CreateDraft(42, Write(effectiveDate: DateOnly.MinValue), 5, Now));

        Assert.Contains("effectiveDate", exception.Errors.Keys);
    }

    [Fact]
    public void Submit_FromDraft_MovesToPendingApprovalAndBumpsVersion()
    {
        var draft = Draft();
        var later = Now.AddMinutes(1);

        draft.Submit(later);

        Assert.Equal(EmployeeEventStatus.PendingApproval, draft.Status);
        Assert.Equal(2, draft.Version);
        Assert.Equal(later, draft.UpdatedAt);
    }

    [Fact]
    public void Approve_FromPendingApproval_SetsApproverAndBumpsVersion()
    {
        var pending = Event(EmployeeEventStatus.PendingApproval, version: 2);

        pending.Approve(approverUserId: 9, Now);

        Assert.Equal(EmployeeEventStatus.Approved, pending.Status);
        Assert.Equal(9, pending.ApprovedBy);
        Assert.Equal(Now, pending.ApprovedAt);
        Assert.Equal(3, pending.Version);
    }

    [Fact]
    public void Approve_FromDraft_ThrowsInvalidTransition()
    {
        var draft = Draft();

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => draft.Approve(9, Now));

        Assert.Equal("corehr.event.invalid_transition", exception.Code);
        Assert.Equal(EmployeeEventStatus.Draft, draft.Status);
        Assert.Equal(1, draft.Version);
    }

    [Theory]
    [InlineData(EmployeeEventStatus.Draft)]
    [InlineData(EmployeeEventStatus.PendingApproval)]
    [InlineData(EmployeeEventStatus.Approved)]
    public void Cancel_WithReason_FromOpenStatus_MovesToCancelledAndBumpsVersion(EmployeeEventStatus status)
    {
        var employeeEvent = Event(status, version: 3);

        employeeEvent.Cancel("Withdrawn by HR", Now);

        Assert.Equal(EmployeeEventStatus.Cancelled, employeeEvent.Status);
        Assert.Equal(4, employeeEvent.Version);
    }

    [Fact]
    public void Cancel_AppliedEvent_ThrowsImmutable()
    {
        var applied = Event(EmployeeEventStatus.Applied, version: 4);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => applied.Cancel("oops", Now));

        Assert.Equal("corehr.event.immutable", exception.Code);
        Assert.Equal(EmployeeEventStatus.Applied, applied.Status);
    }

    [Fact]
    public void Cancel_CancelledEvent_ThrowsInvalidTransition()
    {
        var cancelled = Event(EmployeeEventStatus.Cancelled, version: 2);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => cancelled.Cancel("again", Now));

        Assert.Equal("corehr.event.invalid_transition", exception.Code);
    }

    [Fact]
    public void Cancel_WithoutReason_ThrowsValidationOnReason()
    {
        var pending = Event(EmployeeEventStatus.PendingApproval, version: 2);

        var exception = Assert.Throws<CoreHrValidationException>(() => pending.Cancel("  ", Now));

        Assert.Contains("reason", exception.Errors.Keys);
        Assert.Equal(EmployeeEventStatus.PendingApproval, pending.Status);
        Assert.Equal(2, pending.Version);
    }

    [Theory]
    [InlineData(EmployeeEventStatus.Approved, "2026-09-15", true)]
    [InlineData(EmployeeEventStatus.Approved, "2026-09-01", true)]
    [InlineData(EmployeeEventStatus.Approved, "2026-09-16", false)]
    [InlineData(EmployeeEventStatus.PendingApproval, "2026-09-15", false)]
    [InlineData(EmployeeEventStatus.Applied, "2026-09-15", false)]
    public void IsDue_DependsOnApprovedStatusAndEffectiveDate(EmployeeEventStatus status, string effectiveDate, bool expected)
    {
        var employeeEvent = Event(status, version: 2, effectiveDate: DateOnly.Parse(effectiveDate, System.Globalization.CultureInfo.InvariantCulture));

        Assert.Equal(expected, employeeEvent.IsDue(Today));
    }

    [Fact]
    public void ApplyTo_DueEvent_ChangesMasterDataAndMarksApplied()
    {
        var approved = Event(EmployeeEventStatus.Approved, version: 3, afterData: new JsonObject
        {
            ["departmentId"] = 20,
            ["positionId"] = 30,
            ["managerId"] = 99,
            ["status"] = "active",
            ["salary"] = 1234
        });
        var current = MasterData(departmentId: 10, positionId: 11, managerId: 12, status: "probation", workEmail: "a@qlns.example", version: 5);

        var updated = approved.ApplyTo(current, Today, Now);

        Assert.Equal(20, updated.DepartmentId);
        Assert.Equal(30, updated.PositionId);
        Assert.Equal(99, updated.ManagerId);
        Assert.Equal("active", updated.Status);
        Assert.Equal(6, updated.Version);
        Assert.Equal(EmployeeEventStatus.Applied, approved.Status);
        Assert.Equal(Now, approved.AppliedAt);
        Assert.Equal(4, approved.Version);
        Assert.Equal(5, current.Version);
    }

    [Fact]
    public void ApplyTo_NullManagerId_ClearsManagerAndKeepsUntouchedFields()
    {
        var approved = Event(EmployeeEventStatus.Approved, version: 3, afterData: new JsonObject { ["managerId"] = null });
        var current = MasterData(departmentId: 10, positionId: 11, managerId: 12, status: "active", workEmail: "a@qlns.example", version: 1);

        var updated = approved.ApplyTo(current, Today, Now);

        Assert.Null(updated.ManagerId);
        Assert.Equal(10, updated.DepartmentId);
        Assert.Equal(11, updated.PositionId);
        Assert.Equal("active", updated.Status);
    }

    [Fact]
    public void ApplyTo_NotDue_ThrowsNotDue()
    {
        var future = Event(EmployeeEventStatus.Approved, version: 3, effectiveDate: Today.AddDays(1));

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => future.ApplyTo(MasterData(), Today, Now));

        Assert.Equal("corehr.event.not_due", exception.Code);
        Assert.Equal(EmployeeEventStatus.Approved, future.Status);
    }

    [Fact]
    public void ApplyTo_SelfManager_ThrowsSelfManager()
    {
        var approved = Event(EmployeeEventStatus.Approved, version: 3, afterData: new JsonObject { ["managerId"] = 42 });

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => approved.ApplyTo(MasterData(), Today, Now));

        Assert.Equal("corehr.event.self_manager", exception.Code);
        Assert.Equal(EmployeeEventStatus.Approved, approved.Status);
    }

    [Fact]
    public void ApplyTo_ActiveWithoutWorkEmail_ThrowsActiveRequiresWorkEmail()
    {
        var approved = Event(EmployeeEventStatus.Approved, version: 3, afterData: new JsonObject { ["status"] = "active" });
        var current = MasterData(status: "probation", workEmail: null);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => approved.ApplyTo(current, Today, Now));

        Assert.Equal("corehr.event.active_requires_work_email", exception.Code);
    }

    [Fact]
    public void Constructor_NonDraftWithIdZero_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new EmployeeEvent(
            0, 42, EmployeeEventType.Transfer, EmployeeEventStatus.Approved, Today,
            new JsonObject(), new JsonObject { ["departmentId"] = 1 }, "r", null, 5, null, null, null, 1, Now, Now));
    }

    private static EmployeeEventWrite Write(
        string? eventType = "transfer",
        DateOnly? effectiveDate = null,
        JsonObject? beforeData = null,
        JsonObject? afterData = null,
        string? reason = "Move to product",
        long? compensatesEventId = null) => new(
            eventType,
            effectiveDate ?? new DateOnly(2026, 10, 1),
            beforeData ?? new JsonObject { ["departmentId"] = 1 },
            afterData ?? new JsonObject { ["departmentId"] = 2 },
            reason,
            compensatesEventId);

    private static EmployeeEvent Draft() => EmployeeEvent.CreateDraft(42, Write(), 5, Now);

    private static EmployeeEvent Event(
        EmployeeEventStatus status,
        long version,
        DateOnly? effectiveDate = null,
        JsonObject? afterData = null) => new(
            id: 100,
            employeeId: 42,
            EmployeeEventType.Transfer,
            status,
            effectiveDate ?? Today,
            new JsonObject { ["departmentId"] = 10 },
            afterData ?? new JsonObject { ["departmentId"] = 20 },
            "Move to product",
            compensatesEventId: null,
            createdBy: 5,
            approvedBy: status is EmployeeEventStatus.Approved or EmployeeEventStatus.Applied ? 9 : null,
            approvedAt: status is EmployeeEventStatus.Approved or EmployeeEventStatus.Applied ? Now.AddDays(-1) : null,
            appliedAt: status == EmployeeEventStatus.Applied ? Now.AddHours(-1) : null,
            version,
            createdAt: Now.AddDays(-2),
            updatedAt: Now.AddDays(-1));

    private static EmployeeMasterData MasterData(
        long departmentId = 10,
        long positionId = 11,
        long? managerId = 12,
        string status = "active",
        string? workEmail = "a@qlns.example",
        long version = 1) =>
        new(42, departmentId, positionId, managerId, status, workEmail, version);
}
