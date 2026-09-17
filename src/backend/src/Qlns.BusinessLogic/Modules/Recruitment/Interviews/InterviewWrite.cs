namespace Qlns.BusinessLogic.Modules.Recruitment.Interviews;

/// <summary>Payload of <c>POST /recruitment/interviews</c> (OpenAPI InterviewWrite). Validated by <see cref="Interview.Schedule"/>.</summary>
public sealed record InterviewWrite(
    long ApplicationId,
    string? InterviewType,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    string? Timezone,
    IReadOnlyList<long>? InterviewerUserIds,
    string? Location,
    string? MeetingUrl);

/// <summary>
/// Optional body of the transition endpoint (OpenAPI InterviewAction). For <c>reschedule</c> the new
/// <see cref="StartsAt"/>/<see cref="EndsAt"/> are mandatory and a null venue/timezone field keeps the current value;
/// for <c>cancel</c> only <see cref="Reason"/> is read.
/// </summary>
public sealed record InterviewChange(
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    string? Timezone,
    string? Location,
    string? MeetingUrl,
    string? Reason)
{
    public static InterviewChange Empty { get; } = new(null, null, null, null, null, null);
}
