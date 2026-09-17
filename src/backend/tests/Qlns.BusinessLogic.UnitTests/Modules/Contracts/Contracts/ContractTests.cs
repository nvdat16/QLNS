using Qlns.BusinessLogic.Modules.Contracts.Contracts;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Xunit;
using static Qlns.BusinessLogic.UnitTests.Modules.Contracts.Contracts.ContractTestData;

namespace Qlns.BusinessLogic.UnitTests.Modules.Contracts.Contracts;

public sealed class ContractTests
{
    [Fact]
    public void CreateDraft_Valid_BuildsUnsavedDraftWithDefaults()
    {
        var contract = Contract.CreateDraft(Write(contractNumber: "  HD-2026-001 ", currency: null), Now);

        Assert.Equal(0, contract.Id);
        Assert.Equal(ContractStatus.Draft, contract.Status);
        Assert.Equal(1, contract.Version);
        Assert.Equal("HD-2026-001", contract.ContractNumber);
        Assert.Equal(ContractType.FixedTerm, contract.ContractType);
        Assert.Equal(Contract.DefaultCurrency, contract.Currency);
        Assert.True(contract.IsPrimary);
        Assert.False(contract.SignedDocumentAvailable);
        Assert.Null(contract.SignedAt);
        Assert.Equal(Now, contract.CreatedAt);
        Assert.Equal(Now, contract.UpdatedAt);
    }

    [Fact]
    public void CreateDraft_IndefiniteWithoutEndDate_IsValid()
    {
        var contract = Contract.CreateDraft(Write(contractType: "indefinite", noEndDate: true), Now);

        Assert.Equal(ContractType.Indefinite, contract.ContractType);
        Assert.Null(contract.EndDate);
        Assert.Null(contract.DaysRemaining(Today));
    }

    [Fact]
    public void CreateDraft_ProbationOfExactlySixtyDays_IsValid()
    {
        var start = new DateOnly(2026, 10, 1);
        var contract = Contract.CreateDraft(Write(contractType: "probation", startDate: start, endDate: start.AddDays(60)), Now);

        Assert.Equal(ContractType.Probation, contract.ContractType);
    }

    public static TheoryData<ContractWrite, string> InvalidWrites => new()
    {
        { Write(employeeId: 0), "employeeId" },
        { Write(contractNumber: "  "), "contractNumber" },
        { Write(contractNumber: new string('x', 101)), "contractNumber" },
        { Write(contractType: "permanent"), "contractType" },
        { Write(contractType: "PROBATION"), "contractType" },
        { Write(startDate: default(DateOnly)), "startDate" },
        { Write(contractType: "fixed_term", noEndDate: true), "endDate" },
        { Write(contractType: "fixed_term", startDate: new DateOnly(2026, 10, 1), endDate: new DateOnly(2026, 10, 1)), "endDate" },
        { Write(contractType: "fixed_term", startDate: new DateOnly(2026, 10, 1), endDate: new DateOnly(2026, 9, 30)), "endDate" },
        { Write(contractType: "internship", noEndDate: true), "endDate" },
        { Write(contractType: "service_contract", noEndDate: true), "endDate" },
        { Write(contractType: "indefinite", endDate: new DateOnly(2027, 1, 1)), "endDate" },
        { Write(contractType: "probation", startDate: new DateOnly(2026, 10, 1), endDate: new DateOnly(2026, 12, 1)), "endDate" },
        { Write(salary: -1), "salary" },
        { Write(currency: "vnd"), "currency" },
        { Write(currency: "VN"), "currency" },
        { Write(currency: "US$"), "currency" },
        { Write(noticePeriodDays: -1), "noticePeriodDays" }
    };

    [Theory]
    [MemberData(nameof(InvalidWrites))]
    public void CreateDraft_InvalidField_ThrowsValidationOnThatField(ContractWrite write, string field)
    {
        var exception = Assert.Throws<CoreHrValidationException>(() => Contract.CreateDraft(write, Now));

        Assert.Contains(field, exception.Errors.Keys);
    }

