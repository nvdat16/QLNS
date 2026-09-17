using Microsoft.AspNetCore.Authorization;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.Api.Modules.CoreHr.Shared;

/// <summary>
/// Endpoint-level authorization policies for Core HR. Each policy requires the matching
/// <c>permission</c> claim; data scope and field policy are enforced afterwards by the business services.
/// </summary>
public static class CoreHrPolicies
{
    public const string EmployeeRead = "CoreHrEmployeeRead";
    public const string EmployeeProfileUpdate = "CoreHrEmployeeProfileUpdate";
    public const string OrganizationRead = "CoreHrOrganizationRead";
    public const string OrganizationManage = "CoreHrOrganizationManage";
    public const string OnboardingRead = "CoreHrOnboardingRead";
    public const string OnboardingManage = "CoreHrOnboardingManage";
    public const string EventRead = "CoreHrEventRead";
    public const string EventWrite = "CoreHrEventWrite";
    public const string DocumentRead = "CoreHrDocumentRead";
    public const string DocumentUpload = "CoreHrDocumentUpload";

    public static void AddCoreHrPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(EmployeeRead, p => p.RequireClaim("permission", CoreHrPermissions.EmployeeRead));
        options.AddPolicy(EmployeeProfileUpdate, p => p.RequireClaim("permission",
            CoreHrPermissions.EmployeeProfileUpdate, CoreHrPermissions.EmployeeProfileManage));
        options.AddPolicy(OrganizationRead, p => p.RequireClaim("permission", CoreHrPermissions.OrganizationRead));
        options.AddPolicy(OrganizationManage, p => p.RequireClaim("permission", CoreHrPermissions.OrganizationManage));
        options.AddPolicy(OnboardingRead, p => p.RequireClaim("permission", CoreHrPermissions.OnboardingRead));
        options.AddPolicy(OnboardingManage, p => p.RequireClaim("permission", CoreHrPermissions.OnboardingManage));
        options.AddPolicy(EventRead, p => p.RequireClaim("permission", CoreHrPermissions.EventRead));
        options.AddPolicy(EventWrite, p => p.RequireClaim("permission",
            CoreHrPermissions.EventWrite, CoreHrPermissions.EventApprove));
        options.AddPolicy(DocumentRead, p => p.RequireClaim("permission", CoreHrPermissions.DocumentRead));
        options.AddPolicy(DocumentUpload, p => p.RequireClaim("permission", CoreHrPermissions.DocumentUpload));
    }
}
