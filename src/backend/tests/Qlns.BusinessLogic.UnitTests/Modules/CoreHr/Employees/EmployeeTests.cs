using Qlns.BusinessLogic.Modules.CoreHr.Employees;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Xunit;
using static Qlns.BusinessLogic.UnitTests.Modules.CoreHr.Employees.EmployeeTestData;

namespace Qlns.BusinessLogic.UnitTests.Modules.CoreHr.Employees;

public sealed class EmployeeTests
{
    [Fact]
    public void Constructor_RejectsNonPositiveIdentifiersAndVersion()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Employee(id: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Employee(departmentId: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Employee(version: 0));
    }

    [Fact]
    public void FullName_JoinsFirstAndLast()
    {
        Assert.Equal("An Nguyen", Employee().FullName());
    }

    [Fact]
    public void ApplyPersonalProfilePatch_TrimsAndAppliesAllFourFields()
    {
        var employee = Employee(version: 1, emergencyContact: new Dictionary<string, string> { ["name"] = "Old" });
        var contact = new Dictionary<string, string> { [" name "] = " Binh ", ["phone"] = "0900000001" };

        var changed = employee.ApplyPersonalProfilePatch(
            Patch(Set(" new@example.com "), Set(" (+84) 912-345-678 "), Set(" 5 Le Loi "), SetContact(contact)),
            Now);

        Assert.Equal(["personalEmail", "phone", "temporaryAddress", "emergencyContact"], changed);
        Assert.Equal("new@example.com", employee.PersonalEmail);
        Assert.Equal("(+84) 912-345-678", employee.Phone);
        Assert.Equal("5 Le Loi", employee.TemporaryAddress);
        Assert.Equal("Binh", employee.EmergencyContact!["name"]);
        Assert.Equal(2, employee.Version);
        Assert.Equal(Now, employee.UpdatedAt);
    }

    [Fact]
    public void ApplyPersonalProfilePatch_UnsetFields_AreLeftUntouched()
    {
        var employee = Employee(version: 1, phone: "0123", personalEmail: "keep@example.com");

        employee.ApplyPersonalProfilePatch(Patch(temporaryAddress: Set("New address")), Now);

        Assert.Equal("0123", employee.Phone);
        Assert.Equal("keep@example.com", employee.PersonalEmail);
        Assert.NotNull(employee.EmergencyContact);
    }

    [Fact]
    public void ApplyPersonalProfilePatch_ExplicitNull_ClearsEmergencyContact()
    {
        var employee = Employee(version: 1);

        var changed = employee.ApplyPersonalProfilePatch(Patch(emergencyContact: SetContact(null)), Now);

        Assert.Equal(["emergencyContact"], changed);
        Assert.Null(employee.EmergencyContact);
    }

    [Fact]
    public void ApplyPersonalProfilePatch_EmptyPatch_Throws422OnPatch()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Employee().ApplyPersonalProfilePatch(PersonalProfilePatch.Empty, Now));

        Assert.Equal(["At least one field is required."], exception.Errors["patch"]);
    }

    [Fact]
    public void ApplyPersonalProfilePatch_CollectsAllFieldErrorsAtOnce()
    {
        var employee = Employee(version: 1);

        var exception = Assert.Throws<CoreHrValidationException>(() => employee.ApplyPersonalProfilePatch(
            Patch(Set("bad"), Set("abc"), Set(new string('x', 1001)), SetContact(new Dictionary<string, string>())),
            Now));

        Assert.Equal(4, exception.Errors.Count);
        Assert.Contains("personalEmail", exception.Errors.Keys);
        Assert.Contains("phone", exception.Errors.Keys);
        Assert.Contains("temporaryAddress", exception.Errors.Keys);
        Assert.Contains("emergencyContact", exception.Errors.Keys);
        Assert.Equal(1, employee.Version);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("0123456789012345678901234567890")]
    [InlineData("+84 912.345.678")]
    public void ApplyPersonalProfilePatch_InvalidPhone_Throws(string phone)
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Employee().ApplyPersonalProfilePatch(Patch(phone: Set(phone)), Now));

        Assert.Contains("phone", exception.Errors.Keys);
    }

    [Fact]
    public void ApplyPersonalProfilePatch_EmergencyContactValueTooLong_Throws()
    {
        var contact = new Dictionary<string, string> { ["note"] = new string('a', 256) };

        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Employee().ApplyPersonalProfilePatch(Patch(emergencyContact: SetContact(contact)), Now));

        Assert.Contains("emergencyContact.note", exception.Errors.Keys);
    }

    [Fact]
    public void ApplyPersonalProfilePatch_SameValues_ReturnsNoChangesAndKeepsVersion()
    {
        var employee = Employee(version: 2, phone: "0123456789");

        var changed = employee.ApplyPersonalProfilePatch(Patch(phone: Set("0123456789")), Now);

        Assert.Empty(changed);
        Assert.Equal(2, employee.Version);
        Assert.Equal(Earlier, employee.UpdatedAt);
    }

    [Fact]
    public void EmployeeFieldPolicy_ResolvesVisibilityByActor()
    {
        var employee = Employee();

        Assert.Equal(EmployeeFieldVisibility.Full, EmployeeFieldPolicy.Resolve(Actor(SelfId), employee));
        Assert.Equal(EmployeeFieldVisibility.Public, EmployeeFieldPolicy.Resolve(Actor(ColleagueId, CoreHrDataScope.Organization, CoreHrPermissions.EmployeeRead), employee));
        Assert.Equal(EmployeeFieldVisibility.Full, EmployeeFieldPolicy.Resolve(Actor(ColleagueId, CoreHrDataScope.Organization, CoreHrPermissions.EmployeeReadSensitive), employee));
    }

    [Theory]
    [InlineData(EmployeeStatus.Probation, "probation")]
    [InlineData(EmployeeStatus.Active, "active")]
    [InlineData(EmployeeStatus.Suspended, "suspended")]
    [InlineData(EmployeeStatus.Terminated, "terminated")]
    public void EmployeeStatus_RoundTripsContractValues(EmployeeStatus status, string contract)
    {
        Assert.Equal(contract, status.ToContract());
        Assert.True(EmployeeStatusNames.TryParseContract(contract, out var parsed));
        Assert.Equal(status, parsed);
        Assert.False(EmployeeStatusNames.TryParseContract("Active", out _));
    }
}
