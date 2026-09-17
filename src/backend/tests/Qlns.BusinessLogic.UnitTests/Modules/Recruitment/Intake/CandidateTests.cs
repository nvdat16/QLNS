using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Intake;
using Xunit;

namespace Qlns.BusinessLogic.UnitTests.Modules.Recruitment.Intake;

public sealed class CandidateTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 17, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset ConsentedAt = Now.AddHours(-1);

    [Fact]
    public void Create_Valid_TrimsFieldsNormalizesIdentityAndStartsTransient()
    {
        var input = new CandidateInput(
            "  An ", " Nguyen ", " An.Nguyen@Example.com ", " 0912 345 678 ", " https://linkedin.com/in/an ", "   ");

        var candidate = Candidate.Create(input, "v2", ConsentedAt, Now);

        Assert.Equal(0, candidate.Id);
        Assert.Equal("An", candidate.FirstName);
        Assert.Equal("Nguyen", candidate.LastName);
        Assert.Equal("An.Nguyen@Example.com", candidate.Email);
        Assert.Equal("an.nguyen@example.com", candidate.NormalizedEmail);
        Assert.Equal("0912 345 678", candidate.Phone);
        Assert.Equal("84912345678", candidate.NormalizedPhone);
        Assert.Equal("https://linkedin.com/in/an", candidate.LinkedinUrl);
        Assert.Null(candidate.PortfolioUrl);
        Assert.Equal("v2", candidate.PrivacyNoticeVersion);
        Assert.Equal(ConsentedAt, candidate.ConsentedAt);
        Assert.Equal(1, candidate.Version);
        Assert.Equal(Now, candidate.CreatedAt);
        Assert.Equal(Now, candidate.UpdatedAt);
    }

    [Fact]
    public void Create_WithoutPhone_LeavesNormalizedPhoneNull()
    {
        var candidate = Candidate.Create(ValidInput() with { Phone = null }, "v2", ConsentedAt, Now);

        Assert.Null(candidate.Phone);
        Assert.Null(candidate.NormalizedPhone);
    }

    [Fact]
    public void Create_MissingNamesAndEmail_ReportsCandidateFields()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Candidate.Create(new CandidateInput(" ", null, "", null, null, null), "v2", ConsentedAt, Now));

        Assert.Equal(
            ["candidate.email", "candidate.firstName", "candidate.lastName"],
            exception.Errors.Keys.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void Create_NamesTooLong_ReportsBothNames()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Candidate.Create(ValidInput() with { FirstName = new string('a', 101), LastName = new string('b', 101) }, "v2", ConsentedAt, Now));

        Assert.True(exception.Errors.ContainsKey("candidate.firstName"));
        Assert.True(exception.Errors.ContainsKey("candidate.lastName"));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("an nguyen@example.com")]
    [InlineData("An Nguyen <an@example.com>")]
    [InlineData("@example.com")]
    public void Create_InvalidEmail_ThrowsValidationOnEmail(string email)
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Candidate.Create(ValidInput() with { Email = email }, "v2", ConsentedAt, Now));

        Assert.Equal(["candidate.email"], exception.Errors.Keys);
    }

    [Fact]
    public void Create_EmailTooLong_ThrowsValidationOnEmail()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Candidate.Create(ValidInput() with { Email = new string('a', 320) + "@example.com" }, "v2", ConsentedAt, Now));

        Assert.Equal(["candidate.email"], exception.Errors.Keys);
    }

    [Theory]
    [InlineData("ftp://files.example.com/cv")]
    [InlineData("not a url")]
    [InlineData("www.example.com/portfolio")]
    public void Create_InvalidUrl_ThrowsValidationOnUrlField(string url)
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Candidate.Create(ValidInput() with { PortfolioUrl = url }, "v2", ConsentedAt, Now));

        Assert.Equal(["candidate.portfolioUrl"], exception.Errors.Keys);
    }

    [Fact]
    public void Create_UrlTooLong_ThrowsValidationOnUrlField()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Candidate.Create(ValidInput() with { LinkedinUrl = "https://example.com/" + new string('p', 2048) }, "v2", ConsentedAt, Now));

        Assert.Equal(["candidate.linkedinUrl"], exception.Errors.Keys);
    }

    [Fact]
    public void Create_PhoneWithoutDigits_ThrowsValidationOnPhone()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Candidate.Create(ValidInput() with { Phone = "call me" }, "v2", ConsentedAt, Now));

        Assert.Equal(["candidate.phone"], exception.Errors.Keys);
    }

    [Fact]
    public void Create_PhoneTooLong_ThrowsValidationOnPhone()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Candidate.Create(ValidInput() with { Phone = new string('9', 31) }, "v2", ConsentedAt, Now));

        Assert.Equal(["candidate.phone"], exception.Errors.Keys);
    }

    [Fact]
    public void Create_BlankPrivacyNoticeVersion_ThrowsArgument()
    {
        Assert.Throws<ArgumentException>(() => Candidate.Create(ValidInput(), " ", ConsentedAt, Now));
    }

    [Fact]
    public void Refresh_UpdatesContactChannelsKeepsIdentityAndBumpsVersion()
    {
        var candidate = Existing(version: 3);
        var input = new CandidateInput("Other", "Person", "other@example.com", "0999 888 777", "https://linkedin.com/in/new", "https://portfolio.example.com");

        var changed = candidate.Refresh(input, Now);

        Assert.Equal(["phone", "linkedinUrl", "portfolioUrl"], changed);
        Assert.Equal("An", candidate.FirstName);
        Assert.Equal("Nguyen", candidate.LastName);
        Assert.Equal("an@example.com", candidate.Email);
        Assert.Equal("an@example.com", candidate.NormalizedEmail);
        Assert.Equal("0999 888 777", candidate.Phone);
        Assert.Equal("84999888777", candidate.NormalizedPhone);
        Assert.Equal("https://linkedin.com/in/new", candidate.LinkedinUrl);
        Assert.Equal("https://portfolio.example.com", candidate.PortfolioUrl);
        Assert.Equal(4, candidate.Version);
        Assert.Equal(Now, candidate.UpdatedAt);
    }

    [Fact]
    public void Refresh_SameContactChannels_ReportsNoChangeButBumpsVersion()
    {
        var candidate = Existing(version: 1);

        var changed = candidate.Refresh(new CandidateInput("X", "Y", "x@y.com", "0912345678", null, null), Now);

        Assert.Empty(changed);
        Assert.Equal(2, candidate.Version);
    }

    [Fact]
    public void Refresh_InvalidInput_ThrowsValidationAndLeavesCandidateUnchanged()
    {
        var candidate = Existing(version: 1);

        var exception = Assert.Throws<CoreHrValidationException>(() =>
            candidate.Refresh(new CandidateInput("X", "Y", "bad", "0999", null, null), Now));

        Assert.Equal(["candidate.email"], exception.Errors.Keys);
        Assert.Equal("0912345678", candidate.Phone);
        Assert.Equal(1, candidate.Version);
    }

    [Fact]
    public void Constructor_InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Candidate(-1, "An", "Nguyen", "an@example.com", "an@example.com", null, null, null, null, "v1", Now, 1, Now, Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Candidate(1, "An", "Nguyen", "an@example.com", "an@example.com", null, null, null, null, "v1", Now, 0, Now, Now));
        Assert.Throws<ArgumentException>(() => new Candidate(1, "", "Nguyen", "an@example.com", "an@example.com", null, null, null, null, "v1", Now, 1, Now, Now));
        Assert.Throws<ArgumentException>(() => new Candidate(1, "An", "Nguyen", "an@example.com", " ", null, null, null, null, "v1", Now, 1, Now, Now));
        Assert.Throws<ArgumentException>(() => new Candidate(1, "An", "Nguyen", "an@example.com", "an@example.com", null, null, null, null, "", Now, 1, Now, Now));
    }

    [Fact]
    public void Summaries_ExposeOnlyContractFields()
    {
        var candidate = Existing(version: 1);

        var summary = CandidateSummary.From(candidate);
        var duplicate = DuplicateCandidate.From(candidate);

        Assert.Equal(new CandidateSummary(55, "An", "Nguyen", "an@example.com", "0912345678", null, null), summary);
        Assert.Equal(new DuplicateCandidate(55, "An", "Nguyen", "an@example.com"), duplicate);
    }

    private static CandidateInput ValidInput() =>
        new("An", "Nguyen", "an@example.com", "0912345678", null, null);

    private static Candidate Existing(long version) => new(
        55, "An", "Nguyen", "an@example.com", "an@example.com", "0912345678", "84912345678",
        linkedinUrl: null, portfolioUrl: null, "v1", ConsentedAt, version, Now.AddDays(-30), Now.AddDays(-1));
}
