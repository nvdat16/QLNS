using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Identity.Authentication;
using Qlns.BusinessLogic.Modules.Identity.Shared;

namespace Qlns.BusinessLogic.Modules.Identity.Users;

/// <summary>
/// ADM-02: provisioning accounts, granting roles, disabling access and resetting passwords.
/// <para>
/// The service is the only place that may widen someone's authority, so it deliberately refuses to let an
/// administrator act on their own account (<see cref="SelfServiceForbiddenCode"/>): disabling yourself, or
/// rewriting your own grants, is either an accident that locks the organization out of administration or an
/// attempt to escalate without a second pair of eyes. Everything else is validated against the role
/// catalogue — an unknown or non-assignable role code, or a department that does not exist, is rejected
/// before anything is written.
/// </para>
/// </summary>
public sealed class UserAccountService(
    IUserAccountRepository repository,
    IPasswordHasher passwordHasher,
    TimeProvider timeProvider)
{
    public const string ResourceName = "User account";
    public const string ReadForbiddenCode = "admin.user.read_forbidden";
    public const string ManageForbiddenCode = "admin.user.manage_forbidden";
    public const string RoleReadForbiddenCode = "admin.role.read_forbidden";
    public const string SelfServiceForbiddenCode = "admin.user.self_management_forbidden";
    public const string EmailTakenCode = "admin.user.email_taken";
    public const string EmployeeAlreadyLinkedCode = "admin.user.employee_already_linked";
    public const string UnknownRoleCode = "admin.user.unknown_role";
    private const string ConflictResource = "user account";

    public async Task<PagedResult<UserAccountView>> SearchAsync(
        UserAccountSearchQuery query,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        RequireRead(actor);

        return await repository.SearchAsync(query, cancellationToken);
    }

    public async Task<UserAccountView> GetAsync(long userId, CoreHrActor actor, CancellationToken cancellationToken)
    {
        RequireRead(actor);

        return await repository.GetAsync(userId, cancellationToken)
            ?? throw new CoreHrNotFoundException(ResourceName, userId);
    }

    public async Task<IReadOnlyList<RoleCatalogEntry>> GetRoleCatalogAsync(CoreHrActor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (!actor.HasPermission(IdentityPermissions.RoleRead) && !actor.HasPermission(IdentityPermissions.UserManage))
        {
            throw new CoreHrForbiddenException(
                RoleReadForbiddenCode,
                $"Reading the role catalogue requires the {IdentityPermissions.RoleRead} permission.");
        }

        return await repository.GetRoleCatalogAsync(cancellationToken);
    }

    public async Task<UserAccountView> CreateAsync(CreateUserAccountCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        RequireManage(command.Actor);

        var now = timeProvider.GetUtcNow();
        var email = EmailAddress.Require(command.Write.Email, "email");
        var displayName = RequireDisplayName(command.Write.DisplayName);
        PasswordPolicy.Validate(command.InitialPassword, "initialPassword", email);
        await RequireAssignableGrantsAsync(command.Grants, cancellationToken);

        if (await repository.EmailTakenAsync(email, exceptUserId: null, cancellationToken))
        {
            throw new CoreHrBusinessRuleException(EmailTakenCode, $"Another account already uses the e-mail {email}.");
        }

        if (command.EmployeeId is { } employeeId)
        {
            await RequireLinkableEmployeeAsync(employeeId, expectedUserId: null, cancellationToken);
        }

        var userId = await repository.CreateAsync(
            new NewUserAccount(
                UserAccount.LocalSubject(email),
                email,
                displayName,
                passwordHasher.Hash(command.InitialPassword!),
                passwordHasher.Algorithm,
                command.EmployeeId,
                command.Grants,
                now),
            command.Actor,
            cancellationToken);

        return await repository.GetAsync(userId, cancellationToken)
            ?? throw new CoreHrNotFoundException(ResourceName, userId);
    }

    public async Task<UserAccountView> UpdateAsync(UpdateUserAccountCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        RequireManage(command.Actor);

        var now = timeProvider.GetUtcNow();
        var current = await LoadAsync(command.UserId, cancellationToken);
        var email = EmailAddress.Require(command.Write.Email, "email");
        var displayName = RequireDisplayName(command.Write.DisplayName);

        RequireVersion(current, command.ExpectedVersion);

        if (!string.Equals(email, current.Account.Email, StringComparison.Ordinal) &&
            await repository.EmailTakenAsync(email, command.UserId, cancellationToken))
        {
            throw new CoreHrBusinessRuleException(EmailTakenCode, $"Another account already uses the e-mail {email}.");
        }

        var changedFields = new List<string>();
        if (!string.Equals(email, current.Account.Email, StringComparison.Ordinal))
        {
            changedFields.Add("email");
        }

        if (!string.Equals(displayName, current.Account.DisplayName, StringComparison.Ordinal))
        {
            changedFields.Add("displayName");
        }

        if (changedFields.Count == 0)
        {
            return current;
        }

        if (!await repository.UpdateAsync(
                command.UserId, command.ExpectedVersion, email, displayName, changedFields, command.Actor, now, cancellationToken))
        {
            throw new CoreHrConcurrencyConflictException(ConflictResource);
        }

        return await LoadAsync(command.UserId, cancellationToken);
    }

    /// <summary>
    /// Enables or disables an account. Idempotent: an account already in the requested state is returned
    /// unchanged, whatever ETag the retry carries.
    /// </summary>
    public async Task<UserAccountView> SetStatusAsync(SetUserAccountStatusCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        RequireManage(command.Actor);
        RequireNotSelf(command.Actor, command.UserId, "change the status of");

        var now = timeProvider.GetUtcNow();
        var current = await LoadAsync(command.UserId, cancellationToken);
        if (current.Account.Status == command.Status)
        {
            return current;
        }

        RequireVersion(current, command.ExpectedVersion);

        if (!await repository.SetStatusAsync(
                command.UserId, command.ExpectedVersion, command.Status, command.Actor, now, cancellationToken))
        {
            throw new CoreHrConcurrencyConflictException(ConflictResource);
        }

        return await LoadAsync(command.UserId, cancellationToken);
    }

    /// <summary>
    /// Administrative password reset. The new secret is communicated out of band; the account must change it
    /// at the next sign-in and every existing session of that account is dropped.
    /// </summary>
    public async Task<UserAccountView> ResetPasswordAsync(ResetUserPasswordCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        RequireManage(command.Actor);
        RequireNotSelf(command.Actor, command.UserId, "reset the password of");

        var now = timeProvider.GetUtcNow();
        var current = await LoadAsync(command.UserId, cancellationToken);
        PasswordPolicy.Validate(command.NewPassword, "newPassword", current.Account.Email);

        if (!await repository.ResetPasswordAsync(
                command.UserId,
                passwordHasher.Hash(command.NewPassword!),
                passwordHasher.Algorithm,
                command.Actor,
                now,
                cancellationToken))
        {
            throw new CoreHrNotFoundException(ResourceName, command.UserId);
        }

        return await LoadAsync(command.UserId, cancellationToken);
    }

    /// <summary>Replaces every role grant of the account; an empty list leaves the account without authority.</summary>
    public async Task<UserAccountView> ReplaceRoleGrantsAsync(ReplaceRoleGrantsCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        RequireManage(command.Actor);
        RequireNotSelf(command.Actor, command.UserId, "change the role grants of");

        var now = timeProvider.GetUtcNow();
        var current = await LoadAsync(command.UserId, cancellationToken);
        RequireVersion(current, command.ExpectedVersion);
        await RequireAssignableGrantsAsync(command.Grants, cancellationToken);

        if (!await repository.ReplaceRoleGrantsAsync(
                command.UserId, command.ExpectedVersion, command.Grants, command.Actor, now, cancellationToken))
        {
            throw new CoreHrConcurrencyConflictException(ConflictResource);
        }

        return await LoadAsync(command.UserId, cancellationToken);
    }

    private async Task<UserAccountView> LoadAsync(long userId, CancellationToken cancellationToken) =>
        await repository.GetAsync(userId, cancellationToken)
            ?? throw new CoreHrNotFoundException(ResourceName, userId);

    private async Task RequireAssignableGrantsAsync(IReadOnlyList<RoleGrant> grants, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(grants);

        var errors = new ValidationErrors();
        var duplicates = grants
            .GroupBy(grant => (grant.RoleCode, grant.ScopeType, grant.ScopeId))
            .Where(group => group.Count() > 1)
            .Select(group => group.Key.RoleCode)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        foreach (var duplicate in duplicates)
        {
            errors.Add("roles", $"Role {duplicate} is granted more than once with the same scope.");
        }

        foreach (var grant in grants.Where(grant => !grant.IsWellFormed))
        {
            errors.Add("roles", grant.ScopeType == DataScopeType.Department
                ? $"Role {grant.RoleCode} with department scope requires a positive dataScopeId."
                : $"Role {grant.RoleCode} with {grant.ScopeType.ToContract()} scope must not carry a dataScopeId.");
        }

        errors.ThrowIfAny();

        if (grants.Count == 0)
        {
            return;
        }

        var catalogue = await repository.GetRoleCatalogAsync(cancellationToken);
        var assignable = catalogue
            .Where(entry => entry.IsAssignable)
            .Select(entry => entry.Code)
            .ToHashSet(StringComparer.Ordinal);

        var unknown = grants
            .Select(grant => grant.RoleCode)
            .Where(code => !assignable.Contains(code))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (unknown.Count > 0)
        {
            throw new CoreHrBusinessRuleException(
                UnknownRoleCode,
                "One or more role codes are unknown or not assignable.")
            {
                Details = new Dictionary<string, object?> { ["unknownRoles"] = unknown }
            };
        }

        var departmentIds = grants
            .Where(grant => grant.ScopeType == DataScopeType.Department)
            .Select(grant => grant.ScopeId)
            .Distinct()
            .ToList();

        if (departmentIds.Count == 0)
        {
            return;
        }

        var missing = await repository.FindMissingDepartmentsAsync(departmentIds, cancellationToken);
        if (missing.Count > 0)
        {
            throw new CoreHrBusinessRuleException(
                UnknownRoleCode,
                "One or more role grants point at a department that does not exist.")
            {
                Details = new Dictionary<string, object?> { ["unknownDepartments"] = missing }
            };
        }
    }

    private async Task RequireLinkableEmployeeAsync(long employeeId, long? expectedUserId, CancellationToken cancellationToken)
    {
        var link = await repository.GetEmployeeLinkAsync(employeeId, cancellationToken)
            ?? throw CoreHrValidationException.For("employeeId", $"Employee {employeeId} does not exist.");

        if (link.UserId is { } linkedUserId && linkedUserId != expectedUserId)
        {
            throw new CoreHrBusinessRuleException(
                EmployeeAlreadyLinkedCode,
                $"Employee {employeeId} is already linked to user account {linkedUserId}.");
        }
    }

    private static string RequireDisplayName(string? displayName)
    {
        var trimmed = displayName?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            throw CoreHrValidationException.For("displayName", "displayName is required.");
        }

        if (trimmed.Length > UserAccount.DisplayNameMaxLength)
        {
            throw CoreHrValidationException.For(
                "displayName", $"displayName must be at most {UserAccount.DisplayNameMaxLength} characters long.");
        }

        return trimmed;
    }

    private static void RequireRead(CoreHrActor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (!actor.HasPermission(IdentityPermissions.UserRead) && !actor.HasPermission(IdentityPermissions.UserManage))
        {
            throw new CoreHrForbiddenException(
                ReadForbiddenCode,
                $"Reading user accounts requires the {IdentityPermissions.UserRead} permission.");
        }
    }

    private static void RequireManage(CoreHrActor actor)
    {
        ArgumentNullException.ThrowIfNull(actor);

        if (!actor.HasPermission(IdentityPermissions.UserManage))
        {
            throw new CoreHrForbiddenException(
                ManageForbiddenCode,
                $"Managing user accounts requires the {IdentityPermissions.UserManage} permission.");
        }
    }

    private static void RequireNotSelf(CoreHrActor actor, long userId, string action)
    {
        if (actor.UserId == userId)
        {
            throw new CoreHrForbiddenException(
                SelfServiceForbiddenCode,
                $"An administrator may not {action} their own account. Ask another administrator.");
        }
    }

    private static void RequireVersion(UserAccountView current, long expectedVersion)
    {
        if (current.Account.Version != expectedVersion)
        {
            throw new CoreHrConcurrencyConflictException(ConflictResource);
        }
    }
}
