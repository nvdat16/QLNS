using Qlns.BusinessLogic.Modules.Contracts.Contracts;
using Qlns.BusinessLogic.Modules.Contracts.Shared;
using Qlns.BusinessLogic.Modules.CoreHr.EmployeeDocuments;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Xunit;
using static Qlns.BusinessLogic.UnitTests.Modules.Contracts.Contracts.ContractTestData;

namespace Qlns.BusinessLogic.UnitTests.Modules.Contracts.Contracts;

public sealed class ContractServiceTests
{
    // ----- search / get -----

    [Fact]
    public async Task SearchAsync_SelfOnlyActor_SeesOnlyOwnContracts()
    {
        var repository = new FakeContractRepository();
        repository.Employees[11] = new ContractEmployee(11, DepartmentId, null);
        repository.Add(Build(id: 1, employeeId: EmployeeId, contractNumber: "A"));
        repository.Add(Build(id: 2, employeeId: 11, contractNumber: "B"));
        var service = CreateService(repository);

        var result = await service.SearchAsync(new ContractSearchQuery(null, null, null, PageRequest.Default), Self(), CancellationToken.None);

        Assert.Equal([1L], result.Items.Select(c => c.Id));
        Assert.Same(repository.LastSearchQuery, repository.LastSearchQuery);
    }

