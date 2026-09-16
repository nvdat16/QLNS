namespace Qlns.BusinessLogic.Modules.Recruitment.Applications;

public sealed record AdvanceApplicationCommand(
    long ApplicationId,
    ApplicationStage TargetStage,
    long ExpectedVersion,
    long ActorUserId,
    RecruitmentDataScope DataScope,
    string CorrelationId,
    string? Reason);
