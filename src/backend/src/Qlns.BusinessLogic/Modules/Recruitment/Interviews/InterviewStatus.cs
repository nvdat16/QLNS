namespace Qlns.BusinessLogic.Modules.Recruitment.Interviews;

/// <summary>interviews.status (ck_interview_status). <c>no_show</c> is reserved: no endpoint sets it in this delivery.</summary>
public enum InterviewStatus
{
    Scheduled,
    Completed,
    Cancelled,
    NoShow
}

public static class InterviewStatusNames
{
    public static string ToContract(this InterviewStatus status) => status switch
    {
        InterviewStatus.Scheduled => "scheduled",
        InterviewStatus.Completed => "completed",
        InterviewStatus.Cancelled => "cancelled",
        InterviewStatus.NoShow => "no_show",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static bool TryParseContract(string? value, out InterviewStatus status)
    {
        status = value switch
        {
            "scheduled" => InterviewStatus.Scheduled,
            "completed" => InterviewStatus.Completed,
            "cancelled" => InterviewStatus.Cancelled,
            "no_show" => InterviewStatus.NoShow,
            _ => default
        };

        return value is "scheduled" or "completed" or "cancelled" or "no_show";
    }
}