    [Fact]
    public async Task GetAsync_OutsideDepartmentScope_ThrowsNotFound()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build());
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            service.GetAsync(42, DepartmentReader(OtherDepartmentId), CancellationToken.None));

        Assert.Equal("Contract", exception.Resource);
        Assert.Equal(42, exception.Id);
        Assert.NotNull(await service.GetAsync(42, DepartmentReader(DepartmentId), CancellationToken.None));
        Assert.NotNull(await service.GetAsync(42, Self(), CancellationToken.None));
    }

    // ----- create -----

    [Fact]
    public async Task CreateAsync_Valid_ChecksNumberThenInsertsDraft()
    {
        var repository = new FakeContractRepository();
        var service = CreateService(repository);

        var created = await service.CreateAsync(new CreateContractCommand(Write(), Hr()), CancellationToken.None);

        Assert.Equal(100, created.Id);
        Assert.Equal(ContractStatus.Draft, created.Status);
        Assert.Equal(1, created.Version);
        Assert.Equal(Now, created.CreatedAt);
        Assert.Single(repository.Inserted);
        Assert.Equal(Hr().UserId, repository.Inserted[0].Actor.UserId);
    }

    [Fact]
    public async Task CreateAsync_EmployeeMissingOrOutOfScope_ThrowsNotFoundBeforePermissionCheck()
    {
        var repository = new FakeContractRepository();
        var service = CreateService(repository);
        var outsider = Actor(300, CoreHrDataScope.Departments(OtherDepartmentId), [ContractPermissions.Write]);

        var missing = await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            service.CreateAsync(new CreateContractCommand(Write(employeeId: 999), Hr()), CancellationToken.None));
        var outOfScope = await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            service.CreateAsync(new CreateContractCommand(Write(), outsider), CancellationToken.None));

        Assert.Equal("Employee", missing.Resource);
        Assert.Equal("Employee", outOfScope.Resource);
        Assert.Empty(repository.Inserted);
    }

    [Fact]
    public async Task CreateAsync_WithoutWritePermission_ThrowsForbidden()
    {
        var service = CreateService(new FakeContractRepository());

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() =>
            service.CreateAsync(new CreateContractCommand(Write(), Hr(ContractPermissions.Read)), CancellationToken.None));

        Assert.Equal(ContractService.WriteForbiddenCode, exception.Code);
    }

    [Fact]
    public async Task CreateAsync_SelfOnlyActorEvenWithWriteClaim_ThrowsForbidden()
    {
        var service = CreateService(new FakeContractRepository());

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() =>
            service.CreateAsync(new CreateContractCommand(Write(), Self()), CancellationToken.None));

        Assert.Equal(ContractService.WriteForbiddenCode, exception.Code);
    }

    [Fact]
    public async Task CreateAsync_InvalidPayload_ThrowsValidationWithoutInserting()
    {
        var repository = new FakeContractRepository();
        var service = CreateService(repository);

        await Assert.ThrowsAsync<CoreHrValidationException>(() =>
            service.CreateAsync(new CreateContractCommand(Write(contractType: "fixed_term", noEndDate: true), Hr()), CancellationToken.None));

        Assert.Empty(repository.Inserted);
    }

    [Fact]
    public async Task CreateAsync_DuplicateNumber_ThrowsNumberTaken()
    {
        var repository = new FakeContractRepository();
        repository.TakenNumbers.Add("HD-2026-001");
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            service.CreateAsync(new CreateContractCommand(Write(), Hr()), CancellationToken.None));

        Assert.Equal(ContractService.NumberTakenCode, exception.Code);
        Assert.Equal("HD-2026-001", exception.Details["contractNumber"]);
        Assert.Empty(repository.Inserted);
    }

    // ----- replace -----

    [Fact]
    public async Task ReplaceAsync_Draft_SavesWithChangedFieldsAndExpectedVersion()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build(version: 2));
        var service = CreateService(repository);

        var updated = await service.ReplaceAsync(
            new ReplaceContractCommand(42, 2, Write(contractNumber: "HD-2026-009"), Hr()),
            CancellationToken.None);

        Assert.Equal(3, updated.Version);
        Assert.Single(repository.Replacements);
        Assert.Equal((42L, 2L), (repository.Replacements[0].ContractId, repository.Replacements[0].ExpectedVersion));
        Assert.Equal(["contractNumber"], repository.Replacements[0].ChangedFields);
    }

    [Fact]
    public async Task ReplaceAsync_NumberOwnedByAnotherContract_ThrowsNumberTaken()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build(id: 42, contractNumber: "A"));
        repository.Add(Build(id: 43, contractNumber: "B"));
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            service.ReplaceAsync(new ReplaceContractCommand(42, 1, Write(contractNumber: "B"), Hr()), CancellationToken.None));

        Assert.Equal(ContractService.NumberTakenCode, exception.Code);
        Assert.Empty(repository.Replacements);
    }

    [Fact]
    public async Task ReplaceAsync_KeepingOwnNumber_IsNotADuplicate()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build(contractNumber: "A"));
        var service = CreateService(repository);

        var updated = await service.ReplaceAsync(new ReplaceContractCommand(42, 1, Write(contractNumber: "A", salary: 1), Hr()), CancellationToken.None);

        Assert.Equal(1m, updated.Salary);
    }

    [Fact]
    public async Task ReplaceAsync_StaleVersion_ThrowsConcurrencyWithoutSaving()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build(version: 3));
        var service = CreateService(repository);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() =>
            service.ReplaceAsync(new ReplaceContractCommand(42, 2, Write(), Hr()), CancellationToken.None));

        Assert.Empty(repository.Replacements);
    }

    [Fact]
    public async Task ReplaceAsync_LostRace_ThrowsConcurrency()
    {
        var repository = new FakeContractRepository { SaveSucceeds = false };
        repository.Add(Build());
        var service = CreateService(repository);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() =>
            service.ReplaceAsync(new ReplaceContractCommand(42, 1, Write(), Hr()), CancellationToken.None));

        Assert.Single(repository.Replacements);
    }

    [Fact]
    public async Task ReplaceAsync_NonDraft_ThrowsNotEditable()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build(status: ContractStatus.Approved));
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            service.ReplaceAsync(new ReplaceContractCommand(42, 1, Write(), Hr()), CancellationToken.None));

        Assert.Equal(Contract.NotEditableCode, exception.Code);
    }

    // ----- approve / terminate / cancel -----

    [Fact]
    public async Task TransitionAsync_ApproveWithPermission_SavesApprovedTransition()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build(version: 2));
        var service = CreateService(repository);

        var approved = await service.TransitionAsync(Transition(ContractAction.Approve, 2, Approver()), CancellationToken.None);

        Assert.Equal(ContractStatus.Approved, approved.Status);
        Assert.Equal(3, approved.Version);
        Assert.Equal([(42L, ContractStatus.Draft, ContractStatus.Approved, 2L, (string?)null)], repository.Transitions);
    }

    [Fact]
    public async Task TransitionAsync_ApproveWithoutApprovePermission_ThrowsForbidden()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build());
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() =>
            service.TransitionAsync(Transition(ContractAction.Approve, 1, Hr()), CancellationToken.None));

        Assert.Equal(ContractService.ApproveForbiddenCode, exception.Code);
        Assert.Empty(repository.Transitions);
    }

    [Fact]
    public async Task TransitionAsync_TerminateWithReason_PassesTrimmedReason()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build(status: ContractStatus.Active));
        var service = CreateService(repository);

        var terminated = await service.TransitionAsync(
            Transition(ContractAction.Terminate, 1, Hr(), new ContractActionOptions("  Resigned ", null, false)),
            CancellationToken.None);

        Assert.Equal(ContractStatus.Terminated, terminated.Status);
        Assert.Equal("Resigned", repository.Transitions.Single().Reason);
    }

    [Fact]
    public async Task TransitionAsync_TerminateWithoutReason_ThrowsValidationWithoutSaving()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build(status: ContractStatus.Active));
        var service = CreateService(repository);

        await Assert.ThrowsAsync<CoreHrValidationException>(() =>
            service.TransitionAsync(Transition(ContractAction.Terminate, 1, Hr()), CancellationToken.None));

        Assert.Empty(repository.Transitions);
    }

    [Fact]
    public async Task TransitionAsync_CancelApproved_SavesCancelled()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build(status: ContractStatus.Approved));
        var service = CreateService(repository);

        var cancelled = await service.TransitionAsync(
            Transition(ContractAction.Cancel, 1, Hr(), new ContractActionOptions("Superseded by new offer", null, false)),
            CancellationToken.None);

        Assert.Equal(ContractStatus.Cancelled, cancelled.Status);
        Assert.Equal(ContractStatus.Approved, repository.Transitions.Single().Previous);
    }

    [Fact]
    public async Task TransitionAsync_InvalidWorkflowStep_ThrowsBusinessRuleWithoutSaving()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build(status: ContractStatus.Active));
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            service.TransitionAsync(Transition(ContractAction.Cancel, 1, Hr(), new ContractActionOptions("x", null, false)), CancellationToken.None));

        Assert.Equal(Contract.InvalidTransitionCode, exception.Code);
        Assert.Empty(repository.Transitions);
    }

    [Fact]
    public async Task TransitionAsync_StaleVersion_ThrowsConcurrencyBeforePermissionCheck()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build(version: 5));
        var service = CreateService(repository);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() =>
            service.TransitionAsync(Transition(ContractAction.Approve, 4, Hr(ContractPermissions.Read)), CancellationToken.None));
    }

    [Fact]
    public async Task TransitionAsync_SelfOnlyActor_CannotEndOwnContract()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build(status: ContractStatus.Active));
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() =>
            service.TransitionAsync(Transition(ContractAction.Terminate, 1, Self(), new ContractActionOptions("quit", null, false)), CancellationToken.None));

        Assert.Equal(ContractService.WriteForbiddenCode, exception.Code);
    }

    [Fact]
    public async Task TransitionAsync_UnknownOrOutOfScopeContract_ThrowsNotFound()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build());
        var service = CreateService(repository);
        var outsider = Actor(300, CoreHrDataScope.Departments(OtherDepartmentId), [ContractPermissions.Write]);

        await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            service.TransitionAsync(Transition(ContractAction.Approve, 1, outsider), CancellationToken.None));
        await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            service.TransitionAsync(new TransitionContractCommand(999, ContractAction.Approve, 1, ContractActionOptions.None, Approver()), CancellationToken.None));
    }

    // ----- activate -----

    [Fact]
    public async Task Activate_ExecutedWithoutOtherPrimary_SavesActivationWithoutSupersededOrReview()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build(status: ContractStatus.Executed, documentObjectKey: "key", signedAt: Now.AddDays(-1), version: 3));
        var service = CreateService(repository);

        var active = await service.TransitionAsync(Transition(ContractAction.Activate, 3, Hr()), CancellationToken.None);

        Assert.Equal(ContractStatus.Active, active.Status);
        Assert.Equal(4, active.Version);
        var activation = Assert.Single(repository.Activations);
        Assert.Same(active, activation.Contract);
        Assert.Equal(ContractStatus.Executed, activation.PreviousStatus);
        Assert.Equal(3, activation.ExpectedVersion);
        Assert.Null(activation.Superseded);
        Assert.Null(activation.ProbationReview);
        Assert.Empty(repository.Transitions);
    }

    [Fact]
    public async Task Activate_ApprovedWithSignedAtInBody_RecordsSigningTime()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build(status: ContractStatus.Approved));
        var service = CreateService(repository);
        var signedAt = Now.AddHours(-3);

        var active = await service.TransitionAsync(
            Transition(ContractAction.Activate, 1, Hr(), new ContractActionOptions(null, signedAt, false)),
            CancellationToken.None);

        Assert.Equal(ContractStatus.Active, active.Status);
        Assert.Equal(signedAt, active.SignedAt);
    }

    [Fact]
    public async Task Activate_WithoutSignatureEvidence_ThrowsSignatureRequiredBeforeOverlapLookup()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build(status: ContractStatus.Approved));
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            service.TransitionAsync(Transition(ContractAction.Activate, 1, Hr()), CancellationToken.None));

        Assert.Equal(Contract.SignatureRequiredCode, exception.Code);
        Assert.Equal(0, repository.FindOtherPrimaryCalls);
        Assert.Empty(repository.Activations);
    }

    [Fact]
    public async Task Activate_PredecessorEndsBeforeStart_ExpiresItByNaturalSuccession()
    {
        var repository = new FakeContractRepository();
        var old = repository.Add(Build(id: 41, status: ContractStatus.Active, contractNumber: "OLD",
            startDate: new DateOnly(2025, 10, 1), endDate: new DateOnly(2026, 9, 30), version: 6));
        repository.Add(Build(id: 42, status: ContractStatus.Executed, documentObjectKey: "key",
            startDate: new DateOnly(2026, 10, 1), endDate: new DateOnly(2027, 9, 30)));
        var service = CreateService(repository);

        var active = await service.TransitionAsync(Transition(ContractAction.Activate, 1, Hr()), CancellationToken.None);

        Assert.Equal(ContractStatus.Active, active.Status);
        var superseded = Assert.Single(repository.Activations).Superseded;
        Assert.NotNull(superseded);
        Assert.Same(old, superseded.Contract);
        Assert.Equal(ContractStatus.Expired, old.Status);
        Assert.Equal(ContractStatus.Active, superseded.PreviousStatus);
        Assert.Equal(6, superseded.ExpectedVersion);
        Assert.Equal(7, old.Version);
        Assert.Null(superseded.OverrideReason);
    }

    [Fact]
    public async Task Activate_OverlappingPredecessorWithoutOverride_ThrowsPrimaryOverlap()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build(id: 41, status: ContractStatus.Active, contractNumber: "OLD",
            startDate: new DateOnly(2026, 1, 1), endDate: new DateOnly(2026, 12, 31)));
        repository.Add(Build(id: 42, status: ContractStatus.Approved, startDate: new DateOnly(2026, 10, 1), endDate: new DateOnly(2027, 9, 30)));
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            service.TransitionAsync(Transition(ContractAction.Activate, 1, Approver(), new ContractActionOptions(null, Now, false)), CancellationToken.None));

        Assert.Equal(ContractService.PrimaryOverlapCode, exception.Code);
        Assert.Equal(41L, exception.Details["overlappingContractId"]);
        Assert.Empty(repository.Activations);
    }

    [Fact]
    public async Task Activate_IndefinitePredecessor_IsAnOverlapEvenThoughNewStartsLater()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build(id: 41, status: ContractStatus.Active, contractNumber: "OLD", type: ContractType.Indefinite, indefinite: true,
            startDate: new DateOnly(2020, 1, 1)));
        repository.Add(Build(id: 42, status: ContractStatus.Executed, documentObjectKey: "key"));
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            service.TransitionAsync(Transition(ContractAction.Activate, 1, Hr()), CancellationToken.None));

        Assert.Equal(ContractService.PrimaryOverlapCode, exception.Code);
    }

    [Fact]
    public async Task Activate_OverrideWithoutApprovePermission_ThrowsForbidden()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build(id: 41, status: ContractStatus.Active, contractNumber: "OLD", endDate: new DateOnly(2026, 12, 31)));
        repository.Add(Build(id: 42, status: ContractStatus.Executed, documentObjectKey: "key"));
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() =>
            service.TransitionAsync(Transition(ContractAction.Activate, 1, Hr(), new ContractActionOptions("Renegotiated", null, true)), CancellationToken.None));

        Assert.Equal(ContractService.OverlapOverrideForbiddenCode, exception.Code);
        Assert.Empty(repository.Activations);
    }

    [Fact]
    public async Task Activate_OverrideWithoutReason_ThrowsValidationOnReason()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build(id: 41, status: ContractStatus.Active, contractNumber: "OLD", endDate: new DateOnly(2026, 12, 31)));
        repository.Add(Build(id: 42, status: ContractStatus.Executed, documentObjectKey: "key"));
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() =>
            service.TransitionAsync(Transition(ContractAction.Activate, 1, Approver(), new ContractActionOptions("  ", null, true)), CancellationToken.None));

        Assert.Contains("reason", exception.Errors.Keys);
        Assert.Empty(repository.Activations);
    }

    [Fact]
    public async Task Activate_OverrideByApproverWithReason_TerminatesPredecessorInSameSave()
    {
        var repository = new FakeContractRepository();
        var old = repository.Add(Build(id: 41, status: ContractStatus.Executed, contractNumber: "OLD", endDate: new DateOnly(2026, 12, 31), version: 2));
        repository.Add(Build(id: 42, status: ContractStatus.Executed, documentObjectKey: "key"));
        var service = CreateService(repository);

        var active = await service.TransitionAsync(
            Transition(ContractAction.Activate, 1, Approver(), new ContractActionOptions(" Renegotiated terms ", null, true)),
            CancellationToken.None);

        Assert.Equal(ContractStatus.Active, active.Status);
        var superseded = Assert.Single(repository.Activations).Superseded;
        Assert.NotNull(superseded);
        Assert.Same(old, superseded.Contract);
        Assert.Equal(ContractStatus.Terminated, old.Status);
        Assert.Equal(ContractStatus.Executed, superseded.PreviousStatus);
        Assert.Equal(2, superseded.ExpectedVersion);
        Assert.Equal("Renegotiated terms", superseded.OverrideReason);
    }

    [Fact]
    public async Task Activate_SecondaryContract_SkipsOverlapRule()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build(id: 41, status: ContractStatus.Active, contractNumber: "OLD", endDate: new DateOnly(2026, 12, 31)));
        repository.Add(Build(id: 42, status: ContractStatus.Executed, documentObjectKey: "key", isPrimary: false));
        var service = CreateService(repository);

        var active = await service.TransitionAsync(Transition(ContractAction.Activate, 1, Hr()), CancellationToken.None);

        Assert.Equal(ContractStatus.Active, active.Status);
        Assert.Equal(0, repository.FindOtherPrimaryCalls);
        Assert.Null(repository.Activations.Single().Superseded);
    }

    [Fact]
    public async Task Activate_ProbationContract_OpensReviewDueSevenDaysBeforeEndAssignedToManager()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build(status: ContractStatus.Executed, type: ContractType.Probation, documentObjectKey: "key",
            startDate: new DateOnly(2026, 10, 1), endDate: new DateOnly(2026, 11, 30)));
        var service = CreateService(repository);

        await service.TransitionAsync(Transition(ContractAction.Activate, 1, Hr()), CancellationToken.None);

        var review = Assert.Single(repository.Activations).ProbationReview;
        Assert.NotNull(review);
        Assert.Equal(new ProbationReviewDraft(EmployeeId, 42, new DateOnly(2026, 11, 23), ManagerUserId), review);
    }

    [Fact]
    public async Task Activate_ProbationContract_ManagerWithoutUser_LeavesReviewerNull()
    {
        var repository = new FakeContractRepository();
        repository.Employees[EmployeeId] = new ContractEmployee(EmployeeId, DepartmentId, ManagerUserId: null);
        repository.Add(Build(status: ContractStatus.Executed, type: ContractType.Probation, documentObjectKey: "key",
            startDate: new DateOnly(2026, 10, 1), endDate: new DateOnly(2026, 11, 30)));
        var service = CreateService(repository);

        await service.TransitionAsync(Transition(ContractAction.Activate, 1, Hr()), CancellationToken.None);

        Assert.Null(repository.Activations.Single().ProbationReview!.ReviewerUserId);
    }

    [Fact]
    public async Task Activate_LostRace_ThrowsConcurrency()
    {
        var repository = new FakeContractRepository { SaveSucceeds = false };
        repository.Add(Build(status: ContractStatus.Executed, documentObjectKey: "key"));
        var service = CreateService(repository);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() =>
            service.TransitionAsync(Transition(ContractAction.Activate, 1, Hr()), CancellationToken.None));

        Assert.Single(repository.Activations);
    }

    // ----- signed document -----

    [Fact]
    public async Task AttachSignedDocument_Approved_ScansStoresUnderContractKeyAndBecomesExecuted()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build(status: ContractStatus.Approved, version: 2));
        var storage = new FakeStorage();
        var scanner = new FakeScanner(MalwareScanResult.Clean);
        var service = CreateService(repository, storage, scanner);

        var updated = await service.AttachSignedDocumentAsync(new UploadSignedContractCommand(42, 2, Pdf(), Hr()), CancellationToken.None);

        Assert.Equal(ContractStatus.Executed, updated.Status);
        Assert.Equal(Now, updated.SignedAt);
        Assert.Equal(3, updated.Version);
        Assert.StartsWith("contracts/42/signed/", updated.DocumentObjectKey, StringComparison.Ordinal);
        Assert.Equal(1, scanner.Calls);
        var stored = Assert.Single(storage.Stored);
        Assert.Equal(updated.DocumentObjectKey, stored.ObjectKey);
        Assert.Equal("application/pdf", stored.ContentType);
        Assert.Equal(0, stored.PositionAtStore);
        Assert.Equal("%PDF-1.7", stored.Content);
        Assert.Equal([(42L, ContractStatus.Approved, 2L)], repository.SignedDocuments);
        Assert.Empty(storage.Deleted);
    }

    [Fact]
    public async Task AttachSignedDocument_ApprovedWhilePredecessorInForce_StoresButStaysApproved()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build(id: 41, status: ContractStatus.Active, contractNumber: "OLD"));
        repository.Add(Build(id: 42, status: ContractStatus.Approved));
        var service = CreateService(repository);

        var updated = await service.AttachSignedDocumentAsync(new UploadSignedContractCommand(42, 1, Pdf(), Hr()), CancellationToken.None);

        Assert.Equal(ContractStatus.Approved, updated.Status);
        Assert.True(updated.SignedDocumentAvailable);
    }

    [Theory]
    [InlineData(ContractStatus.Draft)]
    [InlineData(ContractStatus.Active)]
    [InlineData(ContractStatus.Cancelled)]
    public async Task AttachSignedDocument_WrongStatus_Throws409BeforeScanningOrStoring(ContractStatus status)
    {
        var repository = new FakeContractRepository();
        repository.Add(Build(status: status));
        var storage = new FakeStorage();
        var scanner = new FakeScanner(MalwareScanResult.Clean);
        var service = CreateService(repository, storage, scanner);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            service.AttachSignedDocumentAsync(new UploadSignedContractCommand(42, 1, Pdf(), Hr()), CancellationToken.None));

        Assert.Equal(Contract.SignedDocumentNotAllowedCode, exception.Code);
        Assert.Equal(0, scanner.Calls);
        Assert.Empty(storage.Stored);
    }

    [Fact]
    public async Task AttachSignedDocument_NonPdf_Throws422EvenThoughControllerFiltersFirst()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build(status: ContractStatus.Approved));
        var storage = new FakeStorage();
        var service = CreateService(repository, storage);

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() =>
            service.AttachSignedDocumentAsync(new UploadSignedContractCommand(42, 1, Pdf(contentType: "image/png"), Hr()), CancellationToken.None));

        Assert.Contains("file", exception.Errors.Keys);
        Assert.Empty(storage.Stored);
    }

    [Fact]
    public async Task AttachSignedDocument_Infected_Throws422AndStoresNothing()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build(status: ContractStatus.Approved));
        var storage = new FakeStorage();
        var service = CreateService(repository, storage, new FakeScanner(MalwareScanResult.Infected));

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() =>
            service.AttachSignedDocumentAsync(new UploadSignedContractCommand(42, 1, Pdf(), Hr()), CancellationToken.None));

        Assert.Equal(["The file failed the malware scan."], exception.Errors["file"]);
        Assert.Empty(storage.Stored);
        Assert.Empty(repository.SignedDocuments);
    }

    [Fact]
    public async Task AttachSignedDocument_ScannerUnavailable_Throws409ScanUnavailable()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build(status: ContractStatus.Approved));
        var storage = new FakeStorage();
        var service = CreateService(repository, storage, new FakeScanner(MalwareScanResult.Unavailable));

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            service.AttachSignedDocumentAsync(new UploadSignedContractCommand(42, 1, Pdf(), Hr()), CancellationToken.None));

        Assert.Equal(ContractService.ScanUnavailableCode, exception.Code);
        Assert.Empty(storage.Stored);
    }

    [Fact]
    public async Task AttachSignedDocument_SaveFails_DeletesStoredObjectAndRethrows()
    {
        var repository = new FakeContractRepository { SaveFailure = new InvalidOperationException("db down") };
        repository.Add(Build(status: ContractStatus.Approved));
        var storage = new FakeStorage();
        var service = CreateService(repository, storage);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.AttachSignedDocumentAsync(new UploadSignedContractCommand(42, 1, Pdf(), Hr()), CancellationToken.None));

        Assert.Equal([storage.Stored.Single().ObjectKey], storage.Deleted);
    }

    [Fact]
    public async Task AttachSignedDocument_LostRace_DeletesStoredObjectAndThrowsConcurrency()
    {
        var repository = new FakeContractRepository { SaveSucceeds = false };
        repository.Add(Build(status: ContractStatus.Approved));
        var storage = new FakeStorage();
        var service = CreateService(repository, storage);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() =>
            service.AttachSignedDocumentAsync(new UploadSignedContractCommand(42, 1, Pdf(), Hr()), CancellationToken.None));

        Assert.Single(storage.Deleted);
    }

    [Fact]
    public async Task AttachSignedDocument_WithoutWritePermission_ThrowsForbiddenBeforeScanning()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build(status: ContractStatus.Approved));
        var scanner = new FakeScanner(MalwareScanResult.Clean);
        var service = CreateService(repository, scanner: scanner);

        await Assert.ThrowsAsync<CoreHrForbiddenException>(() =>
            service.AttachSignedDocumentAsync(new UploadSignedContractCommand(42, 1, Pdf(), Hr(ContractPermissions.Read)), CancellationToken.None));

        Assert.Equal(0, scanner.Calls);
    }

    // ----- download url -----

    [Fact]
    public async Task CreateDownloadUrlAsync_SelfWithDocument_ExpiresInFifteenMinutesAndAudits()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build(status: ContractStatus.Active, documentObjectKey: "contracts/42/signed/abc"));
        var storage = new FakeStorage();
        var service = CreateService(repository, storage);

        var download = await service.CreateDownloadUrlAsync(42, Self(), CancellationToken.None);

        Assert.Equal(Now.AddMinutes(15), download.ExpiresAt);
        var signed = Assert.Single(storage.SignedUrls);
        Assert.Equal("contracts/42/signed/abc", signed.ObjectKey);
        Assert.Equal("contract-42-signed.pdf", signed.FileName);
        Assert.Equal(download.Url, signed.Url);
        Assert.Equal([(42L, Now, download.ExpiresAt)], repository.Downloads);
    }

    [Fact]
    public async Task CreateDownloadUrlAsync_NoDocument_Throws404ForTheDocument()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build(status: ContractStatus.Approved));
        var storage = new FakeStorage();
        var service = CreateService(repository, storage);

        var exception = await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            service.CreateDownloadUrlAsync(42, Hr(), CancellationToken.None));

        Assert.Equal(ContractService.DocumentResourceName, exception.Resource);
        Assert.Empty(storage.SignedUrls);
        Assert.Empty(repository.Downloads);
    }

    [Fact]
    public async Task CreateDownloadUrlAsync_OutOfScope_Throws404NotForbidden()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build(status: ContractStatus.Active, documentObjectKey: "key"));
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            service.CreateDownloadUrlAsync(42, Self(employeeId: 77), CancellationToken.None));

        Assert.Equal("Contract", exception.Resource);
    }

    // ----- expiring -----

    [Fact]
    public async Task ListExpiringAsync_SelfOnlyActor_ThrowsForbidden()
    {
        var service = CreateService(new FakeContractRepository());

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() =>
            service.ListExpiringAsync(new ExpiringContractsQuery(null, 45, PageRequest.Default), Self(), CancellationToken.None));

        Assert.Equal(ContractService.ExpiringForbiddenCode, exception.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(366)]
    public async Task ListExpiringAsync_WithinDaysOutOfRange_ThrowsValidation(int withinDays)
    {
        var service = CreateService(new FakeContractRepository());

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() =>
            service.ListExpiringAsync(new ExpiringContractsQuery(null, withinDays, PageRequest.Default), Hr(), CancellationToken.None));

        Assert.Contains("withinDays", exception.Errors.Keys);
    }

    [Fact]
    public async Task ListExpiringAsync_DefaultsAsOfToTodayAndPassesPolicyWindows()
    {
        var repository = new FakeContractRepository();
        var service = CreateService(repository);

        var view = await service.ListExpiringAsync(new ExpiringContractsQuery(null, 45, PageRequest.Default), Hr(), CancellationToken.None);

        Assert.Equal(Today, view.AsOf);
        Assert.NotNull(repository.LastExpiringSearch);
        Assert.Equal(Today, repository.LastExpiringSearch.Value.AsOf);
        Assert.Equal(ExpiryAlertPolicy.Windows(Today, 45), repository.LastExpiringSearch.Value.Windows);
    }

    [Fact]
    public async Task ListExpiringAsync_MapsDaysRemainingAndAlertLevelAndAppliesTypeThresholds()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build(id: 1, status: ContractStatus.Active, type: ContractType.Probation, contractNumber: "P-RED", startDate: Today.AddDays(-50), endDate: Today.AddDays(5)));
        repository.Add(Build(id: 2, status: ContractStatus.Active, type: ContractType.Probation, contractNumber: "P-AMBER", startDate: Today.AddDays(-50), endDate: Today.AddDays(12)));
        repository.Add(Build(id: 3, status: ContractStatus.Active, type: ContractType.Probation, contractNumber: "P-NONE", startDate: Today.AddDays(-40), endDate: Today.AddDays(20)));
        repository.Add(Build(id: 4, status: ContractStatus.Executed, type: ContractType.FixedTerm, contractNumber: "F-RED", startDate: Today.AddDays(-300), endDate: Today.AddDays(30)));
        repository.Add(Build(id: 5, status: ContractStatus.Active, type: ContractType.FixedTerm, contractNumber: "F-AMBER", startDate: Today.AddDays(-300), endDate: Today.AddDays(45)));
        repository.Add(Build(id: 6, status: ContractStatus.Active, type: ContractType.FixedTerm, contractNumber: "F-NONE", startDate: Today.AddDays(-300), endDate: Today.AddDays(46)));
        repository.Add(Build(id: 7, status: ContractStatus.Terminated, type: ContractType.FixedTerm, contractNumber: "F-ENDED", startDate: Today.AddDays(-300), endDate: Today.AddDays(10)));
        repository.Add(Build(id: 8, status: ContractStatus.Active, type: ContractType.Indefinite, indefinite: true, contractNumber: "INDEF"));
        var service = CreateService(repository);

        var view = await service.ListExpiringAsync(new ExpiringContractsQuery(Today, 365, PageRequest.Default), Hr(), CancellationToken.None);

        Assert.Equal([1L, 2L, 4L, 5L], view.Contracts.Items.Select(e => e.Contract.Id));
        Assert.Equal([5, 12, 30, 45], view.Contracts.Items.Select(e => e.DaysRemaining));
        Assert.Equal(
            [ExpiryAlertLevel.Red, ExpiryAlertLevel.Amber, ExpiryAlertLevel.Red, ExpiryAlertLevel.Amber],
            view.Contracts.Items.Select(e => e.AlertLevel));
        Assert.Equal(4, view.Contracts.TotalItems);
    }

    [Fact]
    public async Task ListExpiringAsync_ShortHorizon_ExcludesContractsBeyondIt()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build(id: 1, status: ContractStatus.Active, type: ContractType.FixedTerm, contractNumber: "A", startDate: Today.AddDays(-300), endDate: Today.AddDays(10)));
        repository.Add(Build(id: 2, status: ContractStatus.Active, type: ContractType.FixedTerm, contractNumber: "B", startDate: Today.AddDays(-300), endDate: Today.AddDays(11)));
        var service = CreateService(repository);

        var view = await service.ListExpiringAsync(new ExpiringContractsQuery(Today, 10, PageRequest.Default), Hr(), CancellationToken.None);

        Assert.Equal([1L], view.Contracts.Items.Select(e => e.Contract.Id));
    }

    [Fact]
    public async Task ListExpiringAsync_DepartmentScopedActor_SeesOnlyItsDepartment()
    {
        var repository = new FakeContractRepository();
        repository.Employees[11] = new ContractEmployee(11, OtherDepartmentId, null);
        repository.Add(Build(id: 1, status: ContractStatus.Active, contractNumber: "A", startDate: Today.AddDays(-300), endDate: Today.AddDays(10)));
        repository.Add(Build(id: 2, status: ContractStatus.Active, contractNumber: "B", employeeId: 11, startDate: Today.AddDays(-300), endDate: Today.AddDays(10)));
        var service = CreateService(repository);

        var view = await service.ListExpiringAsync(new ExpiringContractsQuery(Today, 45, PageRequest.Default), DepartmentReader(DepartmentId), CancellationToken.None);

        Assert.Equal([1L], view.Contracts.Items.Select(e => e.Contract.Id));
    }

    // ----- worker -----

    [Fact]
    public async Task ExpireDueContractsAsync_ExpiresEveryDueActiveContractAndReportsRaces()
    {
        var repository = new FakeContractRepository();
        repository.Add(Build(id: 1, status: ContractStatus.Active, contractNumber: "A", startDate: Today.AddDays(-400), endDate: Today.AddDays(-1), version: 3));
        repository.Add(Build(id: 2, status: ContractStatus.Active, contractNumber: "B", startDate: Today.AddDays(-400), endDate: Today.AddDays(-10), version: 1));
        repository.Add(Build(id: 3, status: ContractStatus.Active, contractNumber: "C", startDate: Today.AddDays(-400), endDate: Today, version: 1));
        repository.Add(Build(id: 4, status: ContractStatus.Executed, contractNumber: "D", startDate: Today.AddDays(-400), endDate: Today.AddDays(-5), version: 1));
        var service = CreateService(repository);
        var system = CoreHrActor.System("worker");

        var result = await service.ExpireDueContractsAsync(Today, system, CancellationToken.None);

        Assert.Equal(2, result.Expired);
        Assert.Empty(result.Conflicted);
        Assert.Equal(
            [(2L, ContractStatus.Active, ContractStatus.Expired, 1L, (string?)null), (1L, ContractStatus.Active, ContractStatus.Expired, 3L, null)],
            repository.Transitions);
        Assert.Equal(ContractStatus.Active, repository.Contracts.Single(c => c.Id == 3).Status);
        Assert.Equal(ContractStatus.Executed, repository.Contracts.Single(c => c.Id == 4).Status);
    }

    [Fact]
    public async Task ExpireDueContractsAsync_LostRace_IsReportedNotThrown()
    {
        var repository = new FakeContractRepository { SaveSucceeds = false };
        repository.Add(Build(id: 1, status: ContractStatus.Active, startDate: Today.AddDays(-400), endDate: Today.AddDays(-1)));
        var service = CreateService(repository);

        var result = await service.ExpireDueContractsAsync(Today, CoreHrActor.System("worker"), CancellationToken.None);

        Assert.Equal(0, result.Expired);
        Assert.Equal([1L], result.Conflicted);
    }

    private static ContractService CreateService(FakeContractRepository repository, FakeStorage? storage = null, FakeScanner? scanner = null)
    {
        storage ??= new FakeStorage();
        return new ContractService(repository, Uploader(storage, scanner), storage, new FixedTimeProvider(Now));
    }

    private static TransitionContractCommand Transition(ContractAction action, long expectedVersion, CoreHrActor actor, ContractActionOptions? options = null) =>
        new(42, action, expectedVersion, options ?? ContractActionOptions.None, actor);
}