    [Fact]
    public void CreateDraft_SeveralInvalidFields_ReportsAllOfThem()
    {
        var write = Write(employeeId: -1, contractNumber: "", contractType: "fixed_term", noEndDate: true, salary: -5, currency: "x");

        var exception = Assert.Throws<CoreHrValidationException>(() => Contract.CreateDraft(write, Now));

        Assert.Equal(["contractNumber", "currency", "employeeId", "endDate", "salary"], exception.Errors.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Replace_Draft_AppliesValuesAndReportsChangedFields()
    {
        var contract = Build(version: 2);

        var changed = contract.Replace(
            Write(contractNumber: "HD-2026-002", salary: 30_000_000m, noticePeriodDays: 45, isPrimary: false),
            Now);

        Assert.Equal(["contractNumber", "salary", "noticePeriodDays", "isPrimary"], changed);
        Assert.Equal("HD-2026-002", contract.ContractNumber);
        Assert.Equal(30_000_000m, contract.Salary);
        Assert.Equal(45, contract.NoticePeriodDays);
        Assert.False(contract.IsPrimary);
        Assert.Equal(3, contract.Version);
        Assert.Equal(Now, contract.UpdatedAt);
    }

    [Fact]
    public void Replace_SameValues_ReportsNoChangesButStillBumpsVersion()
    {
        var contract = Build(version: 1);

        var changed = contract.Replace(Write(), Now);

        Assert.Empty(changed);
        Assert.Equal(2, contract.Version);
    }

    [Fact]
    public void Replace_ChangingEmployee_ThrowsValidationOnEmployeeId()
    {
        var contract = Build();

        var exception = Assert.Throws<CoreHrValidationException>(() => contract.Replace(Write(employeeId: 11), Now));

        Assert.Contains("employeeId", exception.Errors.Keys);
        Assert.Equal(1, contract.Version);
    }

    [Fact]
    public void Replace_InvalidTerms_ThrowsValidationWithoutMutating()
    {
        var contract = Build();

        Assert.Throws<CoreHrValidationException>(() => contract.Replace(Write(contractType: "indefinite"), Now));

        Assert.Equal(ContractType.FixedTerm, contract.ContractType);
        Assert.Equal(1, contract.Version);
    }

    [Theory]
    [InlineData(ContractStatus.Approved)]
    [InlineData(ContractStatus.Executed)]
    [InlineData(ContractStatus.Active)]
    [InlineData(ContractStatus.Expired)]
    [InlineData(ContractStatus.Terminated)]
    [InlineData(ContractStatus.Cancelled)]
    public void Replace_NonDraft_ThrowsNotEditable(ContractStatus status)
    {
        var contract = Build(status: status);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => contract.Replace(Write(), Now));

        Assert.Equal(Contract.NotEditableCode, exception.Code);
    }

    [Fact]
    public void Approve_FromDraft_MovesToApproved()
    {
        var contract = Build(version: 3);

        contract.Approve(Now);

        Assert.Equal(ContractStatus.Approved, contract.Status);
        Assert.Equal(4, contract.Version);
        Assert.Equal(Now, contract.UpdatedAt);
    }

    [Theory]
    [InlineData(ContractStatus.Approved)]
    [InlineData(ContractStatus.Executed)]
    [InlineData(ContractStatus.Active)]
    [InlineData(ContractStatus.Cancelled)]
    public void Approve_FromNonDraft_ThrowsInvalidTransition(ContractStatus status)
    {
        var contract = Build(status: status);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => contract.Approve(Now));

        Assert.Equal(Contract.InvalidTransitionCode, exception.Code);
        Assert.Equal(status.ToContract(), exception.Details["currentStatus"]);
        Assert.Equal("approve", exception.Details["action"]);
        Assert.Equal(1, contract.Version);
    }

    [Fact]
    public void AttachSignedDocument_Approved_NoOtherPrimary_BecomesExecutedAndRecordsSigning()
    {
        var contract = Build(status: ContractStatus.Approved, version: 2);

        contract.AttachSignedDocument("contracts/42/signed/abc", otherPrimaryInForce: false, Now);

        Assert.Equal(ContractStatus.Executed, contract.Status);
        Assert.Equal("contracts/42/signed/abc", contract.DocumentObjectKey);
        Assert.True(contract.SignedDocumentAvailable);
        Assert.Equal(Now, contract.SignedAt);
        Assert.Equal(3, contract.Version);
    }

