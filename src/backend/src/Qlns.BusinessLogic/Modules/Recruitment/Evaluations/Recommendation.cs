namespace Qlns.BusinessLogic.Modules.Recruitment.Evaluations;

/// <summary>evaluations.recommendation (ck_evaluation_recommendation). Contract values are snake_case.</summary>
public enum Recommendation
{
    StrongHire,
    Hire,
    Hold,
    NoHire,
    StrongNoHire
}

public static class RecommendationNames
{
    public static string ToContract(this Recommendation recommendation) => recommendation switch
    {
        Recommendation.StrongHire => "strong_hire",
        Recommendation.Hire => "hire",
        Recommendation.Hold => "hold",
        Recommendation.NoHire => "no_hire",
        Recommendation.StrongNoHire => "strong_no_hire",
        _ => throw new ArgumentOutOfRangeException(nameof(recommendation))
    };

    public static bool TryParseContract(string? value, out Recommendation recommendation)
    {
        recommendation = value switch
        {
            "strong_hire" => Recommendation.StrongHire,
            "hire" => Recommendation.Hire,
            "hold" => Recommendation.Hold,
            "no_hire" => Recommendation.NoHire,
            "strong_no_hire" => Recommendation.StrongNoHire,
            _ => default
        };

        return value is "strong_hire" or "hire" or "hold" or "no_hire" or "strong_no_hire";
    }
}
