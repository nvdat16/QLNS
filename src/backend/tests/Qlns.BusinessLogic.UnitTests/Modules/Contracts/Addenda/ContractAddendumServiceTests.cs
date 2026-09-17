using Qlns.BusinessLogic.Modules.Contracts.Addenda;
using Qlns.BusinessLogic.Modules.Contracts.Contracts;
using Qlns.BusinessLogic.Modules.Contracts.Shared;
using Qlns.BusinessLogic.Modules.CoreHr.EmployeeDocuments;
using Qlns.BusinessLogic.Modules.CoreHr.EmployeeEvents;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Xunit;
using static Qlns.BusinessLogic.UnitTests.Modules.Contracts.Addenda.AddendumTestData;
using Qlns.BusinessLogic.UnitTests.Modules.Contracts.Contracts;
using static Qlns.BusinessLogic.UnitTests.Modules.Contracts.Contracts.ContractTestData;

namespace Qlns.BusinessLogic.UnitTests.Modules.Contracts.Addenda;

public sealed class ContractAddendumServiceTests
{
    // ----- list -----

    [Fact]
    public async Task ListAsync_VisibleContract_ReturnsAddendaOrderedByEffectiveDateThenId()
    {
        var (contracts, addenda) = Repositories(ContractStatus.Active);
        addenda.Add(Addendum(id: 2, effectiveDate: new DateOnly(2027, 3, 1), addendumNumber: "B"));
        addenda.Add(Addendum(id: 1, effectiveDate: new DateOnly(2027, 3, 1), addendumNumber: "A"));
        addenda.Add(Addendum(id: 3, effectiveDate: new DateOnly(2027, 1, 1), addendumNumber: "C"));
        addenda.Add(Addendum(id: 4, contractId: 99, addendumNumber: "D"));
        var service = CreateService(contracts, addenda);

        var result = await service.ListAsync(ContractId, Self(), CancellationToken.None);

        Assert.Equal([3L, 1L, 2L], result.Select(a => a.Id));
    }

