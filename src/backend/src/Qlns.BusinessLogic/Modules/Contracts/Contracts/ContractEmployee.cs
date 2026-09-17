namespace Qlns.BusinessLogic.Modules.Contracts.Contracts;

/// <summary>
/// What the Contracts module needs to know about an employee: the department that decides data scope and
/// the manager's user account (probation reviewer), resolved through employees.manager_id → employees.user_id.
/// </summary>
public sealed record ContractEmployee(long EmployeeId, long DepartmentId, long? ManagerUserId);
