using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Identity.Shared;

namespace Qlns.BusinessLogic.Modules.Identity.Users;

public sealed record UserAccountSearchQuery(
    string? Search,
    UserAccountStatus? Status,
    string? RoleCode,
    PageRequest Page);

/// <summary>Fields an administrator may set on an account. E-mail is normalized before it reaches here.</summary>
public sealed record UserAccountWrite(string? Email, string? DisplayName);

public sealed record CreateUserAccountCommand(
    UserAccountWrite Write,
    string? InitialPassword,
    long? EmployeeId,
    IReadOnlyList<RoleGrant> Grants,
    CoreHrActor Actor,
    string CorrelationId);

public sealed record UpdateUserAccountCommand(
    long UserId,
    long ExpectedVersion,
    UserAccountWrite Write,
    CoreHrActor Actor);

public sealed record SetUserAccountStatusCommand(
    long UserId,
    long ExpectedVersion,
    UserAccountStatus Status,
    CoreHrActor Actor);

public sealed record ResetUserPasswordCommand(
    long UserId,
    string? NewPassword,
    CoreHrActor Actor);

public sealed record ReplaceRoleGrantsCommand(
    long UserId,
    long ExpectedVersion,
    IReadOnlyList<RoleGrant> Grants,
    CoreHrActor Actor);

/// <summary>A new account and its credential, as handed to the repository in one transaction.</summary>
public sealed record NewUserAccount(
    string ExternalSubject,
    string Email,
    string DisplayName,
    string PasswordHash,
    string PasswordAlgorithm,
    long? EmployeeId,
    IReadOnlyList<RoleGrant> Grants,
    DateTimeOffset CreatedAt);

/// <summary>Stable audit_logs.action values of account administration (ADM-02).</summary>
public static class UserAccountAuditActions
{
    public const string Create = "admin.user.create";
    public const string Update = "admin.user.update";
    public const string Enable = "admin.user.enable";
    public const string Disable = "admin.user.disable";
    public const string PasswordReset = "admin.user.password_reset";
    public const string RoleGrantsReplace = "admin.user.roles.replace";
    public const string EntityType = "user";
}