    [Fact]
    public void AttachSignedDocument_ApprovedPrimary_WhileOtherPrimaryInForce_KeepsApprovedWithDocument()
    {
        var contract = Build(status: ContractStatus.Approved);

        contract.AttachSignedDocument("key", otherPrimaryInForce: true, Now);

        Assert.Equal(ContractStatus.Approved, contract.Status);
        Assert.True(contract.SignedDocumentAvailable);
        Assert.Equal(Now, contract.SignedAt);
        Assert.Equal(2, contract.Version);
    }

    [Fact]
    public void AttachSignedDocument_ApprovedSecondary_WhileOtherPrimaryInForce_BecomesExecuted()
    {
        var contract = Build(status: ContractStatus.Approved, isPrimary: false);

        contract.AttachSignedDocument("key", otherPrimaryInForce: true, Now);

        Assert.Equal(ContractStatus.Executed, contract.Status);
    }

    [Fact]
    public void AttachSignedDocument_Executed_ReplacesDocumentAndKeepsOriginalSigningTime()
    {
        var signedAt = Now.AddDays(-3);
        var contract = Build(status: ContractStatus.Executed, documentObjectKey: "old", signedAt: signedAt);

        contract.AttachSignedDocument("new", otherPrimaryInForce: false, Now);

        Assert.Equal(ContractStatus.Executed, contract.Status);
        Assert.Equal("new", contract.DocumentObjectKey);
        Assert.Equal(signedAt, contract.SignedAt);
    }

    [Theory]
    [InlineData(ContractStatus.Draft)]
    [InlineData(ContractStatus.Active)]
    [InlineData(ContractStatus.Expired)]
    [InlineData(ContractStatus.Terminated)]
    [InlineData(ContractStatus.Cancelled)]
    public void AttachSignedDocument_OutsideApprovedOrExecuted_ThrowsSignedDocumentNotAllowed(ContractStatus status)
    {
        var contract = Build(status: status);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => contract.AttachSignedDocument("key", false, Now));

