using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.EmployeeEvents;

public sealed record CreateEmployeeEventCommand(
    long EmployeeId,
    EmployeeEventWrite Write,
    CoreHrActor Actor);
