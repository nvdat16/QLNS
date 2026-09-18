namespace Qlns.BusinessLogic.Modules.Identity.Authentication;

/// <summary>
/// Session lifetimes, bound from the <c>Authentication:Jwt</c> configuration section. Access tokens are
/// stateless and cannot be revoked, so their lifetime is the upper bound on how long a withdrawn role
/// stays usable — keep it short and let the refresh token carry the long-lived session.
/// </summary>
public sealed record AuthenticationSettings
{
    public static readonly TimeSpan MaxAccessTokenLifetime = TimeSpan.FromHours(2);

    public TimeSpan AccessTokenLifetime { get; init; } = TimeSpan.FromMinutes(30);

    /// <summary>Lifetime of the restricted token issued while must_change_password is set.</summary>
    public TimeSpan PasswordChangeTokenLifetime { get; init; } = TimeSpan.FromMinutes(10);

    public TimeSpan RefreshTokenLifetime { get; init; } = TimeSpan.FromDays(14);

    public void Validate()
    {
        Require(AccessTokenLifetime, nameof(AccessTokenLifetime), MaxAccessTokenLifetime);
        Require(PasswordChangeTokenLifetime, nameof(PasswordChangeTokenLifetime), MaxAccessTokenLifetime);
        Require(RefreshTokenLifetime, nameof(RefreshTokenLifetime), TimeSpan.FromDays(90));

        if (RefreshTokenLifetime <= AccessTokenLifetime)
        {
            throw new InvalidOperationException("RefreshTokenLifetime must be longer than AccessTokenLifetime.");
        }
    }

    private static void Require(TimeSpan value, string name, TimeSpan maximum)
    {
        if (value <= TimeSpan.Zero || value > maximum)
        {
            throw new InvalidOperationException($"Authentication:Jwt:{name} must be between 1 second and {maximum}.");
        }
    }
}
