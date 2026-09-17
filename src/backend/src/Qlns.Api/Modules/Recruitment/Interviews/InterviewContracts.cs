using System.ComponentModel.DataAnnotations;
using Qlns.BusinessLogic.Modules.Recruitment.Interviews;

namespace Qlns.Api.Modules.Recruitment.Interviews;

/// <summary>OpenAPI InterviewWrite.</summary>
public sealed record InterviewWriteRequest(
    long ApplicationId,
    [Required, MaxLength(Interview.InterviewTypeMaxLength)] string InterviewType,
    [Required] DateTimeOffset? StartsAt,
    [Required] DateTimeOffset? EndsAt,
    [Required, MaxLength(InterviewScheduleRules.TimezoneMaxLength)] string Timezone,
    [Required, MinLength(1)] long[] InterviewerUserIds,
    [MaxLength(InterviewScheduleRules.LocationMaxLength)] string? Location,
    [MaxLength(InterviewScheduleRules.MeetingUrlMaxLength)] string? MeetingUrl)
{
    public InterviewWrite ToWrite() => new(
        ApplicationId,
        InterviewType,
        StartsAt,
        EndsAt,
        Timezone,
        InterviewerUserIds,
        Location,
        MeetingUrl);
}

/// <summary>OpenAPI InterviewAction (optional body of the transition endpoint).</summary>
public sealed record InterviewActionRequest(
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    [MaxLength(InterviewScheduleRules.TimezoneMaxLength)] string? Timezone,
    [MaxLength(InterviewScheduleRules.LocationMaxLength)] string? Location,
    [MaxLength(InterviewScheduleRules.MeetingUrlMaxLength)] string? MeetingUrl,
    [MaxLength(Interview.ReasonMaxLength)] string? Reason)
{
    public InterviewChange ToChange() => new(StartsAt, EndsAt, Timezone, Location, MeetingUrl, Reason);
}

/// <summary>OpenAPI Interview.</summary>
public sealed record InterviewResponse(
    long Id,
    long ApplicationId,
    string InterviewType,
    DateTimeOffset StartsAt,
    DateTimeOffset EndsAt,
    string Timezone,
    IReadOnlyList<long> InterviewerUserIds,
    string? Location,
    string? MeetingUrl,
    string Status,
    string? CancellationReason,
    long Version)
{
    public static InterviewResponse From(Interview interview) => new(
        interview.Id,
        interview.ApplicationId,
        interview.InterviewType,
        interview.StartsAt,
        interview.EndsAt,
        interview.Timezone,
        interview.PanelUserIds,
        interview.Location,
        interview.MeetingUrl,
        interview.Status.ToContract(),
        interview.CancellationReason,
        interview.Version);
}
