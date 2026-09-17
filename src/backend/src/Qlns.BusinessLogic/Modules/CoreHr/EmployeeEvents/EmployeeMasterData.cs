namespace Qlns.BusinessLogic.Modules.CoreHr.EmployeeEvents;

/// <summary>
/// Snapshot of the <c>employees</c> columns an event may change, plus what is needed to check data scope
/// and the employees table constraints. <see cref="EmployeeEvent.ApplyTo"/> is the only producer of a new snapshot.
/// </summary>
public sealed record EmployeeMasterData(
    long EmployeeId,
    long DepartmentId,
    long PositionId,
    long? ManagerId,
    string Status,
    string? WorkEmail,
    long Version);
