namespace Qlns.BusinessLogic.Modules.Recruitment.Interviews;

/// <summary>Transition requested through <c>POST /recruitment/interviews/{interviewId}/{action}</c>.</summary>
public enum InterviewAction
{
    Reschedule,
    Complete,
    Cancel
}

public static class InterviewActionNames
{
    public static string ToContract(this InterviewAction action) => action switch
    {
        InterviewAction.Reschedule => "reschedule",
        InterviewAction.Complete => "complete",
        InterviewAction.Cancel => "cancel",
        _ => throw new ArgumentOutOfRangeException(nameof(action))
    };

    public static bool TryParseContract(string? value, out InterviewAction action)
    {
        action = value switch
        {
            "reschedule" => InterviewAction.Reschedule,
            "complete" => InterviewAction.Complete,
            "cancel" => InterviewAction.Cancel,
            _ => default
        };

        return value is "reschedule" or "complete" or "cancel";
    }
}
