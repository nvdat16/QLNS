using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.Employees;

/// <summary>Directory search parameters (GET /employees). Scope filtering is applied by the repository.</summary>
public sealed record EmployeeSearchQuery(
    string? Search,
    long? DepartmentId,
    long? PositionId,
    EmployeeStatus? Status,
    string Sort,
    PageRequest Page);

/// <summary>Sort allowlist of the listEmployees operation.</summary>
public static class EmployeeSort
{
    public const string Name = "name";
    public const string NameDescending = "-name";
    public const string EmployeeCode = "employeeCode";
    public const string HireDate = "hireDate";

    public static IReadOnlyList<string> Allowed { get; } = [Name, NameDescending, EmployeeCode, HireDate];

    public static bool IsAllowed(string? sort) => sort is Name or NameDescending or EmployeeCode or HireDate;

    /// <summary>Blank sort falls back to the contract default (<c>name</c>).</summary>
    public static string Normalize(string? sort) => string.IsNullOrWhiteSpace(sort) ? Name : sort.Trim();
}
