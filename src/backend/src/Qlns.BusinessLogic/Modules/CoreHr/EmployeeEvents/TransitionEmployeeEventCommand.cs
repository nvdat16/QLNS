using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.EmployeeEvents;

public sealed record TransitionEmployeeEventCommand(
    long EventId,
    EmployeeEventAction Action,
    long ExpectedVersion,
    string? Reason,
    CoreHrActor Actor);
