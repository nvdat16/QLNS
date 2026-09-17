using Qlns.BusinessLogic.Modules.Contracts.Contracts;
using Xunit;

namespace Qlns.BusinessLogic.UnitTests.Modules.Contracts.Contracts;

public sealed class ExpiryAlertPolicyTests
{
    private static readonly DateOnly AsOf = new(2026, 9, 17);

    [Theory]
    [InlineData(ContractType.Probation, 0, ExpiryAlertLevel.Red)]
    [InlineData(ContractType.Probation, 7, ExpiryAlertLevel.Red)]
    [InlineData(ContractType.Probation, 8, ExpiryAlertLevel.Amber)]
    [InlineData(ContractType.Probation, 15, ExpiryAlertLevel.Amber)]
    [InlineData(ContractType.FixedTerm, 30, ExpiryAlertLevel.Red)]
    [InlineData(ContractType.FixedTerm, 31, ExpiryAlertLevel.Amber)]
    [InlineData(ContractType.FixedTerm, 45, ExpiryAlertLevel.Amber)]
    [InlineData(ContractType.Internship, 1, ExpiryAlertLevel.Red)]
    [InlineData(ContractType.ServiceContract, 45, ExpiryAlertLevel.Amber)]
    public void Evaluate_WithinThreshold_ReturnsLevel(ContractType type, int daysRemaining, ExpiryAlertLevel expected)
    {
        Assert.Equal(expected, ExpiryAlertPolicy.Evaluate(type, daysRemaining));
    }

    [Theory]
    [InlineData(ContractType.Probation, 16)]
    [InlineData(ContractType.FixedTerm, 46)]
    [InlineData(ContractType.Internship, 100)]
    [InlineData(ContractType.FixedTerm, -1)]
    [InlineData(ContractType.Probation, -1)]
    public void Evaluate_OutsideThresholdOrEnded_ReturnsNull(ContractType type, int daysRemaining)
    {
        Assert.Null(ExpiryAlertPolicy.Evaluate(type, daysRemaining));
    }

    [Fact]
    public void Thresholds_MatchCon02()
    {
        Assert.Equal(7, ExpiryAlertPolicy.RedThresholdDays(ContractType.Probation));
        Assert.Equal(15, ExpiryAlertPolicy.AmberThresholdDays(ContractType.Probation));
        Assert.Equal(30, ExpiryAlertPolicy.RedThresholdDays(ContractType.FixedTerm));
        Assert.Equal(45, ExpiryAlertPolicy.AmberThresholdDays(ContractType.FixedTerm));
        Assert.Equal(45, ExpiryAlertPolicy.AmberThresholdDays(ContractType.Indefinite));
        Assert.Equal(45, ExpiryAlertPolicy.DefaultWithinDays);
    }

    [Fact]
    public void Windows_DefaultHorizon_CapsEachTypeAtItsAmberThresholdAndSkipsIndefinite()
    {
        var windows = ExpiryAlertPolicy.Windows(AsOf, ExpiryAlertPolicy.DefaultWithinDays);

        Assert.Equal(4, windows.Count);
        Assert.DoesNotContain(windows, w => w.Type == ContractType.Indefinite);
        Assert.Equal(AsOf.AddDays(15), windows.Single(w => w.Type == ContractType.Probation).LatestEndDate);
        Assert.Equal(AsOf.AddDays(45), windows.Single(w => w.Type == ContractType.FixedTerm).LatestEndDate);
        Assert.Equal(AsOf.AddDays(45), windows.Single(w => w.Type == ContractType.Internship).LatestEndDate);
        Assert.Equal(AsOf.AddDays(45), windows.Single(w => w.Type == ContractType.ServiceContract).LatestEndDate);
    }

    [Fact]
    public void Windows_ShortHorizon_NarrowsEveryType()
    {
        var windows = ExpiryAlertPolicy.Windows(AsOf, 10);

        Assert.All(windows, w => Assert.Equal(AsOf.AddDays(10), w.LatestEndDate));
    }

    [Fact]
    public void Windows_LongHorizon_NeverExceedsAmberThreshold()
    {
        var windows = ExpiryAlertPolicy.Windows(AsOf, 365);

        Assert.Equal(AsOf.AddDays(15), windows.Single(w => w.Type == ContractType.Probation).LatestEndDate);
        Assert.Equal(AsOf.AddDays(45), windows.Single(w => w.Type == ContractType.FixedTerm).LatestEndDate);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(366)]
    public void Windows_OutOfRangeHorizon_Throws(int withinDays)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ExpiryAlertPolicy.Windows(AsOf, withinDays));
    }

    [Fact]
    public void ToContract_UsesLowerCaseNames()
    {
        Assert.Equal("amber", ExpiryAlertLevel.Amber.ToContract());
        Assert.Equal("red", ExpiryAlertLevel.Red.ToContract());
    }
}
