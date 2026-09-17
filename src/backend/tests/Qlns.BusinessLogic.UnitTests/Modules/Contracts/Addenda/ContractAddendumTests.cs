using System.Text.Json.Nodes;
using Qlns.BusinessLogic.Modules.Contracts.Addenda;
using Qlns.BusinessLogic.Modules.Contracts.Contracts;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Xunit;
using static Qlns.BusinessLogic.UnitTests.Modules.Contracts.Addenda.AddendumTestData;
using Qlns.BusinessLogic.UnitTests.Modules.Contracts.Contracts;
using static Qlns.BusinessLogic.UnitTests.Modules.Contracts.Contracts.ContractTestData;

namespace Qlns.BusinessLogic.UnitTests.Modules.Contracts.Addenda;

public sealed class ContractAddendumTests
{
    private static readonly Contract ActiveContract = ContractTestData.Build(status: ContractStatus.Active, startDate: new DateOnly(2026, 10, 1));

    [Fact]
    public void CreateDraft_Valid_BuildsUnsavedDraftLinkedToContract()
    {
        var addendum = ContractAddendum.CreateDraft(ActiveContract, AddendumWrite(addendumNumber: " PL-1 ", reason: " Annual review "), HrUserId, Now);

        Assert.Equal(0, addendum.Id);
        Assert.Equal(ContractId, addendum.ContractId);
        Assert.Equal("PL-1", addendum.AddendumNumber);
        Assert.Equal("Annual review", addendum.Reason);
        Assert.Equal(ContractAddendumStatus.Draft, addendum.Status);
        Assert.Equal(1, addendum.Version);
        Assert.Equal(HrUserId, addendum.CreatedBy);
        Assert.Equal(["salary"], addendum.ChangedTerms);
        Assert.Null(addendum.ApprovedBy);
        Assert.False(addendum.SignedDocumentAvailable);
    }

    [Fact]
    public void CreateDraft_ClonesTermsSoLaterMutationOfTheRequestDoesNotLeak()
    {
        var after = Terms(("salary", 1));
        var addendum = ContractAddendum.CreateDraft(ActiveContract, AddendumWrite(afterTerms: after), HrUserId, Now);

        after["salary"] = 999;

        Assert.Equal(1, addendum.AfterTerms["salary"]!.GetValue<int>());
    }

    [Fact]
    public void CreateDraft_EffectiveOnContractStartDate_IsValid()
    {
        var addendum = ContractAddendum.CreateDraft(ActiveContract, AddendumWrite(effectiveDate: new DateOnly(2026, 10, 1)), HrUserId, Now);

        Assert.Equal(new DateOnly(2026, 10, 1), addendum.EffectiveDate);
    }

    [Fact]
    public void CreateDraft_AllAllowedKeysWithValidValues_IsValid()
    {
        var after = Terms(
            ("salary", 1_000_000m),
            ("currency", "USD"),
            ("positionId", 3),
            ("departmentId", 4),
            ("allowance", Terms(("lunch", 500_000))),
            ("workLocation", "Da Nang"),
            ("endDate", "2028-12-31"),
            ("noticePeriodDays", 45),
            ("other", "Remote Fridays"));

        var addendum = ContractAddendum.CreateDraft(ActiveContract, AddendumWrite(afterTerms: after), HrUserId, Now);

        Assert.Equal(9, addendum.ChangedTerms.Count);
    }

    public static TheoryData<ContractAddendumWrite, string> InvalidWrites => new()
    {
        { AddendumWrite(addendumNumber: "  "), "addendumNumber" },
        { AddendumWrite(addendumNumber: new string('x', 101)), "addendumNumber" },
        { AddendumWrite(effectiveDate: default(DateOnly)), "effectiveDate" },
        { AddendumWrite(effectiveDate: new DateOnly(2026, 9, 30)), "effectiveDate" },
        { AddendumWrite(nullBefore: true), "beforeTerms" },
        { AddendumWrite(nullAfter: true), "afterTerms" },
        { AddendumWrite(afterTerms: Terms()), "afterTerms" },
        { AddendumWrite(afterTerms: Terms(("bonus", 1))), "afterTerms.bonus" },
        { AddendumWrite(afterTerms: Terms(("salary", -1))), "afterTerms.salary" },
        { AddendumWrite(afterTerms: Terms(("salary", "high"))), "afterTerms.salary" },
        { AddendumWrite(afterTerms: Terms(("positionId", 0))), "afterTerms.positionId" },
        { AddendumWrite(afterTerms: Terms(("positionId", "lead"))), "afterTerms.positionId" },
        { AddendumWrite(afterTerms: Terms(("departmentId", -3))), "afterTerms.departmentId" },
        { AddendumWrite(afterTerms: Terms(("noticePeriodDays", -1))), "afterTerms.noticePeriodDays" },
        { AddendumWrite(afterTerms: Terms(("currency", "usd"))), "afterTerms.currency" },
        { AddendumWrite(afterTerms: Terms(("endDate", "31/12/2028"))), "afterTerms.endDate" },
        { AddendumWrite(reason: "  "), "reason" },
        { AddendumWrite(reason: new string('r', 5001)), "reason" }
    };

