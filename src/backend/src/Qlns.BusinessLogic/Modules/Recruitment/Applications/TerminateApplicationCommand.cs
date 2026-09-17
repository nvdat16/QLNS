using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Recruitment.Applications;

public sealed record TerminateApplicationCommand(
    long ApplicationId,
    ApplicationTerminalAction Action,
    long ExpectedVersion,
    string? Reason,
    CoreHrActor Actor);
