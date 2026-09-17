using Qlns.BusinessLogic.Modules.Recruitment.Intake;
using Xunit;

namespace Qlns.BusinessLogic.UnitTests.Modules.Recruitment.Intake;

public sealed class CandidateNormalizationTests
{
    [Theory]
    [InlineData("An.Nguyen@Example.COM", "an.nguyen@example.com")]
    [InlineData("  an@example.com  ", "an@example.com")]
    [InlineData("an@example.com", "an@example.com")]
    public void NormalizeEmail_TrimsAndLowerCases(string input, string expected)
    {
        Assert.Equal(expected, CandidateNormalization.NormalizeEmail(input));
    }

    [Theory]
    [InlineData("0912 345 678", "84912345678")]
    [InlineData("0912-345-678", "84912345678")]
    [InlineData("+84 912 345 678", "84912345678")]
    [InlineData("(84) 912.345.678", "84912345678")]
    [InlineData("84912345678", "84912345678")]
    [InlineData("1 555 0100", "15550100")]
    public void NormalizePhone_KeepsDigitsAndReplacesNationalPrefix(string input, string expected)
    {
        Assert.Equal(expected, CandidateNormalization.NormalizePhone(input));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no digits here")]
    [InlineData("+-()")]
    public void NormalizePhone_WithoutDigits_ReturnsNull(string? input)
    {
        Assert.Null(CandidateNormalization.NormalizePhone(input));
    }

    [Fact]
    public void NormalizePhone_SameNumberInDifferentNotations_Matches()
    {
        Assert.Equal(
            CandidateNormalization.NormalizePhone("0912345678"),
            CandidateNormalization.NormalizePhone("+84-912-345-678"));
    }
}
