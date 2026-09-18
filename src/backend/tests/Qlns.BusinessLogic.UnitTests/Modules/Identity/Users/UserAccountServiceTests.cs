using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Identity.Shared;
using Qlns.BusinessLogic.Modules.Identity.Users;
using Xunit;
using static Qlns.BusinessLogic.UnitTests.Modules.Identity.IdentityTestData;

namespace Qlns.BusinessLogic.UnitTests.Modules.Identity.Users;

public sealed class UserAccountServiceTests
{
    private const string Read = IdentityPermissions.UserRead;
    private const string Manage = IdentityPermissions.UserManage;

    [Fact]
    public async Task SearchAsync_WithoutReadPermission_IsForbidden()
    {
        var service = CreateService(new FakeUserAccountRepository(UserAccountTestData.View()));

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() => service.SearchAsync(
            Query(), Actor(userId: 8), CancellationToken.None));

        Assert.Equal(UserAccountService.ReadForbiddenCode, exception.Code);
    }

    [Fact]
    public async Task SearchAsync_ManagePermissionAloneIsEnoughToRead()
    {
        var service = CreateService(new FakeUserAccountRepository(UserAccountTestData.View()));

        var result = await service.SearchAsync(Query(), Actor(userId: 8, Manage), CancellationToken.None);

        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetAsync_UnknownAccount_ThrowsNotFound()
    {
        var service = CreateService(new FakeUserAccountRepository(UserAccountTestData.View(id: 7)));

        await Assert.ThrowsAsync<CoreHrNotFoundException>(() => service.GetAsync(
            99, Actor(userId: 8, Read), CancellationToken.None));
    }

    [Fact]
    public async Task CreateAsync_ValidRequest_HashesPasswordAndPersistsGrants()
    {
        var repository = new FakeUserAccountRepository();
        var service = CreateService(repository);

        var view = await service.CreateAsync(
            new CreateUserAccountCommand(
                new UserAccountWrite("  New.User@QLNS.local ", "  Nguyen Van New  "),
                "Initial-Secret-7",
                EmployeeId: null,
                [RoleGrant.Department("ROLE_LINE_MGR", 2)],
                Actor(userId: 8, Manage),
                "test-correlation"),
            CancellationToken.None);

        var created = Assert.Single(repository.Created);
        Assert.Equal("new.user@qlns.local", created.Email);
        Assert.Equal("Nguyen Van New", created.DisplayName);
        Assert.Equal("local|new.user@qlns.local", created.ExternalSubject);
        Assert.Equal(FakePasswordHasher.HashOf("Initial-Secret-7"), created.PasswordHash);
        Assert.Equal(FakePasswordHasher.AlgorithmName, created.PasswordAlgorithm);
        Assert.Equal(42, view.Account.Id);
    }

    [Fact]
    public async Task CreateAsync_WithoutManagePermission_IsForbidden()
    {
        var service = CreateService(new FakeUserAccountRepository());

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() => service.CreateAsync(
            CreateCommand(Actor(userId: 8, Read)), CancellationToken.None));

        Assert.Equal(UserAccountService.ManageForbiddenCode, exception.Code);
    }

    [Fact]
    public async Task CreateAsync_WeakInitialPassword_IsRejected()
    {
        var repository = new FakeUserAccountRepository();
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() => service.CreateAsync(
            CreateCommand(Actor(userId: 8, Manage), password: "new.user"), CancellationToken.None));

        Assert.Contains("initialPassword", exception.Errors);
        Assert.Empty(repository.Created);
    }

    [Fact]
    public async Task CreateAsync_MalformedEmail_IsRejected()
    {
        var repository = new FakeUserAccountRepository();
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() => service.CreateAsync(
            CreateCommand(Actor(userId: 8, Manage), email: "not-an-email"), CancellationToken.None));

        Assert.Contains("email", exception.Errors);
        Assert.Empty(repository.Created);
    }

    [Fact]
    public async Task CreateAsync_EmailAlreadyUsed_ThrowsBusinessRule()
    {
        var repository = new FakeUserAccountRepository { EmailTaken = true };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() => service.CreateAsync(
            CreateCommand(Actor(userId: 8, Manage)), CancellationToken.None));

        Assert.Equal(UserAccountService.EmailTakenCode, exception.Code);
        Assert.Empty(repository.Created);
    }

    [Fact]
    public async Task CreateAsync_NonAssignableRole_ThrowsBusinessRuleListingIt()
    {
        var repository = new FakeUserAccountRepository();
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() => service.CreateAsync(
            CreateCommand(Actor(userId: 8, Manage), grants: [RoleGrant.Organization("ROLE_LEGACY")]),
            CancellationToken.None));

        Assert.Equal(UserAccountService.UnknownRoleCode, exception.Code);
        Assert.Equal(new[] { "ROLE_LEGACY" }, Assert.IsType<List<string>>(exception.Details["unknownRoles"]));
        Assert.Empty(repository.Created);
    }

    [Fact]
    public async Task CreateAsync_DepartmentGrantWithoutDepartmentId_IsRejected()
    {
        var repository = new FakeUserAccountRepository();
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() => service.CreateAsync(
            CreateCommand(Actor(userId: 8, Manage), grants: [new RoleGrant("ROLE_LINE_MGR", DataScopeType.Department, 0)]),
            CancellationToken.None));

        Assert.Contains("roles", exception.Errors);
        Assert.Empty(repository.Created);
    }

    [Fact]
    public async Task CreateAsync_DuplicateGrant_IsRejected()
    {
        var repository = new FakeUserAccountRepository();
        var service = CreateService(repository);

        await Assert.ThrowsAsync<CoreHrValidationException>(() => service.CreateAsync(
            CreateCommand(
                Actor(userId: 8, Manage),
                grants: [RoleGrant.Organization("ROLE_HR_MGR"), RoleGrant.Organization("ROLE_HR_MGR")]),
            CancellationToken.None));

        Assert.Empty(repository.Created);
    }

    [Fact]
    public async Task CreateAsync_UnknownDepartment_ThrowsBusinessRule()
    {
        var repository = new FakeUserAccountRepository { MissingDepartments = [99] };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() => service.CreateAsync(
            CreateCommand(Actor(userId: 8, Manage), grants: [RoleGrant.Department("ROLE_LINE_MGR", 99)]),
            CancellationToken.None));

        Assert.Equal(UserAccountService.UnknownRoleCode, exception.Code);
        Assert.Empty(repository.Created);
    }

    [Fact]
    public async Task CreateAsync_EmployeeLinkedToAnotherAccount_ThrowsBusinessRule()
    {
        var repository = new FakeUserAccountRepository { EmployeeLink = new EmployeeLink(4, 3) };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() => service.CreateAsync(
            CreateCommand(Actor(userId: 8, Manage), employeeId: 4), CancellationToken.None));

        Assert.Equal(UserAccountService.EmployeeAlreadyLinkedCode, exception.Code);
    }

    [Fact]
    public async Task CreateAsync_UnknownEmployee_IsRejected()
    {
        var repository = new FakeUserAccountRepository { EmployeeLink = null };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() => service.CreateAsync(
            CreateCommand(Actor(userId: 8, Manage), employeeId: 404), CancellationToken.None));

        Assert.Contains("employeeId", exception.Errors);
    }

    [Fact]
    public async Task UpdateAsync_ChangedFieldsOnly_ArePassedToTheRepository()
    {
        var repository = new FakeUserAccountRepository(UserAccountTestData.View(id: 7, version: 3));
        var service = CreateService(repository);

        await service.UpdateAsync(
            new UpdateUserAccountCommand(7, 3, new UserAccountWrite("new.user@qlns.local", "Renamed"), Actor(userId: 8, Manage)),
            CancellationToken.None);

        var update = Assert.Single(repository.Updates);
        Assert.Equal(new[] { "displayName" }, update.Changed);
    }

    [Fact]
    public async Task UpdateAsync_NothingChanged_DoesNotWrite()
    {
        var repository = new FakeUserAccountRepository(UserAccountTestData.View(id: 7, version: 3));
        var service = CreateService(repository);

        await service.UpdateAsync(
            new UpdateUserAccountCommand(
                7, 3, new UserAccountWrite("new.user@qlns.local", "Nguyen Van New"), Actor(userId: 8, Manage)),
            CancellationToken.None);

        Assert.Empty(repository.Updates);
    }

    [Fact]
    public async Task UpdateAsync_StaleVersion_ThrowsConcurrencyWithoutWriting()
    {
        var repository = new FakeUserAccountRepository(UserAccountTestData.View(id: 7, version: 5));
        var service = CreateService(repository);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => service.UpdateAsync(
            new UpdateUserAccountCommand(7, 3, new UserAccountWrite("other@qlns.local", "Renamed"), Actor(userId: 8, Manage)),
            CancellationToken.None));

        Assert.Empty(repository.Updates);
    }

    [Fact]
    public async Task SetStatusAsync_DisablingAnotherAccount_IsRecorded()
    {
        var repository = new FakeUserAccountRepository(UserAccountTestData.View(id: 7, version: 3));
        var service = CreateService(repository);

        await service.SetStatusAsync(
            new SetUserAccountStatusCommand(7, 3, UserAccountStatus.Disabled, Actor(userId: 8, Manage)),
            CancellationToken.None);

        Assert.Equal((7L, UserAccountStatus.Disabled), Assert.Single(repository.StatusChanges));
    }

    [Fact]
    public async Task SetStatusAsync_AlreadyInRequestedState_IsIdempotentAndIgnoresTheEtag()
    {
        var repository = new FakeUserAccountRepository(UserAccountTestData.View(id: 7, version: 3));
        var service = CreateService(repository);

        var view = await service.SetStatusAsync(
            new SetUserAccountStatusCommand(7, 1, UserAccountStatus.Active, Actor(userId: 8, Manage)),
            CancellationToken.None);

        Assert.Equal(UserAccountStatus.Active, view.Account.Status);
        Assert.Empty(repository.StatusChanges);
    }

    [Fact]
    public async Task SetStatusAsync_OwnAccount_IsForbidden()
    {
        var repository = new FakeUserAccountRepository(UserAccountTestData.View(id: 8, version: 3));
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() => service.SetStatusAsync(
            new SetUserAccountStatusCommand(8, 3, UserAccountStatus.Disabled, Actor(userId: 8, Manage)),
            CancellationToken.None));

        Assert.Equal(UserAccountService.SelfServiceForbiddenCode, exception.Code);
        Assert.Empty(repository.StatusChanges);
    }

    [Fact]
    public async Task ReplaceRoleGrantsAsync_OwnAccount_IsForbidden()
    {
        var repository = new FakeUserAccountRepository(UserAccountTestData.View(id: 8, version: 3));
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() => service.ReplaceRoleGrantsAsync(
            new ReplaceRoleGrantsCommand(8, 3, [RoleGrant.Organization("ROLE_HR_MGR")], Actor(userId: 8, Manage)),
            CancellationToken.None));

        Assert.Equal(UserAccountService.SelfServiceForbiddenCode, exception.Code);
        Assert.Empty(repository.GrantReplacements);
    }

    [Fact]
    public async Task ReplaceRoleGrantsAsync_EmptyList_LeavesTheAccountWithoutAuthority()
    {
        var repository = new FakeUserAccountRepository(UserAccountTestData.View(id: 7, version: 3));
        var service = CreateService(repository);

        await service.ReplaceRoleGrantsAsync(
            new ReplaceRoleGrantsCommand(7, 3, [], Actor(userId: 8, Manage)), CancellationToken.None);

        Assert.Empty(Assert.Single(repository.GrantReplacements).Grants);
    }

    [Fact]
    public async Task ReplaceRoleGrantsAsync_StaleVersion_ThrowsBeforeValidatingRoles()
    {
        var repository = new FakeUserAccountRepository(UserAccountTestData.View(id: 7, version: 9));
        var service = CreateService(repository);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => service.ReplaceRoleGrantsAsync(
            new ReplaceRoleGrantsCommand(7, 3, [RoleGrant.Organization("ROLE_LEGACY")], Actor(userId: 8, Manage)),
            CancellationToken.None));

        Assert.Empty(repository.GrantReplacements);
    }

    [Fact]
    public async Task ResetPasswordAsync_OwnAccount_IsForbidden()
    {
        var repository = new FakeUserAccountRepository(UserAccountTestData.View(id: 8));
        var service = CreateService(repository);

        await Assert.ThrowsAsync<CoreHrForbiddenException>(() => service.ResetPasswordAsync(
            new ResetUserPasswordCommand(8, "Reset-Secret-2", Actor(userId: 8, Manage)), CancellationToken.None));

        Assert.Empty(repository.PasswordResets);
    }

    [Fact]
    public async Task ResetPasswordAsync_ValidSecret_StoresItsHash()
    {
        var repository = new FakeUserAccountRepository(UserAccountTestData.View(id: 7));
        var service = CreateService(repository);

        await service.ResetPasswordAsync(
            new ResetUserPasswordCommand(7, "Reset-Secret-2", Actor(userId: 8, Manage)), CancellationToken.None);

        Assert.Equal((7L, FakePasswordHasher.HashOf("Reset-Secret-2")), Assert.Single(repository.PasswordResets));
    }

    [Fact]
    public async Task ResetPasswordAsync_WeakSecret_IsRejected()
    {
        var repository = new FakeUserAccountRepository(UserAccountTestData.View(id: 7));
        var service = CreateService(repository);

        await Assert.ThrowsAsync<CoreHrValidationException>(() => service.ResetPasswordAsync(
            new ResetUserPasswordCommand(7, "weak", Actor(userId: 8, Manage)), CancellationToken.None));

        Assert.Empty(repository.PasswordResets);
    }

    [Fact]
    public async Task GetRoleCatalogAsync_WithoutRoleReadPermission_IsForbidden()
    {
        var service = CreateService(new FakeUserAccountRepository());

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() => service.GetRoleCatalogAsync(
            Actor(userId: 8, Read), CancellationToken.None));

        Assert.Equal(UserAccountService.RoleReadForbiddenCode, exception.Code);
    }

    [Fact]
    public async Task GetRoleCatalogAsync_WithRoleRead_ReturnsEveryRole()
    {
        var service = CreateService(new FakeUserAccountRepository());

        var catalogue = await service.GetRoleCatalogAsync(
            Actor(userId: 8, IdentityPermissions.RoleRead), CancellationToken.None);

        Assert.Equal(3, catalogue.Count);
    }

    private static UserAccountSearchQuery Query() => new(null, null, null, PageRequest.Default);

    private static CreateUserAccountCommand CreateCommand(
        CoreHrActor actor,
        string email = "new.user@qlns.local",
        string password = "Initial-Secret-7",
        long? employeeId = null,
        IReadOnlyList<RoleGrant>? grants = null) =>
        new(
            new UserAccountWrite(email, "Nguyen Van New"),
            password,
            employeeId,
            grants ?? [RoleGrant.Organization("ROLE_HR_MGR")],
            actor,
            "test-correlation");

    private static UserAccountService CreateService(FakeUserAccountRepository repository) =>
        new(repository, new FakePasswordHasher(), new FixedTimeProvider(Now));
}
