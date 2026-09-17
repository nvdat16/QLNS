namespace Qlns.BusinessLogic.Modules.CoreHr.Offboarding;

/// <summary>offboarding_tasks.category (ck_offboarding_task_category): the five checklist groups of EMP-07.</summary>
public enum OffboardingTaskCategory
{
    It,
    Admin,
    Hr,
    Manager,
    Finance
}

public static class OffboardingTaskCategoryNames
{
    public static string ToContract(this OffboardingTaskCategory category) => category switch
    {
        OffboardingTaskCategory.It => "it",
        OffboardingTaskCategory.Admin => "admin",
        OffboardingTaskCategory.Hr => "hr",
        OffboardingTaskCategory.Manager => "manager",
        OffboardingTaskCategory.Finance => "finance",
        _ => throw new ArgumentOutOfRangeException(nameof(category))
    };

    public static bool TryParseContract(string? value, out OffboardingTaskCategory category)
    {
        category = value switch
        {
            "it" => OffboardingTaskCategory.It,
            "admin" => OffboardingTaskCategory.Admin,
            "hr" => OffboardingTaskCategory.Hr,
            "manager" => OffboardingTaskCategory.Manager,
            "finance" => OffboardingTaskCategory.Finance,
            _ => default
        };

        return value is "it" or "admin" or "hr" or "manager" or "finance";
    }
}
