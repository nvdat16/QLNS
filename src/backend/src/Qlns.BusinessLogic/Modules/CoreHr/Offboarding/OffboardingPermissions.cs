namespace Qlns.BusinessLogic.Modules.CoreHr.Offboarding;

/// <summary>
/// Permission claim values of Core HR → Offboarding (EMP-07). Endpoint policies require the coarse
/// claim; <see cref="OffboardingCaseService"/> and <see cref="OffboardingTaskService"/> re-check the action-level ones
/// (approve, completion override, task reopen).
/// </summary>
public static class OffboardingPermissions
{
    public const string Read = "corehr.offboarding.read";
    public const string Write = "corehr.offboarding.write";
    public const string Approve = "corehr.offboarding.approve";
}
