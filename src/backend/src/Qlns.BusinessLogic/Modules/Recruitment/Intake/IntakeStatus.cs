namespace Qlns.BusinessLogic.Modules.Recruitment.Intake;

/// <summary>Lifecycle of a résumé intake (resumes.intake_status, OpenAPI CandidateIntake.status).</summary>
public enum IntakeStatus
{
    Scanning,
    Parsing,
    AwaitingConfirmation,
    DuplicateReview,
    Completed,
    Rejected,
    Failed
}

public static class IntakeStatusNames
{
    public static string ToContract(this IntakeStatus status) => status switch
    {
        IntakeStatus.Scanning => "scanning",
        IntakeStatus.Parsing => "parsing",
        IntakeStatus.AwaitingConfirmation => "awaiting_confirmation",
        IntakeStatus.DuplicateReview => "duplicate_review",
        IntakeStatus.Completed => "completed",
        IntakeStatus.Rejected => "rejected",
        IntakeStatus.Failed => "failed",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static bool TryParseContract(string value, out IntakeStatus status)
    {
        status = value switch
        {
            "scanning" => IntakeStatus.Scanning,
            "parsing" => IntakeStatus.Parsing,
            "awaiting_confirmation" => IntakeStatus.AwaitingConfirmation,
            "duplicate_review" => IntakeStatus.DuplicateReview,
            "completed" => IntakeStatus.Completed,
            "rejected" => IntakeStatus.Rejected,
            "failed" => IntakeStatus.Failed,
            _ => default
        };

        return value is "scanning" or "parsing" or "awaiting_confirmation" or "duplicate_review" or
            "completed" or "rejected" or "failed";
    }
}
