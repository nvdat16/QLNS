using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Recruitment.Evaluations;

/// <summary>
/// The four scorecard criteria (ck_evaluation_scores): each 0..5 in steps of 0.5.
/// </summary>
public sealed record EvaluationScores(
    decimal Technical,
    decimal Communication,
    decimal ProblemSolving,
    decimal Teamwork)
{
    public const decimal Min = 0m;
    public const decimal Max = 5m;
    public const decimal Step = 0.5m;

    public IReadOnlyList<decimal> All => [Technical, Communication, ProblemSolving, Teamwork];

    public static bool IsValidScore(decimal score) =>
        score >= Min && score <= Max && decimal.Remainder(score, Step) == 0m;

    /// <summary>Records one error per invalid criterion, keyed by contract field name.</summary>
    public void Validate(ValidationErrors errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        Check(errors, "technicalScore", Technical);
        Check(errors, "communicationScore", Communication);
        Check(errors, "problemSolvingScore", ProblemSolving);
        Check(errors, "teamworkScore", Teamwork);
    }

    private static void Check(ValidationErrors errors, string field, decimal score)
    {
        if (!IsValidScore(score))
        {
            errors.Add(field, $"{field} must be between {Min} and {Max} in steps of {Step}.");
        }
    }
}

/// <summary>
/// Computes <c>overall_score</c> from the four criteria. SRS REC-05 says the weights are a per-position,
/// configurable policy; this port is the extension point for that. The default is equal weighting.
/// </summary>
public interface IEvaluationScoringPolicy
{
    /// <summary>Overall score on the 0..5 scale, rounded to one decimal (numeric(2,1)).</summary>
    decimal ComputeOverall(EvaluationScores scores);
}

/// <summary>Default policy: arithmetic mean of the four criteria, rounded half away from zero to one decimal.</summary>
public sealed class EqualWeightScoringPolicy : IEvaluationScoringPolicy
{
    public decimal ComputeOverall(EvaluationScores scores)
    {
        ArgumentNullException.ThrowIfNull(scores);
        var mean = scores.All.Sum() / scores.All.Count;
        return Math.Round(mean, 1, MidpointRounding.AwayFromZero);
    }
}
