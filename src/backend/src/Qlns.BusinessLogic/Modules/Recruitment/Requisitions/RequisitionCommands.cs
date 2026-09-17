using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Recruitment.Requisitions;

public sealed record CreateRequisitionCommand(
    RequisitionWrite Write,
    CoreHrActor Actor);

public sealed record ReplaceRequisitionCommand(
    long RequisitionId,
    long ExpectedVersion,
    RequisitionWrite Write,
    CoreHrActor Actor);

public sealed record TransitionRequisitionCommand(
    long RequisitionId,
    RequisitionAction Action,
    long ExpectedVersion,
    string? Reason,
    CoreHrActor Actor);
