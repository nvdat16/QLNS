namespace Qlns.BusinessLogic.Modules.CoreHr.EmployeeEvents;

/// <summary>Outcome of one Effective-Date Worker run: applied count and the ids left for reconciliation.</summary>
public sealed record ApplyDueEventsResult(int Applied, IReadOnlyList<long> Conflicted);
