namespace Qlns.BusinessLogic.Modules.Recruitment.Requisitions;

/// <summary>Working arrangement of a requisition (OpenAPI EmploymentType, job_postings.employment_type).</summary>
public enum EmploymentType
{
    FullTime,
    PartTime,
    Hybrid,
    Remote,
    Internship,
    ServiceContract
}

public static class EmploymentTypeNames
{
    public static IReadOnlyList<string> ContractValues { get; } =
        ["full_time", "part_time", "hybrid", "remote", "internship", "service_contract"];

    public static string ToContract(this EmploymentType type) => type switch
    {
        EmploymentType.FullTime => "full_time",
        EmploymentType.PartTime => "part_time",
        EmploymentType.Hybrid => "hybrid",
        EmploymentType.Remote => "remote",
        EmploymentType.Internship => "internship",
        EmploymentType.ServiceContract => "service_contract",
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public static bool TryParseContract(string? value, out EmploymentType type)
    {
        type = value switch
        {
            "full_time" => EmploymentType.FullTime,
            "part_time" => EmploymentType.PartTime,
            "hybrid" => EmploymentType.Hybrid,
            "remote" => EmploymentType.Remote,
            "internship" => EmploymentType.Internship,
            "service_contract" => EmploymentType.ServiceContract,
            _ => default
        };

        return value is "full_time" or "part_time" or "hybrid" or "remote" or "internship" or "service_contract";
    }
}
