namespace Qlns.BusinessLogic.Modules.CoreHr.Organization;

/// <summary>
/// Node of the organization chart (OpenAPI <c>OrganizationNode</c>).
/// The contract exposes a <c>manager</c> (EmployeeSummary) per node, but the canonical schema has no
/// <c>departments.manager_id</c> column (gap noted in docs/functional_specifications.md [EMP-02]);
/// until that migration exists the presentation layer returns <c>manager: null</c>.
/// </summary>
public sealed record OrganizationNode(
    Department Department,
    int Headcount,
    IReadOnlyList<OrganizationNode> Children);
