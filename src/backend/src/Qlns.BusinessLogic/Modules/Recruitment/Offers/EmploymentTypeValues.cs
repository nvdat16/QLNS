namespace Qlns.BusinessLogic.Modules.Recruitment.Offers;

/// <summary>
/// offers.employment_type contract values (OpenAPI EmploymentType). Kept local so this feature does not depend on
/// the Requisitions feature; the strings are the same snake_case values stored in the database.
/// </summary>
public static class EmploymentTypeValues
{
    public const string FullTime = "full_time";
    public const string PartTime = "part_time";
    public const string Hybrid = "hybrid";
    public const string Remote = "remote";
    public const string Internship = "internship";
    public const string ServiceContract = "service_contract";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        FullTime, PartTime, Hybrid, Remote, Internship, ServiceContract
    };

    public static bool IsValid(string? value) => value is not null && All.Contains(value);
}
