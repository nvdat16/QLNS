using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.Employees;

public sealed record UpdatePersonalProfileCommand(
    long EmployeeId,
    long ExpectedVersion,
    PersonalProfilePatch Patch,
    CoreHrActor Actor);
