namespace Qlns.BusinessLogic.Modules.CoreHr.Probation;

/// <summary>probation_reviews.outcome (ck_probation_outcome). Contract values are snake_case.</summary>
public enum ProbationOutcome
{
    Confirmed,
    Extended,
    Terminated
}

public static class ProbationOutcomeNames
{
    public static string ToContract(this ProbationOutcome outcome) => outcome switch
    {
        ProbationOutcome.Confirmed => "confirmed",
        ProbationOutcome.Extended => "extended",
        ProbationOutcome.Terminated => "terminated",
        _ => throw new ArgumentOutOfRangeException(nameof(outcome))
    };

    public static bool TryParseContract(string? value, out ProbationOutcome outcome)
    {
        outcome = value switch
        {
            "confirmed" => ProbationOutcome.Confirmed,
            "extended" => ProbationOutcome.Extended,
            "terminated" => ProbationOutcome.Terminated,
            _ => default
        };

        return value is "confirmed" or "extended" or "terminated";
    }
}