        Assert.Equal(Contract.SignedDocumentNotAllowedCode, exception.Code);
        Assert.Null(contract.DocumentObjectKey);
    }

    [Theory]
    [InlineData(ContractStatus.Approved)]
    [InlineData(ContractStatus.Executed)]
    public void Activate_WithStoredDocument_BecomesActive(ContractStatus status)
    {
        var signedAt = Now.AddDays(-1);
        var contract = Build(status: status, documentObjectKey: "key", signedAt: signedAt, version: 4);

        contract.Activate(signedAt: null, Now);

        Assert.Equal(ContractStatus.Active, contract.Status);
        Assert.Equal(signedAt, contract.SignedAt);
        Assert.Equal(5, contract.Version);
    }

    [Fact]
    public void Activate_WithExplicitSignedAt_RecordsItAsSigningTime()
    {
        var explicitSignedAt = Now.AddHours(-2);
        var contract = Build(status: ContractStatus.Approved);

        contract.Activate(explicitSignedAt, Now);

        Assert.Equal(ContractStatus.Active, contract.Status);
        Assert.Equal(explicitSignedAt, contract.SignedAt);
        Assert.False(contract.SignedDocumentAvailable);
    }

    [Fact]
    public void Activate_WithoutAnyEvidence_ThrowsSignatureRequired()
    {
        var contract = Build(status: ContractStatus.Approved);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => contract.Activate(null, Now));

        Assert.Equal(Contract.SignatureRequiredCode, exception.Code);
        Assert.Equal(ContractStatus.Approved, contract.Status);
    }

    [Theory]
    [InlineData(ContractStatus.Draft)]
    [InlineData(ContractStatus.Active)]
    [InlineData(ContractStatus.Terminated)]
    public void Activate_FromWrongStatus_ThrowsInvalidTransitionEvenWithEvidence(ContractStatus status)
    {
        var contract = Build(status: status, documentObjectKey: "key");

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => contract.Activate(Now, Now));

        Assert.Equal(Contract.InvalidTransitionCode, exception.Code);
    }

    [Theory]
    [InlineData(ContractStatus.Active)]
    [InlineData(ContractStatus.Executed)]
    public void Terminate_InForce_WithReason_BecomesTerminated(ContractStatus status)
    {
        var contract = Build(status: status);

        contract.Terminate("Mutual agreement", Now);

        Assert.Equal(ContractStatus.Terminated, contract.Status);
        Assert.Equal(2, contract.Version);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Terminate_WithoutReason_ThrowsValidationOnReason(string? reason)
    {
        var contract = Build(status: ContractStatus.Active);

        var exception = Assert.Throws<CoreHrValidationException>(() => contract.Terminate(reason, Now));

        Assert.Contains("reason", exception.Errors.Keys);
        Assert.Equal(ContractStatus.Active, contract.Status);
    }

    [Theory]
    [InlineData(ContractStatus.Draft)]
    [InlineData(ContractStatus.Approved)]
    [InlineData(ContractStatus.Expired)]
    [InlineData(ContractStatus.Cancelled)]
    public void Terminate_NotInForce_ThrowsInvalidTransition(ContractStatus status)
    {
        var contract = Build(status: status);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => contract.Terminate("reason", Now));

        Assert.Equal(Contract.InvalidTransitionCode, exception.Code);
    }

    [Theory]
    [InlineData(ContractStatus.Draft)]
    [InlineData(ContractStatus.Approved)]
    public void Cancel_DraftOrApproved_WithReason_BecomesCancelled(ContractStatus status)
    {
        var contract = Build(status: status);

        contract.Cancel("Wrong employee", Now);

        Assert.Equal(ContractStatus.Cancelled, contract.Status);
    }

    [Fact]
    public void Cancel_WithoutReason_ThrowsValidation()
    {
        var contract = Build();

        Assert.Throws<CoreHrValidationException>(() => contract.Cancel(" ", Now));
    }

    [Theory]
    [InlineData(ContractStatus.Executed)]
    [InlineData(ContractStatus.Active)]
    [InlineData(ContractStatus.Terminated)]
    public void Cancel_AfterExecution_ThrowsInvalidTransition(ContractStatus status)
    {
        var contract = Build(status: status);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => contract.Cancel("reason", Now));

        Assert.Equal(Contract.InvalidTransitionCode, exception.Code);
    }

    [Theory]
    [InlineData(ContractStatus.Active)]
    [InlineData(ContractStatus.Executed)]
    public void Expire_InForceAndEnded_BecomesExpired(ContractStatus status)
    {
        var contract = Build(status: status, endDate: new DateOnly(2026, 9, 16));

        contract.Expire(Today, Now);

        Assert.Equal(ContractStatus.Expired, contract.Status);
        Assert.Equal(2, contract.Version);
    }

    [Fact]
    public void Expire_EndingOnOrAfterAsOf_ThrowsInvalidTransition()
    {
        var contract = Build(status: ContractStatus.Active, endDate: Today);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => contract.Expire(Today, Now));

        Assert.Equal(Contract.InvalidTransitionCode, exception.Code);
        Assert.Equal(ContractStatus.Active, contract.Status);
    }

    [Fact]
    public void Expire_Indefinite_ThrowsInvalidTransition()
    {
        var contract = Build(status: ContractStatus.Active, type: ContractType.Indefinite, indefinite: true);

        Assert.Throws<CoreHrBusinessRuleException>(() => contract.Expire(Today, Now));
    }

    [Theory]
    [InlineData(ContractStatus.Draft)]
    [InlineData(ContractStatus.Approved)]
    [InlineData(ContractStatus.Expired)]
    public void Expire_NotInForce_ThrowsInvalidTransition(ContractStatus status)
    {
        var contract = Build(status: status, endDate: new DateOnly(2026, 1, 1));

        Assert.Throws<CoreHrBusinessRuleException>(() => contract.Expire(Today, Now));
    }

    [Fact]
    public void DaysRemainingAndEndsBefore_CountFromTheGivenDate()
    {
        var contract = Build(endDate: new DateOnly(2026, 9, 27));

        Assert.Equal(10, contract.DaysRemaining(Today));
        Assert.Equal(-1, contract.DaysRemaining(new DateOnly(2026, 9, 28)));
        Assert.True(contract.EndsBefore(new DateOnly(2026, 9, 28)));
        Assert.False(contract.EndsBefore(new DateOnly(2026, 9, 27)));
    }

    [Fact]
    public void Constructor_GuardsIdentifiersAndVersion()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(id: 0, status: ContractStatus.Approved));
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(id: -1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(employeeId: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(version: 0));
        Assert.Throws<ArgumentException>(() => Build(contractNumber: " "));
    }
}
