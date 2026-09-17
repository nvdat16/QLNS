using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Recruitment.Requisitions;

/// <summary>Filters of <c>GET /recruitment/requisitions</c>. Data scope is applied by the repository from the actor, never from the query.</summary>
public sealed record RequisitionSearchQuery(
    string? Search,
    long? DepartmentId,
    RequisitionStatus? Status,
    string Sort,
    PageRequest Page);

/// <summary>Sort allowlist of the listRecruitmentRequisitions operation.</summary>
public static class RequisitionSort
{
    public const string CreatedAt = "createdAt";
    public const string CreatedAtDescending = "-createdAt";
    public const string ClosingDate = "closingDate";
    public const string ClosingDateDescending = "-closingDate";

    public static IReadOnlyList<string> Allowed { get; } = [CreatedAt, CreatedAtDescending, ClosingDate, ClosingDateDescending];

    public static bool IsAllowed(string? sort) =>
        sort is CreatedAt or CreatedAtDescending or ClosingDate or ClosingDateDescending;

    /// <summary>Blank sort falls back to the contract default (<c>-createdAt</c>).</summary>
    public static string Normalize(string? sort) => string.IsNullOrWhiteSpace(sort) ? CreatedAtDescending : sort.Trim();
}
