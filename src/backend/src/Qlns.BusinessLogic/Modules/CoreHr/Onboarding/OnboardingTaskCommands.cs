using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.Onboarding;

public sealed record TransitionOnboardingTaskCommand(
    long TaskId,
    OnboardingTaskAction Action,
    long ExpectedVersion,
    string? Reason,
    CoreHrActor Actor);

public sealed record UpdateOnboardingTaskCommand(
    long TaskId,
    long ExpectedVersion,
    OnboardingTaskWrite Write,
    CoreHrActor Actor);