    [Fact]
    public async Task ListAsync_ContractOutOfScopeOrMissing_ThrowsNotFoundForTheContract()
    {
        var (contracts, addenda) = Repositories(ContractStatus.Active);
        var service = CreateService(contracts, addenda);

        var outOfScope = await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            service.ListAsync(ContractId, DepartmentReader(OtherDepartmentId), CancellationToken.None));
        var missing = await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            service.ListAsync(999, Hr(), CancellationToken.None));

        Assert.Equal("Contract", outOfScope.Resource);
        Assert.Equal(999, missing.Id);
    }

    // ----- create -----

    [Theory]
    [InlineData(ContractStatus.Active)]
    [InlineData(ContractStatus.Executed)]
    public async Task CreateAsync_InForceContract_InsertsDraftCreatedByActor(ContractStatus contractStatus)
    {
        var (contracts, addenda) = Repositories(contractStatus);
        var service = CreateService(contracts, addenda);

        var created = await service.CreateAsync(new CreateContractAddendumCommand(ContractId, AddendumWrite(), Hr()), CancellationToken.None);

        Assert.Equal(500, created.Id);
        Assert.Equal(ContractId, created.ContractId);
        Assert.Equal(ContractAddendumStatus.Draft, created.Status);
        Assert.Equal(Hr().UserId, created.CreatedBy);
        Assert.Equal(Now, created.CreatedAt);
        Assert.Single(addenda.Inserted);
    }

    [Theory]
    [InlineData(ContractStatus.Draft)]
    [InlineData(ContractStatus.Approved)]
    [InlineData(ContractStatus.Expired)]
    [InlineData(ContractStatus.Terminated)]
    public async Task CreateAsync_ContractNotInForce_ThrowsContractNotActive(ContractStatus contractStatus)
    {
        var (contracts, addenda) = Repositories(contractStatus);
        var service = CreateService(contracts, addenda);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            service.CreateAsync(new CreateContractAddendumCommand(ContractId, AddendumWrite(), Hr()), CancellationToken.None));

        Assert.Equal(ContractAddendumService.ContractNotActiveCode, exception.Code);
        Assert.Empty(addenda.Inserted);
    }

    [Fact]
    public async Task CreateAsync_EffectiveBeforeContractStart_ThrowsValidation()
    {
        var (contracts, addenda) = Repositories(ContractStatus.Active);
        var service = CreateService(contracts, addenda);

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() =>
            service.CreateAsync(new CreateContractAddendumCommand(ContractId, AddendumWrite(effectiveDate: new DateOnly(2026, 1, 1)), Hr()), CancellationToken.None));

        Assert.Contains("effectiveDate", exception.Errors.Keys);
    }

    [Fact]
    public async Task CreateAsync_DuplicateNumber_ThrowsNumberTaken()
    {
        var (contracts, addenda) = Repositories(ContractStatus.Active);
        addenda.TakenNumbers.Add("PL-2026-001");
        var service = CreateService(contracts, addenda);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            service.CreateAsync(new CreateContractAddendumCommand(ContractId, AddendumWrite(), Hr()), CancellationToken.None));

        Assert.Equal(ContractAddendumService.NumberTakenCode, exception.Code);
        Assert.Empty(addenda.Inserted);
    }

    [Fact]
    public async Task CreateAsync_WithoutWritePermissionOrSelfOnly_ThrowsForbidden()
    {
        var (contracts, addenda) = Repositories(ContractStatus.Active);
        var service = CreateService(contracts, addenda);

        var reader = await Assert.ThrowsAsync<CoreHrForbiddenException>(() =>
            service.CreateAsync(new CreateContractAddendumCommand(ContractId, AddendumWrite(), Hr(ContractPermissions.Read)), CancellationToken.None));
        var self = await Assert.ThrowsAsync<CoreHrForbiddenException>(() =>
            service.CreateAsync(new CreateContractAddendumCommand(ContractId, AddendumWrite(), Self()), CancellationToken.None));

        Assert.Equal(ContractAddendumService.WriteForbiddenCode, reader.Code);
        Assert.Equal(ContractAddendumService.WriteForbiddenCode, self.Code);
    }

    [Fact]
    public async Task CreateAsync_ContractOutOfScope_ThrowsNotFoundBeforePermissionCheck()
    {
        var (contracts, addenda) = Repositories(ContractStatus.Active);
        var service = CreateService(contracts, addenda);
        var outsider = Actor(300, CoreHrDataScope.Departments(OtherDepartmentId), [ContractPermissions.Write]);

        await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            service.CreateAsync(new CreateContractAddendumCommand(ContractId, AddendumWrite(), outsider), CancellationToken.None));
    }

    // ----- simple transitions -----

    [Fact]
    public async Task TransitionAsync_Submit_SavesPendingApproval()
    {
        var (contracts, addenda) = Repositories(ContractStatus.Active);
        addenda.Add(Addendum(version: 2));
        var service = CreateService(contracts, addenda);

        var updated = await service.TransitionAsync(Transition(ContractAddendumAction.Submit, 2, Hr()), CancellationToken.None);

        Assert.Equal(ContractAddendumStatus.PendingApproval, updated.Status);
        Assert.Equal([(AddendumId, ContractAddendumStatus.Draft, ContractAddendumStatus.PendingApproval, 2L, (string?)null)], addenda.Transitions);
    }

    [Fact]
    public async Task TransitionAsync_ApproveWithPermission_RecordsActorAsApprover()
    {
        var (contracts, addenda) = Repositories(ContractStatus.Active);
        addenda.Add(Addendum(status: ContractAddendumStatus.PendingApproval));
        var service = CreateService(contracts, addenda);

        var updated = await service.TransitionAsync(Transition(ContractAddendumAction.Approve, 1, Approver()), CancellationToken.None);

        Assert.Equal(ContractAddendumStatus.Approved, updated.Status);
        Assert.Equal(Approver().UserId, updated.ApprovedBy);
        Assert.Equal(Now, updated.ApprovedAt);
    }

    [Fact]
    public async Task TransitionAsync_ApproveWithoutPermission_ThrowsForbiddenWithoutSaving()
    {
        var (contracts, addenda) = Repositories(ContractStatus.Active);
        addenda.Add(Addendum(status: ContractAddendumStatus.PendingApproval));
        var service = CreateService(contracts, addenda);

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() =>
            service.TransitionAsync(Transition(ContractAddendumAction.Approve, 1, Hr()), CancellationToken.None));

        Assert.Equal(ContractAddendumService.ApproveForbiddenCode, exception.Code);
        Assert.Empty(addenda.Transitions);
    }

    [Fact]
    public async Task TransitionAsync_MarkSignedWithDocument_SetsSignedAt()
    {
        var (contracts, addenda) = Repositories(ContractStatus.Active);
        addenda.Add(Addendum(status: ContractAddendumStatus.Approved, documentObjectKey: "key"));
        var service = CreateService(contracts, addenda);

        var updated = await service.TransitionAsync(Transition(ContractAddendumAction.MarkSigned, 1, Hr()), CancellationToken.None);

        Assert.Equal(Now, updated.SignedAt);
        Assert.Equal(ContractAddendumStatus.Approved, updated.Status);
        Assert.Equal(ContractAddendumStatus.Approved, addenda.Transitions.Single().Previous);
    }

    [Fact]
    public async Task TransitionAsync_MarkSignedWithoutDocument_ThrowsSignatureRequired()
    {
        var (contracts, addenda) = Repositories(ContractStatus.Active);
        addenda.Add(Addendum(status: ContractAddendumStatus.Approved));
        var service = CreateService(contracts, addenda);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            service.TransitionAsync(Transition(ContractAddendumAction.MarkSigned, 1, Hr()), CancellationToken.None));

        Assert.Equal(ContractAddendum.SignatureRequiredCode, exception.Code);
    }

    [Fact]
    public async Task TransitionAsync_CancelWithReason_PassesTrimmedReason()
    {
        var (contracts, addenda) = Repositories(ContractStatus.Active);
        addenda.Add(Addendum(status: ContractAddendumStatus.PendingApproval));
        var service = CreateService(contracts, addenda);

        var updated = await service.TransitionAsync(Transition(ContractAddendumAction.Cancel, 1, Hr(), "  Withdrawn "), CancellationToken.None);

        Assert.Equal(ContractAddendumStatus.Cancelled, updated.Status);
        Assert.Equal("Withdrawn", addenda.Transitions.Single().Reason);
    }

    [Fact]
    public async Task TransitionAsync_CancelWithoutReason_ThrowsValidation()
    {
        var (contracts, addenda) = Repositories(ContractStatus.Active);
        addenda.Add(Addendum());
        var service = CreateService(contracts, addenda);

        await Assert.ThrowsAsync<CoreHrValidationException>(() =>
            service.TransitionAsync(Transition(ContractAddendumAction.Cancel, 1, Hr()), CancellationToken.None));
    }

    [Fact]
    public async Task TransitionAsync_InvalidStep_ThrowsBusinessRuleWithoutSaving()
    {
        var (contracts, addenda) = Repositories(ContractStatus.Active);
        addenda.Add(Addendum(status: ContractAddendumStatus.Approved));
        var service = CreateService(contracts, addenda);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            service.TransitionAsync(Transition(ContractAddendumAction.Submit, 1, Hr()), CancellationToken.None));

        Assert.Equal(ContractAddendum.InvalidTransitionCode, exception.Code);
        Assert.Empty(addenda.Transitions);
    }

    [Fact]
    public async Task TransitionAsync_StaleVersion_ThrowsConcurrencyWithoutSaving()
    {
        var (contracts, addenda) = Repositories(ContractStatus.Active);
        addenda.Add(Addendum(version: 3));
        var service = CreateService(contracts, addenda);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() =>
            service.TransitionAsync(Transition(ContractAddendumAction.Submit, 2, Hr()), CancellationToken.None));

        Assert.Empty(addenda.Transitions);
    }

    [Fact]
    public async Task TransitionAsync_LostRace_ThrowsConcurrency()
    {
        var (contracts, addenda) = Repositories(ContractStatus.Active);
        addenda.SaveSucceeds = false;
        addenda.Add(Addendum());
        var service = CreateService(contracts, addenda);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() =>
            service.TransitionAsync(Transition(ContractAddendumAction.Submit, 1, Hr()), CancellationToken.None));

        Assert.Single(addenda.Transitions);
    }

    [Fact]
    public async Task TransitionAsync_UnknownAddendumOrContractOutOfScope_ThrowsNotFoundForTheAddendum()
    {
        var (contracts, addenda) = Repositories(ContractStatus.Active);
        addenda.Add(Addendum());
        var service = CreateService(contracts, addenda);
        var outsider = Actor(300, CoreHrDataScope.Departments(OtherDepartmentId), [ContractPermissions.Write]);

        var missing = await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            service.TransitionAsync(new TransitionContractAddendumCommand(999, ContractAddendumAction.Submit, 1, null, Hr()), CancellationToken.None));
        var outOfScope = await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            service.TransitionAsync(Transition(ContractAddendumAction.Submit, 1, outsider), CancellationToken.None));

        Assert.Equal(ContractAddendumService.ResourceName, missing.Resource);
        Assert.Equal(ContractAddendumService.ResourceName, outOfScope.Resource);
        Assert.Equal(AddendumId, outOfScope.Id);
    }

    // ----- make-effective -----

    [Fact]
    public async Task MakeEffective_SalaryAddendum_SavesEffectiveWithApprovedSalaryAdjustmentEvent()
    {
        var (contracts, addenda) = Repositories(ContractStatus.Active);
        addenda.Add(Addendum(status: ContractAddendumStatus.Approved, documentObjectKey: "key", signedAt: Now.AddDays(-1), version: 4,
            beforeTerms: Terms(("salary", 25_000_000m)), afterTerms: Terms(("salary", 30_000_000m), ("workLocation", "HCM"))));
        var service = CreateService(contracts, addenda);

        var updated = await service.TransitionAsync(Transition(ContractAddendumAction.MakeEffective, 4, Hr()), CancellationToken.None);

        Assert.Equal(ContractAddendumStatus.Effective, updated.Status);
        var activation = Assert.Single(addenda.Activations);
        Assert.Same(updated, activation.Addendum);
        Assert.Equal(4, activation.ExpectedVersion);
        Assert.Empty(activation.Superseded);
        Assert.NotNull(activation.EmployeeEvent);
        Assert.Equal(EmployeeEventType.SalaryAdjustment, activation.EmployeeEvent.EventType);
        Assert.Equal(EmployeeId, activation.EmployeeEvent.EmployeeId);
        Assert.Equal(updated.EffectiveDate, activation.EmployeeEvent.EffectiveDate);
        Assert.Equal(["salary"], activation.EmployeeEvent.AfterData.Select(pair => pair.Key));
        Assert.Empty(addenda.Transitions);
    }

    [Fact]
    public async Task MakeEffective_NonMasterDataAddendum_RaisesNoEvent()
    {
        var (contracts, addenda) = Repositories(ContractStatus.Active);
        addenda.Add(Addendum(status: ContractAddendumStatus.Approved, documentObjectKey: "key", signedAt: Now,
            afterTerms: Terms(("workLocation", "Da Nang"))));
        var service = CreateService(contracts, addenda);

        await service.TransitionAsync(Transition(ContractAddendumAction.MakeEffective, 1, Hr()), CancellationToken.None);

        Assert.Null(addenda.Activations.Single().EmployeeEvent);
    }

    [Fact]
    public async Task MakeEffective_SupersedesOlderEffectiveAddendaOnSharedTermsOnly()
    {
        var (contracts, addenda) = Repositories(ContractStatus.Active);
        var olderSalary = addenda.Add(Addendum(id: 1, status: ContractAddendumStatus.Effective, signedAt: Now.AddDays(-100), version: 5,
            addendumNumber: "OLD-SALARY", effectiveDate: new DateOnly(2026, 11, 1), afterTerms: Terms(("salary", 26_000_000m))));
        var olderLocation = addenda.Add(Addendum(id: 2, status: ContractAddendumStatus.Effective, signedAt: Now.AddDays(-90),
            addendumNumber: "OLD-LOCATION", effectiveDate: new DateOnly(2026, 11, 15), afterTerms: Terms(("workLocation", "Hanoi"))));
        addenda.Add(Addendum(id: 3, status: ContractAddendumStatus.Cancelled, addendumNumber: "CANCELLED", afterTerms: Terms(("salary", 1))));
        addenda.Add(Addendum(id: 4, status: ContractAddendumStatus.Effective, signedAt: Now, contractId: 99, addendumNumber: "OTHER-CONTRACT", afterTerms: Terms(("salary", 1))));
        addenda.Add(Addendum(status: ContractAddendumStatus.Approved, documentObjectKey: "key", signedAt: Now.AddDays(-1),
            afterTerms: Terms(("salary", 30_000_000m))));
        var service = CreateService(contracts, addenda);

        await service.TransitionAsync(Transition(ContractAddendumAction.MakeEffective, 1, Hr()), CancellationToken.None);

        var activation = addenda.Activations.Single();
        var superseded = Assert.Single(activation.Superseded);
        Assert.Same(olderSalary, superseded.Addendum);
        Assert.Equal(5, superseded.ExpectedVersion);
        Assert.Equal(ContractAddendumStatus.Superseded, olderSalary.Status);
        Assert.Equal(6, olderSalary.Version);
        Assert.Equal(ContractAddendumStatus.Effective, olderLocation.Status);
    }

    [Fact]
    public async Task MakeEffective_Unsigned_ThrowsSignatureRequiredWithoutSaving()
    {
        var (contracts, addenda) = Repositories(ContractStatus.Active);
        addenda.Add(Addendum(status: ContractAddendumStatus.Approved, documentObjectKey: "key"));
        var service = CreateService(contracts, addenda);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            service.TransitionAsync(Transition(ContractAddendumAction.MakeEffective, 1, Hr()), CancellationToken.None));

        Assert.Equal(ContractAddendum.SignatureRequiredCode, exception.Code);
        Assert.Empty(addenda.Activations);
    }

    [Fact]
    public async Task MakeEffective_ContractNoLongerInForce_ThrowsContractNotActive()
    {
        var (contracts, addenda) = Repositories(ContractStatus.Terminated);
        addenda.Add(Addendum(status: ContractAddendumStatus.Approved, documentObjectKey: "key", signedAt: Now));
        var service = CreateService(contracts, addenda);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            service.TransitionAsync(Transition(ContractAddendumAction.MakeEffective, 1, Hr()), CancellationToken.None));

        Assert.Equal(ContractAddendumService.ContractNotActiveCode, exception.Code);
        Assert.Empty(addenda.Activations);
    }

    [Fact]
    public async Task MakeEffective_LostRace_ThrowsConcurrency()
    {
        var (contracts, addenda) = Repositories(ContractStatus.Active);
        addenda.SaveSucceeds = false;
        addenda.Add(Addendum(status: ContractAddendumStatus.Approved, documentObjectKey: "key", signedAt: Now));
        var service = CreateService(contracts, addenda);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() =>
            service.TransitionAsync(Transition(ContractAddendumAction.MakeEffective, 1, Hr()), CancellationToken.None));
    }

    // ----- signed document -----

    [Fact]
    public async Task AttachSignedDocument_Approved_ScansStoresUnderAddendumKeyAndSaves()
    {
        var (contracts, addenda) = Repositories(ContractStatus.Active);
        addenda.Add(Addendum(status: ContractAddendumStatus.Approved, version: 2));
        var storage = new FakeStorage();
        var scanner = new FakeScanner(MalwareScanResult.Clean);
        var service = CreateService(contracts, addenda, storage, scanner);

        var updated = await service.AttachSignedDocumentAsync(new UploadSignedAddendumCommand(AddendumId, 2, Pdf(), Hr()), CancellationToken.None);

        Assert.StartsWith("contracts/42/addenda/7/", updated.DocumentObjectKey, StringComparison.Ordinal);
        Assert.Null(updated.SignedAt);
        Assert.Equal(ContractAddendumStatus.Approved, updated.Status);
        Assert.Equal(3, updated.Version);
        Assert.Equal(1, scanner.Calls);
        Assert.Equal(updated.DocumentObjectKey, storage.Stored.Single().ObjectKey);
        Assert.Equal(0, storage.Stored.Single().PositionAtStore);
        Assert.Equal([(AddendumId, 2L)], addenda.SignedDocuments);
    }

    [Theory]
    [InlineData(ContractAddendumStatus.Draft)]
    [InlineData(ContractAddendumStatus.PendingApproval)]
    [InlineData(ContractAddendumStatus.Effective)]
    public async Task AttachSignedDocument_NotApproved_Throws409BeforeScanning(ContractAddendumStatus status)
    {
        var (contracts, addenda) = Repositories(ContractStatus.Active);
        addenda.Add(Addendum(status: status));
        var scanner = new FakeScanner(MalwareScanResult.Clean);
        var service = CreateService(contracts, addenda, scanner: scanner);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            service.AttachSignedDocumentAsync(new UploadSignedAddendumCommand(AddendumId, 1, Pdf(), Hr()), CancellationToken.None));

        Assert.Equal(ContractAddendum.SignedDocumentNotAllowedCode, exception.Code);
        Assert.Equal(0, scanner.Calls);
    }

    [Fact]
    public async Task AttachSignedDocument_Infected_Throws422AndStoresNothing()
    {
        var (contracts, addenda) = Repositories(ContractStatus.Active);
        addenda.Add(Addendum(status: ContractAddendumStatus.Approved));
        var storage = new FakeStorage();
        var service = CreateService(contracts, addenda, storage, new FakeScanner(MalwareScanResult.Infected));

        await Assert.ThrowsAsync<CoreHrValidationException>(() =>
            service.AttachSignedDocumentAsync(new UploadSignedAddendumCommand(AddendumId, 1, Pdf(), Hr()), CancellationToken.None));

        Assert.Empty(storage.Stored);
        Assert.Empty(addenda.SignedDocuments);
    }

    [Fact]
    public async Task AttachSignedDocument_ScannerUnavailable_Throws409WithAddendumCode()
    {
        var (contracts, addenda) = Repositories(ContractStatus.Active);
        addenda.Add(Addendum(status: ContractAddendumStatus.Approved));
        var service = CreateService(contracts, addenda, scanner: new FakeScanner(MalwareScanResult.Unavailable));

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            service.AttachSignedDocumentAsync(new UploadSignedAddendumCommand(AddendumId, 1, Pdf(), Hr()), CancellationToken.None));

        Assert.Equal(ContractAddendumService.ScanUnavailableCode, exception.Code);
    }

    [Fact]
    public async Task AttachSignedDocument_SaveFails_DeletesStoredObjectAndRethrows()
    {
        var (contracts, addenda) = Repositories(ContractStatus.Active);
        addenda.SaveFailure = new InvalidOperationException("db down");
        addenda.Add(Addendum(status: ContractAddendumStatus.Approved));
        var storage = new FakeStorage();
        var service = CreateService(contracts, addenda, storage);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AttachSignedDocumentAsync(new UploadSignedAddendumCommand(AddendumId, 1, Pdf(), Hr()), CancellationToken.None));

        Assert.Equal([storage.Stored.Single().ObjectKey], storage.Deleted);
    }

    [Fact]
    public async Task AttachSignedDocument_LostRace_DeletesStoredObjectAndThrowsConcurrency()
    {
        var (contracts, addenda) = Repositories(ContractStatus.Active);
        addenda.SaveSucceeds = false;
        addenda.Add(Addendum(status: ContractAddendumStatus.Approved));
        var storage = new FakeStorage();
        var service = CreateService(contracts, addenda, storage);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() =>
            service.AttachSignedDocumentAsync(new UploadSignedAddendumCommand(AddendumId, 1, Pdf(), Hr()), CancellationToken.None));

        Assert.Equal([storage.Stored.Single().ObjectKey], storage.Deleted);
        Assert.Single(addenda.SignedDocuments);
    }

    [Fact]
    public async Task AttachSignedDocument_WithoutWritePermission_ThrowsForbidden()
    {
        var (contracts, addenda) = Repositories(ContractStatus.Active);
        addenda.Add(Addendum(status: ContractAddendumStatus.Approved));
        var service = CreateService(contracts, addenda);

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() =>
            service.AttachSignedDocumentAsync(new UploadSignedAddendumCommand(AddendumId, 1, Pdf(), Self()), CancellationToken.None));

        Assert.Equal(ContractAddendumService.WriteForbiddenCode, exception.Code);
    }

    private static (FakeContractRepository Contracts, FakeAddendumRepository Addenda) Repositories(ContractStatus contractStatus)
    {
        var contracts = new FakeContractRepository();
        contracts.Add(ContractTestData.Build(id: ContractId, status: contractStatus, startDate: new DateOnly(2026, 10, 1), endDate: new DateOnly(2028, 9, 30)));
        return (contracts, new FakeAddendumRepository());
    }

    private static ContractAddendumService CreateService(
        FakeContractRepository contracts,
        FakeAddendumRepository addenda,
        FakeStorage? storage = null,
        FakeScanner? scanner = null) =>
        new(contracts, addenda, Uploader(storage, scanner), new FixedTimeProvider(Now));

    private static TransitionContractAddendumCommand Transition(ContractAddendumAction action, long expectedVersion, CoreHrActor actor, string? reason = null) =>
        new(AddendumId, action, expectedVersion, reason, actor);
}
