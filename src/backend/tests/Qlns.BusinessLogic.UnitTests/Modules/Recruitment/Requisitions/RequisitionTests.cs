using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Requisitions;
using Xunit;

namespace Qlns.BusinessLogic.UnitTests.Modules.Recruitment.Requisitions;

public sealed class RequisitionTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 17, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 9, 17);

    [Fact]
    public void CreateDraft_Valid_TrimsTextAndStartsAsDraftVersionOne()
    {
        var write = ValidWrite() with
        {
            Title = "  Senior .NET Engineer  ",
            Description = "  Build APIs ",
            Requirements = "   ",
            Location = " Hanoi ",
            SalaryMin = 1000m,
            SalaryMax = 2000m,
            ClosingDate = Today.AddDays(30)
        };

        var draft = Requisition.CreateDraft(write, createdBy: 7, Today, Now);

        Assert.Equal(0, draft.Id);
        Assert.Equal(string.Empty, draft.JobCode);
        Assert.Equal("Senior .NET Engineer", draft.Title);
        Assert.Equal("Build APIs", draft.Description);
        Assert.Null(draft.Requirements);
        Assert.Equal("Hanoi", draft.Location);
        Assert.Equal(EmploymentType.FullTime, draft.EmploymentType);
        Assert.Equal(1000m, draft.SalaryMin);
        Assert.Equal(2000m, draft.SalaryMax);
        Assert.Equal(2, draft.TargetHeadcount);
        Assert.Equal(RequisitionStatus.Draft, draft.Status);
        Assert.Equal(Today.AddDays(30), draft.ClosingDate);
        Assert.Null(draft.PublishedAt);
        Assert.Equal(7, draft.CreatedBy);
        Assert.Equal(1, draft.Version);
        Assert.Equal(Now, draft.CreatedAt);
        Assert.Equal(Now, draft.UpdatedAt);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateDraft_BlankTitle_ThrowsValidationOnTitle(string? title)
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Requisition.CreateDraft(ValidWrite() with { Title = title }, 7, Today, Now));

        Assert.Equal(["title"], exception.Errors.Keys);
    }

    [Fact]
    public void CreateDraft_TitleTooLong_ThrowsValidationOnTitle()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Requisition.CreateDraft(ValidWrite() with { Title = new string('a', 256) }, 7, Today, Now));

        Assert.True(exception.Errors.ContainsKey("title"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void CreateDraft_NonPositiveDepartment_ThrowsValidationOnDepartmentId(long departmentId)
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Requisition.CreateDraft(ValidWrite() with { DepartmentId = departmentId }, 7, Today, Now));

        Assert.Equal(["departmentId"], exception.Errors.Keys);
    }

    [Fact]
    public void CreateDraft_NonPositivePosition_ThrowsValidationOnPositionId()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Requisition.CreateDraft(ValidWrite() with { PositionId = 0 }, 7, Today, Now));

        Assert.Equal(["positionId"], exception.Errors.Keys);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("contract")]
    [InlineData("Full_Time")]
    public void CreateDraft_UnknownEmploymentType_ThrowsValidationOnEmploymentType(string? employmentType)
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Requisition.CreateDraft(ValidWrite() with { EmploymentType = employmentType }, 7, Today, Now));

        Assert.Equal(["employmentType"], exception.Errors.Keys);
        Assert.Contains("service_contract", exception.Errors["employmentType"][0], StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("full_time", EmploymentType.FullTime)]
    [InlineData("part_time", EmploymentType.PartTime)]
    [InlineData("hybrid", EmploymentType.Hybrid)]
    [InlineData("remote", EmploymentType.Remote)]
    [InlineData("internship", EmploymentType.Internship)]
    [InlineData("service_contract", EmploymentType.ServiceContract)]
    public void CreateDraft_EveryContractEmploymentType_IsAccepted(string value, EmploymentType expected)
    {
        var draft = Requisition.CreateDraft(ValidWrite() with { EmploymentType = value }, 7, Today, Now);

        Assert.Equal(expected, draft.EmploymentType);
        Assert.Equal(value, draft.EmploymentType.ToContract());
    }

    [Fact]
    public void CreateDraft_NegativeSalaries_ReportsBothFields()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Requisition.CreateDraft(ValidWrite() with { SalaryMin = -1m, SalaryMax = -2m }, 7, Today, Now));

        Assert.True(exception.Errors.ContainsKey("salaryMin"));
        Assert.True(exception.Errors.ContainsKey("salaryMax"));
    }

    [Fact]
    public void CreateDraft_SalaryMinAboveMax_ThrowsValidationOnSalaryMin()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Requisition.CreateDraft(ValidWrite() with { SalaryMin = 3000m, SalaryMax = 2000m }, 7, Today, Now));

        Assert.Equal(["salaryMin"], exception.Errors.Keys);
    }

    [Fact]
    public void CreateDraft_OnlyOneSalaryBound_IsAccepted()
    {
        var draft = Requisition.CreateDraft(ValidWrite() with { SalaryMin = null, SalaryMax = 2000m }, 7, Today, Now);

        Assert.Null(draft.SalaryMin);
        Assert.Equal(2000m, draft.SalaryMax);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void CreateDraft_HeadcountBelowOne_ThrowsValidationOnTargetHeadcount(int headcount)
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Requisition.CreateDraft(ValidWrite() with { TargetHeadcount = headcount }, 7, Today, Now));

        Assert.Equal(["targetHeadcount"], exception.Errors.Keys);
    }

    [Fact]
    public void CreateDraft_ClosingDateInPast_ThrowsValidationOnClosingDate()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Requisition.CreateDraft(ValidWrite() with { ClosingDate = Today.AddDays(-1) }, 7, Today, Now));

        Assert.Equal(["closingDate"], exception.Errors.Keys);
    }

    [Fact]
    public void CreateDraft_ClosingDateToday_IsAccepted()
    {
        var draft = Requisition.CreateDraft(ValidWrite() with { ClosingDate = Today }, 7, Today, Now);

        Assert.Equal(Today, draft.ClosingDate);
    }

    [Fact]
    public void CreateDraft_ManyInvalidFields_ReportsAllOfThem()
    {
        var write = new RequisitionWrite(
            Title: "",
            DepartmentId: 0,
            PositionId: -1,
            Description: new string('d', 20001),
            Requirements: new string('r', 20001),
            Location: new string('l', 256),
            EmploymentType: "nope",
            SalaryMin: -1m,
            SalaryMax: null,
            TargetHeadcount: 0,
            ClosingDate: Today.AddDays(-2));

        var exception = Assert.Throws<CoreHrValidationException>(() => Requisition.CreateDraft(write, 7, Today, Now));

        Assert.Equal(
            ["closingDate", "departmentId", "description", "employmentType", "location", "positionId", "requirements", "salaryMin", "targetHeadcount", "title"],
            exception.Errors.Keys.Order(StringComparer.Ordinal));
    }

    [Theory]
    [InlineData(2026, 1, "REQ-2026-00001")]
    [InlineData(2026, 12345, "REQ-2026-12345")]
    [InlineData(2027, 123456, "REQ-2027-123456")]
    public void BuildJobCode_FormatsYearAndZeroPaddedId(int year, long id, string expected)
    {
        Assert.Equal(expected, Requisition.BuildJobCode(year, id));
    }

    [Fact]
    public void Replace_Draft_AppliesValuesReportsChangedFieldsAndBumpsVersion()
    {
        var requisition = Create(RequisitionStatus.Draft, version: 2);
        var write = ValidWrite() with
        {
            Title = "Staff Engineer",
            PositionId = 11,
            Location = "Da Nang",
            EmploymentType = "remote",
            SalaryMax = 5000m,
            TargetHeadcount = 3,
            ClosingDate = Today.AddDays(10)
        };

        var changed = requisition.Replace(write, Today, Now);

        Assert.Equal(["title", "positionId", "location", "employmentType", "salaryMax", "targetHeadcount", "closingDate"], changed);
        Assert.Equal("Staff Engineer", requisition.Title);
        Assert.Equal(11, requisition.PositionId);
        Assert.Equal(EmploymentType.Remote, requisition.EmploymentType);
        Assert.Equal(5000m, requisition.SalaryMax);
        Assert.Equal(3, requisition.TargetHeadcount);
        Assert.Equal(3, requisition.Version);
        Assert.Equal(Now, requisition.UpdatedAt);
        Assert.Equal(RequisitionStatus.Draft, requisition.Status);
    }

    [Fact]
    public void Replace_SameValues_ReportsNoFieldsButStillBumpsVersion()
    {
        var requisition = Create(RequisitionStatus.Draft, version: 1);

        var changed = requisition.Replace(ValidWrite(), Today, Now);

        Assert.Empty(changed);
        Assert.Equal(2, requisition.Version);
    }

    [Theory]
    [InlineData(RequisitionStatus.PendingApproval)]
    [InlineData(RequisitionStatus.Approved)]
    [InlineData(RequisitionStatus.ActiveRecruiting)]
    [InlineData(RequisitionStatus.Closed)]
    [InlineData(RequisitionStatus.Cancelled)]
    public void Replace_NonDraft_ThrowsNotEditableWithoutChanging(RequisitionStatus status)
    {
        var requisition = Create(status, version: 1);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() =>
            requisition.Replace(ValidWrite() with { Title = "Other" }, Today, Now));

        Assert.Equal(Requisition.NotEditableCode, exception.Code);
        Assert.Equal(status.ToContract(), exception.Details["currentStatus"]);
        Assert.Equal("Senior Engineer", requisition.Title);
        Assert.Equal(1, requisition.Version);
    }

    [Fact]
    public void Replace_InvalidWrite_ThrowsValidationAndLeavesRequisitionUnchanged()
    {
        var requisition = Create(RequisitionStatus.Draft, version: 1);

        var exception = Assert.Throws<CoreHrValidationException>(() =>
            requisition.Replace(ValidWrite() with { Title = "Other", TargetHeadcount = 0 }, Today, Now));

        Assert.Equal(["targetHeadcount"], exception.Errors.Keys);
        Assert.Equal("Senior Engineer", requisition.Title);
        Assert.Equal(1, requisition.Version);
    }

    [Fact]
    public void Submit_FromDraft_MovesToPendingApproval()
    {
        var requisition = Create(RequisitionStatus.Draft, version: 3);

        requisition.Submit(Now);

        Assert.Equal(RequisitionStatus.PendingApproval, requisition.Status);
        Assert.Equal(4, requisition.Version);
        Assert.Equal(Now, requisition.UpdatedAt);
    }

    [Theory]
    [InlineData(RequisitionStatus.PendingApproval)]
    [InlineData(RequisitionStatus.Approved)]
    [InlineData(RequisitionStatus.ActiveRecruiting)]
    [InlineData(RequisitionStatus.Closed)]
    [InlineData(RequisitionStatus.Cancelled)]
    public void Submit_FromNonDraft_ThrowsInvalidTransition(RequisitionStatus status)
    {
        var requisition = Create(status, version: 1);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => requisition.Submit(Now));

        Assert.Equal(Requisition.InvalidTransitionCode, exception.Code);
        Assert.Equal(status.ToContract(), exception.Details["currentStatus"]);
        Assert.Equal("submit", exception.Details["action"]);
        Assert.Equal(status, requisition.Status);
        Assert.Equal(1, requisition.Version);
    }

    [Fact]
    public void Approve_FromPendingApproval_MovesToApproved()
    {
        var requisition = Create(RequisitionStatus.PendingApproval, version: 2);

        requisition.Approve(Now);

        Assert.Equal(RequisitionStatus.Approved, requisition.Status);
        Assert.Equal(3, requisition.Version);
    }

    [Theory]
    [InlineData(RequisitionStatus.Draft)]
    [InlineData(RequisitionStatus.Approved)]
    [InlineData(RequisitionStatus.ActiveRecruiting)]
    public void Approve_FromNonPending_ThrowsInvalidTransition(RequisitionStatus status)
    {
        var requisition = Create(status, version: 1);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => requisition.Approve(Now));

        Assert.Equal(Requisition.InvalidTransitionCode, exception.Code);
    }

    [Fact]
    public void Reject_WithReason_ReturnsToDraft()
    {
        var requisition = Create(RequisitionStatus.PendingApproval, version: 2);

        requisition.Reject("Budget frozen", Now);

        Assert.Equal(RequisitionStatus.Draft, requisition.Status);
        Assert.Equal(3, requisition.Version);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Reject_WithoutReason_ThrowsValidationOnReason(string? reason)
    {
        var requisition = Create(RequisitionStatus.PendingApproval, version: 2);

        var exception = Assert.Throws<CoreHrValidationException>(() => requisition.Reject(reason, Now));

        Assert.Equal(["reason"], exception.Errors.Keys);
        Assert.Equal(RequisitionStatus.PendingApproval, requisition.Status);
        Assert.Equal(2, requisition.Version);
    }

    [Fact]
    public void Reject_ReasonTooLong_ThrowsValidationOnReason()
    {
        var requisition = Create(RequisitionStatus.PendingApproval, version: 2);

        var exception = Assert.Throws<CoreHrValidationException>(() => requisition.Reject(new string('x', 1001), Now));

        Assert.Equal(["reason"], exception.Errors.Keys);
    }

    [Theory]
    [InlineData(RequisitionStatus.Draft)]
    [InlineData(RequisitionStatus.Approved)]
    [InlineData(RequisitionStatus.Cancelled)]
    public void Reject_FromNonPending_ThrowsInvalidTransition(RequisitionStatus status)
    {
        var requisition = Create(status, version: 1);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => requisition.Reject("reason", Now));

        Assert.Equal(Requisition.InvalidTransitionCode, exception.Code);
        Assert.Equal("reject", exception.Details["action"]);
    }

    [Fact]
    public void Publish_FromApprovedWithoutClosingDate_ActivatesAndRecordsPublishedAt()
    {
        var requisition = Create(RequisitionStatus.Approved, version: 3);

        requisition.Publish(Today, Now);

        Assert.Equal(RequisitionStatus.ActiveRecruiting, requisition.Status);
        Assert.Equal(Now, requisition.PublishedAt);
        Assert.Equal(4, requisition.Version);
    }

    [Fact]
    public void Publish_WithFutureClosingDate_IsAccepted()
    {
        var requisition = Create(RequisitionStatus.Approved, version: 3, closingDate: Today.AddDays(1));

        requisition.Publish(Today, Now);

        Assert.Equal(RequisitionStatus.ActiveRecruiting, requisition.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Publish_WithClosingDateTodayOrPast_ThrowsValidationOnClosingDate(int offsetDays)
    {
        var requisition = Create(RequisitionStatus.Approved, version: 3, closingDate: Today.AddDays(offsetDays));

        var exception = Assert.Throws<CoreHrValidationException>(() => requisition.Publish(Today, Now));

        Assert.Equal(["closingDate"], exception.Errors.Keys);
        Assert.Equal(RequisitionStatus.Approved, requisition.Status);
        Assert.Null(requisition.PublishedAt);
        Assert.Equal(3, requisition.Version);
    }

    [Theory]
    [InlineData(RequisitionStatus.Draft)]
    [InlineData(RequisitionStatus.PendingApproval)]
    [InlineData(RequisitionStatus.ActiveRecruiting)]
    [InlineData(RequisitionStatus.Closed)]
    public void Publish_FromNonApproved_ThrowsInvalidTransition(RequisitionStatus status)
    {
        var requisition = Create(status, version: 1);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => requisition.Publish(Today, Now));

        Assert.Equal(Requisition.InvalidTransitionCode, exception.Code);
        Assert.Equal("publish", exception.Details["action"]);
    }

    [Fact]
    public void Close_FromActiveRecruiting_MovesToClosed()
    {
        var requisition = Create(RequisitionStatus.ActiveRecruiting, version: 5, publishedAt: Now.AddDays(-3));

        requisition.Close(Now);

        Assert.Equal(RequisitionStatus.Closed, requisition.Status);
        Assert.Equal(Now.AddDays(-3), requisition.PublishedAt);
        Assert.Equal(6, requisition.Version);
    }

    [Theory]
    [InlineData(RequisitionStatus.Draft)]
    [InlineData(RequisitionStatus.Approved)]
    [InlineData(RequisitionStatus.Closed)]
    [InlineData(RequisitionStatus.Cancelled)]
    public void Close_FromNonActive_ThrowsInvalidTransition(RequisitionStatus status)
    {
        var requisition = Create(status, version: 1);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => requisition.Close(Now));

        Assert.Equal(Requisition.InvalidTransitionCode, exception.Code);
    }

    [Theory]
    [InlineData(RequisitionStatus.Draft)]
    [InlineData(RequisitionStatus.PendingApproval)]
    [InlineData(RequisitionStatus.Approved)]
    [InlineData(RequisitionStatus.ActiveRecruiting)]
    public void Cancel_FromOpenStatus_MovesToCancelled(RequisitionStatus status)
    {
        var requisition = Create(status, version: 1);

        requisition.Cancel("Role no longer needed", Now);

        Assert.Equal(RequisitionStatus.Cancelled, requisition.Status);
        Assert.Equal(2, requisition.Version);
    }

    [Theory]
    [InlineData(RequisitionStatus.Closed)]
    [InlineData(RequisitionStatus.Cancelled)]
    public void Cancel_FromTerminalStatus_ThrowsInvalidTransition(RequisitionStatus status)
    {
        var requisition = Create(status, version: 1);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => requisition.Cancel("reason", Now));

        Assert.Equal(Requisition.InvalidTransitionCode, exception.Code);
        Assert.Equal("cancel", exception.Details["action"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void Cancel_WithoutReason_ThrowsValidationOnReason(string? reason)
    {
        var requisition = Create(RequisitionStatus.Draft, version: 1);

        var exception = Assert.Throws<CoreHrValidationException>(() => requisition.Cancel(reason, Now));

        Assert.Equal(["reason"], exception.Errors.Keys);
        Assert.Equal(RequisitionStatus.Draft, requisition.Status);
    }

    [Theory]
    [InlineData(RequisitionAction.Submit, RequisitionStatus.Draft, RequisitionStatus.PendingApproval)]
    [InlineData(RequisitionAction.Approve, RequisitionStatus.PendingApproval, RequisitionStatus.Approved)]
    [InlineData(RequisitionAction.Reject, RequisitionStatus.PendingApproval, RequisitionStatus.Draft)]
    [InlineData(RequisitionAction.Publish, RequisitionStatus.Approved, RequisitionStatus.ActiveRecruiting)]
    [InlineData(RequisitionAction.Close, RequisitionStatus.ActiveRecruiting, RequisitionStatus.Closed)]
    [InlineData(RequisitionAction.Cancel, RequisitionStatus.Approved, RequisitionStatus.Cancelled)]
    public void Apply_RunsTheWorkflowAndReturnsPreviousStatus(RequisitionAction action, RequisitionStatus from, RequisitionStatus to)
    {
        var requisition = Create(from, version: 1);

        var previous = requisition.Apply(action, "because", Today, Now);

        Assert.Equal(from, previous);
        Assert.Equal(to, requisition.Status);
        Assert.Equal(2, requisition.Version);
    }

    [Theory]
    [InlineData(RequisitionAction.Submit, false)]
    [InlineData(RequisitionAction.Approve, false)]
    [InlineData(RequisitionAction.Reject, true)]
    [InlineData(RequisitionAction.Publish, false)]
    [InlineData(RequisitionAction.Close, false)]
    [InlineData(RequisitionAction.Cancel, true)]
    public void RecordsReason_OnlyForRejectAndCancel(RequisitionAction action, bool expected)
    {
        Assert.Equal(expected, Requisition.RecordsReason(action));
    }

    [Fact]
    public void Constructor_InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(id: -1, jobCode: "REQ-2026-00001"));
        Assert.Throws<ArgumentException>(() => Build(id: 1, jobCode: ""));
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(departmentId: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(positionId: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(targetHeadcount: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(version: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(createdBy: 0));
    }

    private static RequisitionWrite ValidWrite() => new(
        Title: "Senior Engineer",
        DepartmentId: 3,
        PositionId: 10,
        Description: "Build the platform",
        Requirements: "5 years of .NET",
        Location: "Hanoi",
        EmploymentType: "full_time",
        SalaryMin: 1000m,
        SalaryMax: 2000m,
        TargetHeadcount: 2,
        ClosingDate: null);

    private static Requisition Create(
        RequisitionStatus status,
        long version,
        DateOnly? closingDate = null,
        DateTimeOffset? publishedAt = null) =>
        Build(status: status, version: version, closingDate: closingDate, publishedAt: publishedAt);

    private static Requisition Build(
        long id = 42,
        string jobCode = "REQ-2026-00042",
        long departmentId = 3,
        long? positionId = 10,
        int targetHeadcount = 2,
        RequisitionStatus status = RequisitionStatus.Draft,
        DateOnly? closingDate = null,
        DateTimeOffset? publishedAt = null,
        long createdBy = 7,
        long version = 1) => new(
        id,
        jobCode,
        "Senior Engineer",
        departmentId,
        positionId,
        "Build the platform",
        "5 years of .NET",
        "Hanoi",
        EmploymentType.FullTime,
        1000m,
        2000m,
        targetHeadcount,
        status,
        closingDate,
        publishedAt,
        createdBy,
        version,
        Now.AddDays(-2),
        Now.AddMinutes(-5));
}
