namespace Qlns.BusinessLogic.Modules.CoreHr.Probation;

/// <summary>probation_reviews.status (ck_probation_status). Contract values are snake_case.</summary>
public enum ProbationReviewStatus
{
    Pending,
    InReview,
    Decided,
    Cancelled
}

public static class ProbationReviewStatusNames
{
    public static string ToContract(this ProbationReviewStatus status) => status switch
    {
        ProbationReviewStatus.Pending => "pending",
        ProbationReviewStatus.InReview => "in_review",
        ProbationReviewStatus.Decided => "decided",
        ProbationReviewStatus.Cancelled => "cancelled",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static bool TryParseContract(string? value, out ProbationReviewStatus status)
    {
        status = value switch
        {
            "pending" => ProbationReviewStatus.Pending,
            "in_review" => ProbationReviewStatus.InReview,
            "decided" => ProbationReviewStatus.Decided,
            "cancelled" => ProbationReviewStatus.Cancelled,
            _ => default
        };

        return value is "pending" or "in_review" or "decided" or "cancelled";
    }

    /// <summary>A review that still awaits assessment or decision (the states that can become overdue).</summary>
    public static bool IsOpen(this ProbationReviewStatus status) =>
        status is ProbationReviewStatus.Pending or ProbationReviewStatus.InReview;
}
