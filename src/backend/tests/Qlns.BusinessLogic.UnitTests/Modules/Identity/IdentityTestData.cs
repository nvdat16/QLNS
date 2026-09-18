using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Identity.Authentication;
using Qlns.BusinessLogic.Modules.Identity.Shared;

namespace Qlns.BusinessLogic.UnitTests.Modules.Identity;

/// <summary>
/// Fixtures and test doubles of the Identity &amp; Access module. The fake hasher treats a hash as
/// <c>hash:{password}</c>, which keeps the assertions about *which* secret was stored readable without
/// running PBKDF2 in every test.
/// </summary>
internal static class IdentityTestData
{
    public const string Password = "Correct-Horse-9";
    public const string Email = "hr.manager@qlns.local";

    public static readonly DateTimeOffset Now = new(2026, 9, 18, 8, 30, 0, TimeSpan.Zero);

    public static StoredCredential Credential(
        string password = Password,
        bool mustChangePassword = false,
        int failedAttempts = 0,
        DateTimeOffset? lockedUntil = null,
        long version = 3) =>
        new(FakePasswordHasher.HashOf(password), FakePasswordHasher.AlgorithmName, mustChangePassword, failedAttempts, lockedUntil, version);

    public static UserSignInRecord Record(
        StoredCredential? credential = null,
        string status = UserSignInRecord.ActiveStatus,
        long userId = 1,
        long? employeeId = 2) =>
        new(userId, Email, "Tran Thi HR Manager", status, employeeId, credential ?? Credential());

    public static IdentityAuthorization Authorization() => new(
        [RoleGrant.Department("ROLE_LINE_MGR", 2), RoleGrant.Department("ROLE_LINE_MGR", 4)],
        new HashSet<string>(StringComparer.Ordinal) { "corehr.employee.read", "corehr.probation.read" });

    public static CoreHrActor Actor(long userId = 1, params string[] permissions) =>
        new(userId, 2, CoreHrDataScope.Organization, permissions.ToHashSet(StringComparer.Ordinal), "test-correlation");

    public static SignInContext Context() => new("10.0.0.7", "xunit", "test-correlation");

    internal sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }

    /// <summary>Reversible stand-in for PBKDF2: the "hash" is the password with a prefix.</summary>
    internal sealed class FakePasswordHasher : IPasswordHasher
    {
        public const string AlgorithmName = "fake";
        private const string Prefix = "hash:";

        public int VerifyCalls { get; private set; }
        public int DummyVerifyCalls { get; private set; }

        public string Algorithm => AlgorithmName;

        public string DummyHash => HashOf("dummy-secret-nobody-knows");

        public static string HashOf(string password) => Prefix + password;

        public string Hash(string password) => HashOf(password);

        public PasswordVerification Verify(string encodedHash, string password)
        {
            VerifyCalls++;
            if (string.Equals(encodedHash, DummyHash, StringComparison.Ordinal))
            {
                DummyVerifyCalls++;
            }

            return string.Equals(encodedHash, HashOf(password), StringComparison.Ordinal)
                ? PasswordVerification.Succeeded
                : PasswordVerification.Failed;
        }
    }

    internal sealed class FakeAccessTokenIssuer : IAccessTokenIssuer
    {
        public int IssueCalls { get; private set; }
        public AuthenticatedIdentity? LastIdentity { get; private set; }

        public IssuedAccessToken Issue(AuthenticatedIdentity identity, DateTimeOffset now)
        {
            IssueCalls++;
            LastIdentity = identity;
            return new IssuedAccessToken($"access-{identity.UserId}", $"jti-{IssueCalls}", now.AddMinutes(30));
        }
    }

    internal sealed class FakeRefreshTokenGenerator : IRefreshTokenGenerator
    {
        private int _issued;

        public string NewToken() => $"refresh-{++_issued}";

        public string Fingerprint(string token) => $"fp({token})";
    }
}
