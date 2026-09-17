namespace Qlns.BusinessLogic.Modules.Recruitment.Interviews;

/// <summary>
/// Half-open time window <c>[StartsAt, EndsAt)</c> of an interview. Two slots conflict when they overlap;
/// back-to-back slots (one ends exactly when the other starts) do not.
/// </summary>
public readonly record struct InterviewSlot(DateTimeOffset StartsAt, DateTimeOffset EndsAt)
{
    public bool Overlaps(InterviewSlot other) => StartsAt < other.EndsAt && other.StartsAt < EndsAt;
}
