using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Evaluations;
using Xunit;

namespace Qlns.BusinessLogic.UnitTests.Modules.Recruitment.Evaluations;

public sealed class EvaluationScoringTests
{
    [Theory]
    [InlineData(5, 5, 5, 5, 5)]
    [InlineData(0, 0, 0, 0, 0)]
    [InlineData(4, 3.5, 4.5, 5, 4.3)]     // 4.25 → 4.3 (half away from zero)
    [InlineData(3, 3, 3, 3.5, 3.1)]       // 3.125 → 3.1
    [InlineData(4.5, 4.5, 4.5, 4, 4.4)]   // 4.375 → 4.4
    [InlineData(2.5, 2.5, 2.5, 3, 2.6)]   // 2.625 → 2.6
    [InlineData(1, 2, 3, 4, 2.5)]
    public void EqualWeightPolicy_ComputesMeanRoundedToOneDecimal(double technical, double communication, double problemSolving, double teamwork, double expected)
    {
        var policy = new EqualWeightScoringPolicy();

        var overall = policy.ComputeOverall(new EvaluationScores((decimal)technical, (decimal)communication, (decimal)problemSolving, (decimal)teamwork));

        Assert.Equal((decimal)expected, overall);
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(0.5, true)]
    [InlineData(2.5, true)]
    [InlineData(5, true)]
    [InlineData(5.5, false)]
    [InlineData(-0.5, false)]
    [InlineData(4.3, false)]
    [InlineData(0.25, false)]
    public void IsValidScore_AcceptsOnlyHalfStepsBetweenZeroAndFive(double score, bool expected)
    {
        Assert.Equal(expected, EvaluationScores.IsValidScore((decimal)score));
    }

    [Fact]
    public void Validate_RecordsOneErrorPerInvalidCriterion()
    {
        var errors = new ValidationErrors();

        new EvaluationScores(4m, 5.5m, 4m, 4.1m).Validate(errors);

        var exception = Assert.Throws<CoreHrValidationException>(errors.ThrowIfAny);
        Assert.Equal(["communicationScore", "teamworkScore"], exception.Errors.Keys.Order());
    }

    [Fact]
    public void Validate_AllValid_RecordsNothing()
    {
        var errors = new ValidationErrors();

        new EvaluationScores(0m, 2.5m, 5m, 4.5m).Validate(errors);

        Assert.False(errors.HasErrors);
    }
}
