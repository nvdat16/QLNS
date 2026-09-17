using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Recruitment.Interviews;

/// <summary>
/// Semantic validation shared by scheduling and rescheduling (422). Field names are the contract names.
/// </summary>
public static class InterviewScheduleRules
{
    public const int LocationMaxLength = 255;
    public const int MeetingUrlMaxLength = 2048;
    public const int TimezoneMaxLength = 100;

    /// <summary>Both instants required, <c>endsAt &gt; startsAt</c>, and the slot must start in the future.</summary>
    public static InterviewSlot? ValidateSlot(
        DateTimeOffset? startsAt,
        DateTimeOffset? endsAt,
        DateTimeOffset now,
        ValidationErrors errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        if (startsAt is null)
        {
            errors.Add("startsAt", "startsAt is required.");
        }

        if (endsAt is null)
        {
            errors.Add("endsAt", "endsAt is required.");
        }

        if (startsAt is null || endsAt is null)
        {
            return null;
        }

        if (endsAt <= startsAt)
        {
            errors.Add("endsAt", "endsAt must be after startsAt.");
        }

        if (startsAt <= now)
        {
            errors.Add("startsAt", "startsAt must be in the future.");
        }

        return new InterviewSlot(startsAt.Value, endsAt.Value);
    }

    /// <summary>Trimmed IANA (or platform) time zone id accepted by <see cref="TimeZoneInfo.TryFindSystemTimeZoneById"/>.</summary>
    public static string? ValidateTimezone(string? timezone, ValidationErrors errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var value = timezone?.Trim();
        if (string.IsNullOrEmpty(value))
        {
            errors.Add("timezone", "timezone is required.");
            return null;
        }

        if (value.Length > TimezoneMaxLength || !TimeZoneInfo.TryFindSystemTimeZoneById(value, out _))
        {
            errors.Add("timezone", "timezone must be a valid IANA time zone identifier, for example Asia/Ho_Chi_Minh.");
            return null;
        }

        return value;
    }

    /// <summary>
    /// Normalizes the venue: blank strings become null, at least one of location / meetingUrl must remain,
    /// and meetingUrl must be an absolute http(s) URL.
    /// </summary>
    public static (string? Location, string? MeetingUrl) ValidateVenue(string? location, string? meetingUrl, ValidationErrors errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var normalizedLocation = string.IsNullOrWhiteSpace(location) ? null : location.Trim();
        var normalizedUrl = string.IsNullOrWhiteSpace(meetingUrl) ? null : meetingUrl.Trim();

        if (normalizedLocation is null && normalizedUrl is null)
        {
            errors.Add("location", "Either location or meetingUrl is required.");
            errors.Add("meetingUrl", "Either location or meetingUrl is required.");
        }

        if (normalizedLocation is { Length: > LocationMaxLength })
        {
            errors.Add("location", $"location must be at most {LocationMaxLength} characters.");
        }

        if (normalizedUrl is not null &&
            (normalizedUrl.Length > MeetingUrlMaxLength ||
             !Uri.TryCreate(normalizedUrl, UriKind.Absolute, out var uri) ||
             (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp)))
        {
            errors.Add("meetingUrl", "meetingUrl must be an absolute http(s) URL.");
        }

        return (normalizedLocation, normalizedUrl);
    }
}
