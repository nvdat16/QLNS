using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Qlns.BusinessLogic.Modules.Identity.Authentication;

namespace Qlns.DataAccess.Modules.Identity.Authentication;

/// <summary>
/// <see cref="IAccessTokenIssuer"/> adapter issuing HS256 JSON Web Tokens.
/// <para>
/// The claim set is the contract between this issuer and <c>CoreHrActorResolver</c> in the host: dropping or
/// renaming one of <see cref="UserIdClaim"/>, <see cref="EmployeeIdClaim"/>, <see cref="DataScopeClaim"/>,
/// <see cref="DepartmentIdClaim"/> or <see cref="PermissionClaim"/> would make every business endpoint reject
/// the tokens we mint. Repeated claims (permissions, departments, roles) are serialized as JSON arrays and
/// read back as repeated claims by the bearer handler.
/// </para>
/// A token issued while a password change is pending carries <see cref="PasswordChangeRequiredClaim"/>, a
/// shorter lifetime and no permission at all.
/// </summary>
public sealed class JwtAccessTokenIssuer(JwtAuthenticationOptions options) : IAccessTokenIssuer
{
    public const string UserIdClaim = "qlns_user_id";
    public const string EmployeeIdClaim = "qlns_employee_id";
    public const string DataScopeClaim = "data_scope";
    public const string DepartmentIdClaim = "department_id";
    public const string PermissionClaim = "permission";
    public const string RoleClaim = "qlns_role";
    public const string PasswordChangeRequiredClaim = "pwd_change_required";

    private readonly SymmetricSecurityKey _signingKey = new(Encoding.UTF8.GetBytes(options.SigningKey));
    private readonly JsonWebTokenHandler _handler = new();

    public IssuedAccessToken Issue(AuthenticatedIdentity identity, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(identity);

        var tokenId = Guid.CreateVersion7(now).ToString("n");
        var expiresAt = now + (identity.PasswordChangeRequired
            ? options.Settings.PasswordChangeTokenLifetime
            : options.Settings.AccessTokenLifetime);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, Text(identity.UserId)),
            new(JwtRegisteredClaimNames.Jti, tokenId),
            new(UserIdClaim, Text(identity.UserId)),
            new(DataScopeClaim, identity.DataScopeClaim),
            new(JwtRegisteredClaimNames.Email, identity.Email),
            new(JwtRegisteredClaimNames.Name, identity.DisplayName)
        };

        if (identity.EmployeeId is { } employeeId)
        {
            claims.Add(new Claim(EmployeeIdClaim, Text(employeeId)));
        }

        claims.AddRange(identity.DataScope.DepartmentIds.Select(id => new Claim(DepartmentIdClaim, Text(id))));
        claims.AddRange(identity.Permissions.Select(permission => new Claim(PermissionClaim, permission)));
        claims.AddRange(identity.Roles.Select(role => new Claim(RoleClaim, role)));

        if (identity.PasswordChangeRequired)
        {
            claims.Add(new Claim(PasswordChangeRequiredClaim, "true"));
        }

        var token = _handler.CreateToken(new SecurityTokenDescriptor
        {
            Issuer = options.Issuer,
            Audience = options.Audience,
            Subject = new ClaimsIdentity(claims),
            IssuedAt = now.UtcDateTime,
            NotBefore = now.UtcDateTime,
            Expires = expiresAt.UtcDateTime,
            SigningCredentials = new SigningCredentials(_signingKey, SecurityAlgorithms.HmacSha256)
        });

        return new IssuedAccessToken(token, tokenId, expiresAt);
    }

    private static string Text(long value) => value.ToString(CultureInfo.InvariantCulture);
}
