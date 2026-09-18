using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Identity.Authentication;
using Qlns.BusinessLogic.Modules.Identity.Shared;
using Xunit;
using static Qlns.BusinessLogic.UnitTests.Modules.Identity.IdentityTestData;

namespace Qlns.BusinessLogic.UnitTests.Modules.Identity.Shared;

public sealed class PasswordPolicyTests
{
    [Theory]
    [InlineData("Correct-Horse-9")]
    [InlineData("aB3-dEfGhIjK")]
    [InlineData("Th1s is a pass phrase")]
    public void Validate_StrongPassword_Passes(string password) =>
        PasswordPolicy.Validate(password, "password");

    [Theory]
    [InlineData(null, "is required")]
    [InlineData("", "is required")]
    [InlineData("Short-1", "at least")]
    [InlineData("alllowercaseonly", "combine at least")]
    [InlineData(" Leading-Space-1", "whitespace")]
    [InlineData("Trailing-Space-1 ", "whitespace")]
    public void Validate_WeakPassword_IsRejected(string? password, string expected)
    {
        var exception = Assert.Throws<CoreHrValidationException>(() => PasswordPolicy.Validate(password, "password"));

        Assert.Contains(expected, string.Join(" ", exception.Errors["password"]), StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_TooLongPassword_IsRejected()
    {
        var password = new string('a', PasswordPolicy.MaxLength) + "B1-";

        Assert.Throws<CoreHrValidationException>(() => PasswordPolicy.Validate(password, "password"));
    }

    [Fact]
    public void Validate_PasswordContainingTheEmailAccountName_IsRejected()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() => PasswordPolicy.Validate(
            "Hr.Manager-2026", "password", "hr.manager@qlns.local"));

        Assert.Contains("account name", string.Join(" ", exception.Errors["password"]), StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_ShortEmailAccountName_DoesNotBlockUnrelatedPasswords()
    {
        // A two-character local part would otherwise reject far too many acceptable passwords.
        PasswordPolicy.Validate("It-Is-A-Secret-1", "password", "it@qlns.local");
    }
}

public sealed class SignInLockoutTests
{
    [Fact]
    public void NextFailure_BelowThreshold_CountsWithoutLocking()
    {
        var next = SignInLockout.NextFailure(Credential(failedAttempts: 1), Now);

        Assert.Equal(2, next.FailedAttempts);
        Assert.Null(next.LockedUntil);
    }

    [Fact]
    public void NextFailure_ReachingThreshold_StartsTheWindow()
    {
        var next = SignInLockout.NextFailure(Credential(failedAttempts: SignInLockout.MaxFailedAttempts - 1), Now);

        Assert.Equal(SignInLockout.MaxFailedAttempts, next.FailedAttempts);
        Assert.Equal(Now + SignInLockout.Window, next.LockedUntil);
    }

    [Fact]
    public void IsLocked_ExpiredWindow_IsNoLongerLocked()
    {
        Assert.False(SignInLockout.IsLocked(Credential(lockedUntil: Now), Now));
        Assert.True(SignInLockout.IsLocked(Credential(lockedUntil: Now.AddSeconds(1)), Now));
    }

    [Fact]
    public void RetryAfter_ReportsTheRemainingWindowOnly()
    {
        Assert.Equal(TimeSpan.FromMinutes(3), SignInLockout.RetryAfter(Credential(lockedUntil: Now.AddMinutes(3)), Now));
        Assert.Equal(TimeSpan.Zero, SignInLockout.RetryAfter(Credential(lockedUntil: null), Now));
    }
}

public sealed class IdentityAuthorizationTests
{
    [Fact]
    public void DataScope_OrganizationGrant_WinsOverDepartmentGrants()
    {
        var authorization = new IdentityAuthorization(
            [RoleGrant.Department("ROLE_LINE_MGR", 2), RoleGrant.Organization("ROLE_HR_MGR")],
            Permissions());

        Assert.True(authorization.DataScope.OrganizationWide);
    }

    [Fact]
    public void DataScope_DepartmentGrants_AreUnioned()
    {
        var authorization = new IdentityAuthorization(
            [RoleGrant.Department("ROLE_LINE_MGR", 2), RoleGrant.Department("ROLE_LINE_MGR", 4)],
            Permissions());

        Assert.False(authorization.DataScope.OrganizationWide);
        Assert.Equal([2L, 4L], authorization.DataScope.DepartmentIds.Order());
    }

    [Fact]
    public void DataScope_SelfGrantOnly_IsSelfScope()
    {
        var authorization = new IdentityAuthorization([RoleGrant.Self("ROLE_EMPLOYEE")], Permissions());

        Assert.True(authorization.DataScope.IsSelfOnly);
    }

    [Fact]
    public void None_GrantsNothing()
    {
        Assert.Empty(IdentityAuthorization.None.Permissions);
        Assert.Empty(IdentityAuthorization.None.RoleCodes);
        Assert.True(IdentityAuthorization.None.DataScope.IsSelfOnly);
    }

    [Fact]
    public void RoleCodes_AreDeduplicatedAndOrdered()
    {
        var authorization = new IdentityAuthorization(
            [RoleGrant.Department("ROLE_LINE_MGR", 4), RoleGrant.Department("ROLE_LINE_MGR", 2), RoleGrant.Self("ROLE_EMPLOYEE")],
            Permissions());

        Assert.Equal(["ROLE_EMPLOYEE", "ROLE_LINE_MGR"], authorization.RoleCodes);
    }

    [Theory]
    [InlineData("ROLE_X", DataScopeType.Department, 0, false)]
    [InlineData("ROLE_X", DataScopeType.Department, 2, true)]
    [InlineData("ROLE_X", DataScopeType.Organization, 3, false)]
    [InlineData("ROLE_X", DataScopeType.Self, 0, true)]
    public void RoleGrant_IsWellFormed_MatchesTheDatabaseConstraint(
        string roleCode,
        DataScopeType scopeType,
        long scopeId,
        bool expected) =>
        Assert.Equal(expected, new RoleGrant(roleCode, scopeType, scopeId).IsWellFormed);

    private static IReadOnlySet<string> Permissions() => new HashSet<string>(StringComparer.Ordinal);
}

public sealed class EmailAddressTests
{
    [Theory]
    [InlineData("  HR.Manager@QLNS.Local  ", "hr.manager@qlns.local")]
    [InlineData(null, "")]
    [InlineData("   ", "")]
    public void Normalize_TrimsAndLowerCases(string? input, string expected) =>
        Assert.Equal(expected, EmailAddress.Normalize(input));

    [Theory]
    [InlineData("user@qlns.local", true)]
    [InlineData("user@sub.qlns.local", true)]
    [InlineData("user", false)]
    [InlineData("@qlns.local", false)]
    [InlineData("user@", false)]
    [InlineData("user@qlns", false)]
    [InlineData("user@@qlns.local", false)]
    [InlineData("us er@qlns.local", false)]
    [InlineData("user@.qlns.local", false)]
    public void IsWellFormed_ChecksTheStructure(string email, bool expected) =>
        Assert.Equal(expected, EmailAddress.IsWellFormed(EmailAddress.Normalize(email)));

    [Fact]
    public void Require_MalformedEmail_ThrowsOnTheGivenField()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() => EmailAddress.Require("nope", "email"));

        Assert.Contains("email", exception.Errors);
    }

