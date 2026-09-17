using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Recruitment.Interviews;

public enum InterviewConflictKind
{
    /// <summary>A panelist already sits on another scheduled interview in an overlapping slot.</summary>
    Interviewer,

    /// <summary>The same physical location is booked by another scheduled interview in an overlapping slot.</summary>
    Location
}

/// <summary>Minimal projection of a scheduled interview used for conflict detection.</summary>
public sealed record ScheduledInterviewSummary(
    long Id,
    InterviewSlot Slot,
    string? Location,
    IReadOnlyList<long> PanelUserIds);

/// <summary>A detected double booking. Maps to 409 <c>recruitment.interview.interviewer_conflict</c> / <c>location_conflict</c>.</summary>
public sealed record InterviewConflict(
    InterviewConflictKind Kind,
    long? InterviewerUserId,
    long ConflictingInterviewId,
    InterviewSlot Slot)
{
    public const string InterviewerConflictCode = "recruitment.interview.interviewer_conflict";
    public const string LocationConflictCode = "recruitment.interview.location_conflict";

    public CoreHrBusinessRuleException ToBusinessRule() => Kind switch
    {
        InterviewConflictKind.Interviewer => new CoreHrBusinessRuleException(
            InterviewerConflictCode,
            $"Interviewer {InterviewerUserId} already has interview {ConflictingInterviewId} from {Slot.StartsAt:O} to {Slot.EndsAt:O}.")
        {
            Details = new Dictionary<string, object?>
            {
                ["interviewerUserId"] = InterviewerUserId,
                ["conflictingInterviewId"] = ConflictingInterviewId,
                ["startsAt"] = Slot.StartsAt,
                ["endsAt"] = Slot.EndsAt
            }
        },
        InterviewConflictKind.Location => new CoreHrBusinessRuleException(
            LocationConflictCode,
            $"The location is already booked by interview {ConflictingInterviewId} from {Slot.StartsAt:O} to {Slot.EndsAt:O}.")
        {
            Details = new Dictionary<string, object?>
            {
                ["conflictingInterviewId"] = ConflictingInterviewId,
                ["startsAt"] = Slot.StartsAt,
                ["endsAt"] = Slot.EndsAt
            }
        },
        _ => throw new ArgumentOutOfRangeException(nameof(Kind))
    };
}

/// <summary>
/// Pure double-booking detection (REC-04.1). Interviewer conflicts take precedence over location conflicts;
/// among several, the earliest conflicting interview (then lowest id) is reported so the message is deterministic.
/// </summary>
public static class InterviewConflictDetector
{
    public static InterviewConflict? FindFirst(
        InterviewSlot slot,
        IReadOnlyCollection<long> panelUserIds,
        string? location,
        IEnumerable<ScheduledInterviewSummary> scheduledInterviews,
        long? excludeInterviewId = null)
    {
        ArgumentNullException.ThrowIfNull(panelUserIds);
        ArgumentNullException.ThrowIfNull(scheduledInterviews);

        var ordered = scheduledInterviews
            .Where(other => other.Id != excludeInterviewId && other.Slot.Overlaps(slot))
            .OrderBy(other => other.Slot.StartsAt)
            .ThenBy(other => other.Id)
            .ToList();

        foreach (var other in ordered)
        {
            foreach (var userId in panelUserIds)
            {
                if (other.PanelUserIds.Contains(userId))
                {
                    return new InterviewConflict(InterviewConflictKind.Interviewer, userId, other.Id, other.Slot);
                }
            }
        }

        var normalizedLocation = string.IsNullOrWhiteSpace(location) ? null : location.Trim();
        if (normalizedLocation is null)
        {
            return null;
        }

        var locationClash = ordered.FirstOrDefault(other =>
            !string.IsNullOrWhiteSpace(other.Location) &&
            string.Equals(other.Location.Trim(), normalizedLocation, StringComparison.OrdinalIgnoreCase));

        return locationClash is null
            ? null
            : new InterviewConflict(InterviewConflictKind.Location, null, locationClash.Id, locationClash.Slot);
    }
}
