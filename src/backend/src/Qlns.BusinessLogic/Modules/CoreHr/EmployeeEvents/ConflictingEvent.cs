namespace Qlns.BusinessLogic.Modules.CoreHr.EmployeeEvents;

/// <summary>A non-cancelled event on the same effective date that changes at least one of the same fields.</summary>
public sealed record ConflictingEvent(long EventId, IReadOnlyList<string> Fields);
