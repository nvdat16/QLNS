using Qlns.BusinessLogic.Modules.CoreHr.EmployeeDocuments;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Xunit;

namespace Qlns.BusinessLogic.UnitTests.Modules.CoreHr.EmployeeDocuments;

public sealed class DocumentAccessPolicyTests
{
    private const long EmployeeId = 10;
    private const long DepartmentId = 3;
    private const long OtherDepartmentId = 99;

    [Fact]
    public void CanRead_StandardDocument_VisibleToAnyoneInScope()
    {
        var document = Document(DocumentType.Degree);

        Assert.True(DocumentAccessPolicy.CanRead(Colleague(), document, DepartmentId));
        Assert.True(DocumentAccessPolicy.CanRead(Self(), document, DepartmentId));
        Assert.True(DocumentAccessPolicy.CanRead(HrSensitive(), document, DepartmentId));
    }

    [Theory]
    [InlineData(DocumentType.NationalId)]
    [InlineData(DocumentType.Contract)]
    [InlineData(DocumentType.BankAccount)]
    public void CanRead_PersonalDocument_VisibleToSelfAndHrSensitiveOnly(string type)
    {
        var document = Document(type);

        Assert.True(DocumentAccessPolicy.CanRead(Self(), document, DepartmentId));
        Assert.True(DocumentAccessPolicy.CanRead(HrSensitive(), document, DepartmentId));
        Assert.False(DocumentAccessPolicy.CanRead(Colleague(), document, DepartmentId));
    }

    [Fact]
    public void CanRead_HrOnlyDocument_HiddenFromSelfVisibleToHrSensitive()
    {
        var document = Document(DocumentType.DisciplinaryRecord);

        Assert.False(DocumentAccessPolicy.CanRead(Self(), document, DepartmentId));
        Assert.False(DocumentAccessPolicy.CanRead(Colleague(), document, DepartmentId));
        Assert.True(DocumentAccessPolicy.CanRead(HrSensitive(), document, DepartmentId));
    }

    [Fact]
    public void CanRead_UnknownStoredType_FailsClosedToHrSensitive()
    {
        var document = Document("legacy_scan");

        Assert.False(DocumentAccessPolicy.CanRead(Self(), document, DepartmentId));
        Assert.True(DocumentAccessPolicy.CanRead(HrSensitive(), document, DepartmentId));
    }

    [Theory]
    [InlineData(DocumentType.Degree)]
    [InlineData(DocumentType.Contract)]
    [InlineData(DocumentType.DisciplinaryRecord)]
    public void CanRead_OutOfScope_AlwaysFalse(string type)
    {
        var document = Document(type);
        var hrOfOtherDepartment = Actor(employeeId: 500, CoreHrDataScope.Departments(OtherDepartmentId),
            CoreHrPermissions.DocumentRead, CoreHrPermissions.DocumentReadSensitive);

        Assert.False(DocumentAccessPolicy.CanRead(hrOfOtherDepartment, document, DepartmentId));
        Assert.False(DocumentAccessPolicy.CanRead(Colleague(), document, OtherDepartmentId));
    }

    [Fact]
    public void CanUpload_SelfMayUploadStandardButNotPersonalOrHrOnly()
    {
        var self = Self();

        Assert.True(DocumentAccessPolicy.CanUpload(self, Type(DocumentType.Resume), EmployeeId, DepartmentId));
        Assert.False(DocumentAccessPolicy.CanUpload(self, Type(DocumentType.NationalId), EmployeeId, DepartmentId));
        Assert.False(DocumentAccessPolicy.CanUpload(self, Type(DocumentType.DisciplinaryRecord), EmployeeId, DepartmentId));
    }

    [Fact]
    public void CanUpload_HrSensitiveMayUploadEveryTypeInScope()
    {
        var hr = HrSensitive();

        foreach (var type in DocumentType.All)
        {
            Assert.True(DocumentAccessPolicy.CanUpload(hr, type, EmployeeId, DepartmentId));
        }

        Assert.False(DocumentAccessPolicy.CanUpload(
            Actor(employeeId: 500, CoreHrDataScope.Departments(OtherDepartmentId), CoreHrPermissions.DocumentReadSensitive),
            Type(DocumentType.Degree),
            EmployeeId,
            DepartmentId));
    }

    [Fact]
    public void CanUpload_ColleagueWithoutSensitivePermission_StandardOnly()
    {
        var colleague = Colleague();

        Assert.True(DocumentAccessPolicy.CanUpload(colleague, Type(DocumentType.Certificate), EmployeeId, DepartmentId));
        Assert.False(DocumentAccessPolicy.CanUpload(colleague, Type(DocumentType.Contract), EmployeeId, DepartmentId));
    }

    private static DocumentTypeInfo Type(string code)
    {
        Assert.True(DocumentType.TryParse(code, out var info));
        return info;
    }

    private static EmployeeDocument Document(string type) => new(
        id: 1,
        EmployeeId,
        type,
        version: 1,
        originalFileName: "scan.pdf",
        objectKey: "employees/10/x/v1/abc",
        contentType: "application/pdf",
        sizeBytes: 100,
        uploadedBy: 7,
        uploadedAt: DateTimeOffset.UnixEpoch,
        retentionUntil: null,
        deletedAt: null);

    private static CoreHrActor Self() =>
        Actor(EmployeeId, CoreHrDataScope.Self, CoreHrPermissions.DocumentRead);

    private static CoreHrActor Colleague() =>
        Actor(employeeId: 11, CoreHrDataScope.Departments(DepartmentId), CoreHrPermissions.DocumentRead);

    private static CoreHrActor HrSensitive() =>
        Actor(employeeId: 200, CoreHrDataScope.Organization,
            CoreHrPermissions.DocumentRead, CoreHrPermissions.DocumentReadSensitive, CoreHrPermissions.DocumentUpload);

    private static CoreHrActor Actor(long employeeId, CoreHrDataScope scope, params string[] permissions) => new(
        UserId: employeeId + 1000,
        EmployeeId: employeeId,
        DataScope: scope,
        Permissions: permissions.ToHashSet(StringComparer.Ordinal),
        CorrelationId: "test");
}
