namespace Qlns.DataAccess.Modules.Identity.Shared;

// Persistence models of the Identity & Access tables of database/schema.sql (baseline v1.2). The users
// row itself is mapped by CoreHr.Shared.UserEntity, which predates this module and stays shared.
// Column names are mapped in IdentityEntityConfigurations.

public sealed class UserCredentialEntity
{
    public long UserId { get; set; }
    /// <summary>Self-describing hash: <c>pbkdf2-sha512$&lt;iterations&gt;$&lt;salt&gt;$&lt;hash&gt;</c>.</summary>
    public string PasswordHash { get; set; } = null!;
    public string PasswordAlgorithm { get; set; } = null!;
    public bool MustChangePassword { get; set; }
    public DateTimeOffset PasswordUpdatedAt { get; set; }
    public int FailedAttempts { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
    public DateTimeOffset? LastLoginAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long Version { get; set; }
}

public sealed class RoleEntity
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsAssignable { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class RolePermissionEntity
{
    public string RoleCode { get; set; } = null!;
    public string Permission { get; set; } = null!;
}

public sealed class UserRoleEntity
{
    public long UserId { get; set; }
    public string RoleCode { get; set; } = null!;
    public string DataScopeType { get; set; } = null!;
    public long DataScopeId { get; set; }
    public DateTimeOffset GrantedAt { get; set; }
    public long? GrantedBy { get; set; }
}

public sealed class RefreshTokenEntity
{
    public long Id { get; set; }
    public long UserId { get; set; }
    /// <summary>SHA-256 of the opaque token, 64 lower-case hex characters. The token is never stored.</summary>
    public string TokenHash { get; set; } = null!;
    public DateTimeOffset IssuedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevokedReason { get; set; }
    public long? ReplacedByTokenId { get; set; }
    public string? ClientIp { get; set; }
    public string? UserAgent { get; set; }
}
