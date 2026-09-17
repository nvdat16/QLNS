using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Recruitment.Applications;

public sealed record AdvanceApplicationCommand(
    long ApplicationId,
    ApplicationStage TargetStage,
    long ExpectedVersion,
    string? Reason,
    CoreHrActor Actor);
