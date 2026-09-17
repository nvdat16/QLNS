using Qlns.BusinessLogic.Modules.CoreHr.Employees;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.UnitTests.Modules.CoreHr.Employees;

internal static class EmployeeTestData
{
    public static readonly DateTimeOffset Now = new(2026, 9, 15, 7, 0, 0, TimeSpan.Zero);
    public static readonly DateTimeOffset Earlier = Now.AddDays(-30);

    public const long SelfId = 42;
    public const long ColleagueId = 43;
    public const long OtherDepartmentEmployeeId = 44;
    public const long EngineeringDepartmentId = 10;
    public const long FinanceDepartmentId = 11;

    public static Employee Employee(
        long id = SelfId,
        long departmentId = EngineeringDepartmentId,
        long version = 3,
        string? phone = "+84 912 345 678",
        string? personalEmail = "an.nguyen@example.com",
        string? temporaryAddress = "12 Ly Thuong Kiet, Ha Noi",
        IReadOnlyDictionary<string, string>? emergencyContact = null) => new(
        id,
        $"EMP-{id:00000}",
        sourceApplicationId: null,
        userId: id + 1000,
        firstName: "An",
        lastName: "Nguyen",
        workEmail: $"an.nguyen{id}@qlns.example",
        personalEmail,
        phone,
        dateOfBirth: new DateOnly(1995, 4, 20),
        gender: "female",
        officeLocation: "Ha Noi",
        permanentAddress: "1 Tran Hung Dao, Ha Noi",
        temporaryAddress,
        emergencyContact ?? new Dictionary<string, string> { ["name"] = "Binh Nguyen", ["phone"] = "0900000000" },
        managerId: 7,
        departmentId,
        positionId: 5,
        hireDate: new DateOnly(2024, 1, 15),
        status: EmployeeStatus.Active,
        version,
        createdAt: Earlier,
        updatedAt: Earlier);

    public static CoreHrActor Actor(
        long? employeeId,
        CoreHrDataScope? scope = null,
        params string[] permissions) => new(
        UserId: (employeeId ?? 0) + 1000,
        EmployeeId: employeeId,
        DataScope: scope ?? CoreHrDataScope.Self,
        Permissions: new HashSet<string>(permissions, StringComparer.Ordinal),
        CorrelationId: "test-correlation");

    public static PersonalProfilePatch Patch(
        Optional<string?> personalEmail = default,
        Optional<string?> phone = default,
        Optional<string?> temporaryAddress = default,
        Optional<IReadOnlyDictionary<string, string>?> emergencyContact = default) =>
        new(personalEmail, phone, temporaryAddress, emergencyContact);

    public static Optional<string?> Set(string? value) => Optional<string?>.Of(value);

    public static Optional<IReadOnlyDictionary<string, string>?> SetContact(IReadOnlyDictionary<string, string>? value) =>
        Optional<IReadOnlyDictionary<string, string>?>.Of(value);
}
