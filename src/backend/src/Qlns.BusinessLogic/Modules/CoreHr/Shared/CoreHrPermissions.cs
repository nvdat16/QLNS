namespace Qlns.BusinessLogic.Modules.CoreHr.Shared;

/// <summary>
/// Permission claim values carried by the access token (claim type <c>permission</c>).
/// Endpoint-level policies in Qlns.Api require one of these; business services check the
/// finer-grained ones (field policy, action-specific approval) themselves.
/// </summary>
public static class CoreHrPermissions
{
    public const string EmployeeRead = "corehr.employee.read";
    public const string EmployeeReadSensitive = "corehr.employee.read_sensitive";
    public const string EmployeeProfileUpdate = "corehr.employee.profile.update";
    public const string EmployeeProfileManage = "corehr.employee.profile.manage";

    public const string OrganizationRead = "corehr.organization.read";
    public const string OrganizationManage = "corehr.organization.manage";

    public const string OnboardingRead = "corehr.onboarding.read";
    public const string OnboardingManage = "corehr.onboarding.manage";
    public const string OnboardingReopen = "corehr.onboarding.reopen";

    public const string EventRead = "corehr.event.read";
    public const string EventWrite = "corehr.event.write";
    public const string EventApprove = "corehr.event.approve";

    public const string DocumentRead = "corehr.document.read";
    public const string DocumentReadSensitive = "corehr.document.read_sensitive";
    public const string DocumentUpload = "corehr.document.upload";
}