    [Fact]
    public void Require_ReturnsTheNormalizedValue() =>
        Assert.Equal("user@qlns.local", EmailAddress.Require(" User@QLNS.local ", "email"));
}

public sealed class AuthenticationSettingsTests
{
    [Fact]
    public void Validate_Defaults_AreAccepted() => new AuthenticationSettings().Validate();

    [Fact]
    public void Validate_AccessTokenLongerThanTheCeiling_IsRejected() =>
        Assert.Throws<InvalidOperationException>(() =>
            new AuthenticationSettings { AccessTokenLifetime = TimeSpan.FromHours(3) }.Validate());

    [Fact]
    public void Validate_RefreshTokenShorterThanAccessToken_IsRejected() =>
        Assert.Throws<InvalidOperationException>(() => new AuthenticationSettings
        {
            AccessTokenLifetime = TimeSpan.FromMinutes(30),
            RefreshTokenLifetime = TimeSpan.FromMinutes(10)
        }.Validate());

    [Fact]
    public void Validate_NonPositiveLifetime_IsRejected() =>
        Assert.Throws<InvalidOperationException>(() =>
            new AuthenticationSettings { AccessTokenLifetime = TimeSpan.Zero }.Validate());
}

public sealed class SignInContextTests
{
    [Fact]
    public void Normalized_TruncatesAttackerControlledFields()
    {
        var context = new SignInContext(new string('1', 80), new string('a', 400), "correlation")
            .Normalized();

        Assert.Equal(SignInContext.ClientIpMaxLength, context.ClientIp!.Length);
        Assert.Equal(SignInContext.UserAgentMaxLength, context.UserAgent!.Length);
        Assert.Equal("correlation", context.CorrelationId);
    }

    [Fact]
    public void Normalized_BlankFieldsBecomeNull()
    {
        var context = new SignInContext("   ", "", "correlation").Normalized();

        Assert.Null(context.ClientIp);
        Assert.Null(context.UserAgent);
    }
}
