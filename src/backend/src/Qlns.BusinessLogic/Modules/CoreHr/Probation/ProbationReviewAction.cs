namespace Qlns.BusinessLogic.Modules.CoreHr.Probation;

/// <summary>Transition requested through <c>POST /probation-reviews/{reviewId}/{action}</c>.</summary>
public enum ProbationReviewAction
{
    Decide,
    Cancel,
    Unlock
}

public static class ProbationReviewActionNames
{
    public static string ToContract(this ProbationReviewAction action) => action switch
    {
        ProbationReviewAction.Decide => "decide",
        ProbationReviewAction.Cancel => "cancel",
        ProbationReviewAction.Unlock => "unlock",
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };

    public static bool TryParseContract(string? value, out ProbationReviewAction action)
    {
        action = value switch
        {
            "decide" => ProbationReviewAction.Decide,
            "cancel" => ProbationReviewAction.Cancel,
            "unlock" => ProbationReviewAction.Unlock,
            _ => default
        };

        return value is "decide" or "cancel" or "unlock";
    }
}
