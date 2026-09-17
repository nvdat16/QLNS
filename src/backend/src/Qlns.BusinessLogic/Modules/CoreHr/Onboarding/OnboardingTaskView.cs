namespace Qlns.BusinessLogic.Modules.CoreHr.Onboarding;

/// <summary>Read model returned by the service: the task plus the time-dependent overdue flag.</summary>
public sealed record OnboardingTaskView(OnboardingTask Task, bool Overdue);
