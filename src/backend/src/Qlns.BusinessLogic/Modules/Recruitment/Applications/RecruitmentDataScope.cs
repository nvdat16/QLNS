namespace Qlns.BusinessLogic.Modules.Recruitment.Applications;

public sealed record RecruitmentDataScope(
    bool OrganizationWide,
    IReadOnlySet<long> DepartmentIds)
{
    public static RecruitmentDataScope Organization { get; } = new(true, new HashSet<long>());
}