    [Theory]
    [MemberData(nameof(InvalidWrites))]
    public void CreateDraft_InvalidField_ThrowsValidationOnThatField(ContractAddendumWrite write, string field)
    {
        var exception = Assert.Throws<CoreHrValidationException>(() => ContractAddendum.CreateDraft(ActiveContract, write, HrUserId, Now));

        Assert.Contains(field, exception.Errors.Keys);
    }

    [Fact]
    public void Submit_FromDraft_MovesToPendingApproval()
    {
        var addendum = Addendum(version: 2);

        addendum.Submit(Now);

        Assert.Equal(ContractAddendumStatus.PendingApproval, addendum.Status);
        Assert.Equal(3, addendum.Version);
        Assert.Equal(Now, addendum.UpdatedAt);
    }

    [Theory]
    [InlineData(ContractAddendumStatus.PendingApproval)]
    [InlineData(ContractAddendumStatus.Approved)]
    [InlineData(ContractAddendumStatus.Effective)]
    [InlineData(ContractAddendumStatus.Cancelled)]
    public void Submit_FromNonDraft_ThrowsInvalidTransition(ContractAddendumStatus status)
    {
        var addendum = Addendum(status: status);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => addendum.Submit(Now));

        Assert.Equal(ContractAddendum.InvalidTransitionCode, exception.Code);
        Assert.Equal("submit", exception.Details["action"]);
    }

    [Fact]
    public void Approve_FromPendingApproval_RecordsApprover()
    {
        var addendum = Addendum(status: ContractAddendumStatus.PendingApproval);

        addendum.Approve(1300, Now);

        Assert.Equal(ContractAddendumStatus.Approved, addendum.Status);
        Assert.Equal(1300, addendum.ApprovedBy);
        Assert.Equal(Now, addendum.ApprovedAt);
        Assert.Equal(2, addendum.Version);
    }

    [Theory]
    [InlineData(ContractAddendumStatus.Draft)]
    [InlineData(ContractAddendumStatus.Approved)]
    [InlineData(ContractAddendumStatus.Effective)]
    public void Approve_FromNonPending_ThrowsInvalidTransition(ContractAddendumStatus status)
    {
        var addendum = Addendum(status: status);

        Assert.Throws<CoreHrBusinessRuleException>(() => addendum.Approve(1300, Now));
    }

    [Fact]
    public void AttachSignedDocument_Approved_StoresKeyWithoutSigning()
    {
        var addendum = Addendum(status: ContractAddendumStatus.Approved);

        addendum.AttachSignedDocument("contracts/42/addenda/7/abc", Now);

        Assert.Equal("contracts/42/addenda/7/abc", addendum.DocumentObjectKey);
        Assert.True(addendum.SignedDocumentAvailable);
        Assert.Null(addendum.SignedAt);
        Assert.Equal(ContractAddendumStatus.Approved, addendum.Status);
        Assert.Equal(2, addendum.Version);
    }

    [Theory]
    [InlineData(ContractAddendumStatus.Draft)]
    [InlineData(ContractAddendumStatus.PendingApproval)]
    [InlineData(ContractAddendumStatus.Effective)]
    [InlineData(ContractAddendumStatus.Superseded)]
    [InlineData(ContractAddendumStatus.Cancelled)]
    public void AttachSignedDocument_NotApproved_ThrowsSignedDocumentNotAllowed(ContractAddendumStatus status)
    {
        var addendum = Addendum(status: status);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => addendum.AttachSignedDocument("key", Now));

        Assert.Equal(ContractAddendum.SignedDocumentNotAllowedCode, exception.Code);
    }

    [Fact]
    public void MarkSigned_ApprovedWithDocument_SetsSignedAtAndStaysApproved()
    {
        var addendum = Addendum(status: ContractAddendumStatus.Approved, documentObjectKey: "key");

        addendum.MarkSigned(Now);

        Assert.Equal(Now, addendum.SignedAt);
        Assert.Equal(ContractAddendumStatus.Approved, addendum.Status);
        Assert.Equal(2, addendum.Version);
    }

    [Fact]
    public void MarkSigned_WithoutDocument_ThrowsSignatureRequired()
    {
        var addendum = Addendum(status: ContractAddendumStatus.Approved);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => addendum.MarkSigned(Now));

        Assert.Equal(ContractAddendum.SignatureRequiredCode, exception.Code);
        Assert.Null(addendum.SignedAt);
    }

    [Fact]
    public void MarkSigned_AlreadySigned_ThrowsInvalidTransition()
    {
        var addendum = Addendum(status: ContractAddendumStatus.Approved, documentObjectKey: "key", signedAt: Now.AddDays(-1));

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => addendum.MarkSigned(Now));

        Assert.Equal(ContractAddendum.InvalidTransitionCode, exception.Code);
    }

    [Theory]
    [InlineData(ContractAddendumStatus.Draft)]
    [InlineData(ContractAddendumStatus.PendingApproval)]
    [InlineData(ContractAddendumStatus.Effective)]
    public void MarkSigned_NotApproved_ThrowsInvalidTransition(ContractAddendumStatus status)
    {
        var addendum = Addendum(status: status, documentObjectKey: "key");

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => addendum.MarkSigned(Now));

        Assert.Equal(ContractAddendum.InvalidTransitionCode, exception.Code);
    }

    [Fact]
    public void MakeEffective_ApprovedAndSigned_BecomesEffective()
    {
        var addendum = Addendum(status: ContractAddendumStatus.Approved, documentObjectKey: "key", signedAt: Now.AddDays(-1), version: 4);

        addendum.MakeEffective(Now);

        Assert.Equal(ContractAddendumStatus.Effective, addendum.Status);
        Assert.Equal(5, addendum.Version);
    }

    [Fact]
    public void MakeEffective_ApprovedButUnsigned_ThrowsSignatureRequired()
    {
        var addendum = Addendum(status: ContractAddendumStatus.Approved, documentObjectKey: "key");

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => addendum.MakeEffective(Now));

        Assert.Equal(ContractAddendum.SignatureRequiredCode, exception.Code);
        Assert.Equal(ContractAddendumStatus.Approved, addendum.Status);
    }

    [Theory]
    [InlineData(ContractAddendumStatus.Draft)]
    [InlineData(ContractAddendumStatus.PendingApproval)]
    [InlineData(ContractAddendumStatus.Effective)]
    [InlineData(ContractAddendumStatus.Cancelled)]
    public void MakeEffective_NotApproved_ThrowsInvalidTransition(ContractAddendumStatus status)
    {
        var addendum = Addendum(status: status, signedAt: Now);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => addendum.MakeEffective(Now));

        Assert.Equal(ContractAddendum.InvalidTransitionCode, exception.Code);
    }

    [Fact]
    public void Supersede_Effective_BecomesSuperseded()
    {
        var addendum = Addendum(status: ContractAddendumStatus.Effective, signedAt: Now.AddDays(-3));

        addendum.Supersede(Now);

        Assert.Equal(ContractAddendumStatus.Superseded, addendum.Status);
        Assert.Equal(2, addendum.Version);
    }

    [Theory]
    [InlineData(ContractAddendumStatus.Approved)]
    [InlineData(ContractAddendumStatus.Superseded)]
    public void Supersede_NotEffective_ThrowsInvalidTransition(ContractAddendumStatus status)
    {
        var addendum = Addendum(status: status);

        Assert.Throws<CoreHrBusinessRuleException>(() => addendum.Supersede(Now));
    }

    [Theory]
    [InlineData(ContractAddendumStatus.Draft)]
    [InlineData(ContractAddendumStatus.PendingApproval)]
    [InlineData(ContractAddendumStatus.Approved)]
    public void Cancel_BeforeEffective_WithReason_BecomesCancelled(ContractAddendumStatus status)
    {
        var addendum = Addendum(status: status);

        addendum.Cancel("Withdrawn", Now);

        Assert.Equal(ContractAddendumStatus.Cancelled, addendum.Status);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void Cancel_WithoutReason_ThrowsValidation(string? reason)
    {
        var addendum = Addendum();

        var exception = Assert.Throws<CoreHrValidationException>(() => addendum.Cancel(reason, Now));

        Assert.Contains("reason", exception.Errors.Keys);
    }

    [Theory]
    [InlineData(ContractAddendumStatus.Effective)]
    [InlineData(ContractAddendumStatus.Superseded)]
    [InlineData(ContractAddendumStatus.Cancelled)]
    public void Cancel_AfterEffective_ThrowsInvalidTransition(ContractAddendumStatus status)
    {
        var addendum = Addendum(status: status);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => addendum.Cancel("reason", Now));

        Assert.Equal(ContractAddendum.InvalidTransitionCode, exception.Code);
    }

    [Fact]
    public void SharesTermWith_DetectsCommonAfterTermKeys()
    {
        var salary = Addendum(id: 1, afterTerms: Terms(("salary", 1)));
        var salaryAndLocation = Addendum(id: 2, afterTerms: Terms(("salary", 2), ("workLocation", "HCM")));
        var position = Addendum(id: 3, afterTerms: Terms(("positionId", 5)));

        Assert.True(salary.SharesTermWith(salaryAndLocation));
        Assert.True(salaryAndLocation.SharesTermWith(salary));
        Assert.False(salary.SharesTermWith(position));
    }

    [Fact]
    public void Constructor_GuardsIdentifiersAndVersion()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Addendum(id: 0, status: ContractAddendumStatus.Approved));
        Assert.Throws<ArgumentOutOfRangeException>(() => Addendum(contractId: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Addendum(version: 0));
        Assert.Throws<ArgumentNullException>(() => new ContractAddendum(
            1, 42, "PL", ContractAddendumStatus.Draft, new DateOnly(2027, 1, 1), null!, new JsonObject(), "r", null, HrUserId, null, null, null, 1, Now, Now));
    }
}
