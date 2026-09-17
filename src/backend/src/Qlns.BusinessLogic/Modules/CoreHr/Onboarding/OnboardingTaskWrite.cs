namespace Qlns.BusinessLogic.Modules.CoreHr.Onboarding;

/// <summary>Assignment payload of <c>PUT /onboarding/tasks/{taskId}</c> (OpenAPI OnboardingTaskWrite).</summary>
public sealed record OnboardingTaskWrite(
    string TaskName,
    string? Description,
    long? AssignedToUserId,
    DateTimeOffset? DueAt);
