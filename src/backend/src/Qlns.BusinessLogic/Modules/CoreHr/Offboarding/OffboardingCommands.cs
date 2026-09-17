using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.Offboarding;

public sealed record CreateOffboardingCaseCommand(
    OffboardingCaseWrite Write,
    CoreHrActor Actor);

public sealed record TransitionOffboardingCaseCommand(
    long CaseId,
    OffboardingCaseAction Action,
    long ExpectedVersion,
    string? Reason,
    CoreHrActor Actor);

public sealed record TransitionOffboardingTaskCommand(
    long TaskId,
    OffboardingTaskAction Action,
    long ExpectedVersion,
    string? Reason,
    CoreHrActor Actor);
