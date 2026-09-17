namespace Qlns.BusinessLogic.Modules.CoreHr.Employees;

/// <summary>An employee together with the field visibility resolved for the requesting actor.</summary>
public sealed record EmployeeView(Employee Employee, EmployeeFieldVisibility Visibility);
