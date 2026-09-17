using Qlns.BusinessLogic.Modules.CoreHr.Employees;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Xunit;
using static Qlns.BusinessLogic.UnitTests.Modules.CoreHr.Employees.EmployeeTestData;

namespace Qlns.BusinessLogic.UnitTests.Modules.CoreHr.Employees;

public sealed class EmployeeDirectoryServiceTests
{
    private static readonly CoreHrActor Self = Actor(SelfId, CoreHrDataScope.Self, CoreHrPermissions.EmployeeRead, CoreHrPermissions.EmployeeProfileUpdate);
    private static readonly CoreHrActor Colleague = Actor(ColleagueId, CoreHrDataScope.Departments(EngineeringDepartmentId), CoreHrPermissions.EmployeeRead, CoreHrPermissions.EmployeeProfileUpdate);
    private static readonly CoreHrActor HrManager = Actor(900, CoreHrDataScope.Organization,
        CoreHrPermissions.EmployeeRead, CoreHrPermissions.EmployeeReadSensitive, CoreHrPermissions.EmployeeProfileManage);

    // ---------- SearchAsync ----------

    [Fact]
    public async Task SearchAsync_UnknownSort_ThrowsValidationOnSortWithoutQueryingRepository()
    {
        var repository = new FakeEmployeeRepository(Employee());
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() => service.SearchAsync(
            Query(sort: "salary"), HrManager, CancellationToken.None));

        Assert.Contains("sort", exception.Errors.Keys);
        Assert.Equal(0, repository.SearchCalls);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("-name")]
    [InlineData("employeeCode")]
    [InlineData("hireDate")]
    public async Task SearchAsync_AllowedSort_IsPassedToRepository(string sort)
    {
        var repository = new FakeEmployeeRepository(Employee());
        var service = CreateService(repository);

        var result = await service.SearchAsync(Query(sort: sort), HrManager, CancellationToken.None);

        Assert.Equal(sort, repository.LastQuery!.Sort);
        Assert.Same(HrManager, repository.LastActor);
        Assert.Single(result.Items);
        Assert.Equal(1, result.TotalItems);
    }

    [Fact]
    public async Task SearchAsync_BlankSort_FallsBackToName()
    {
        var repository = new FakeEmployeeRepository(Employee());
        var service = CreateService(repository);

        await service.SearchAsync(Query(sort: "  "), HrManager, CancellationToken.None);

        Assert.Equal(EmployeeSort.Name, repository.LastQuery!.Sort);
    }

    [Fact]
    public async Task SearchAsync_TrimsSearchAndTreatsBlankAsNull()
    {
        var repository = new FakeEmployeeRepository(Employee());
        var service = CreateService(repository);

        await service.SearchAsync(Query(search: "  EMP-0001 "), HrManager, CancellationToken.None);
        Assert.Equal("EMP-0001", repository.LastQuery!.Search);

        await service.SearchAsync(Query(search: "   "), HrManager, CancellationToken.None);
        Assert.Null(repository.LastQuery!.Search);
    }

    [Fact]
    public async Task SearchAsync_PreservesFiltersAndPage()
    {
        var repository = new FakeEmployeeRepository(Employee());
        var service = CreateService(repository);
        var page = new PageRequest(2, 50);

        await service.SearchAsync(
            new EmployeeSearchQuery("an", EngineeringDepartmentId, 5, EmployeeStatus.Probation, "hireDate", page),
            HrManager,
            CancellationToken.None);

        var query = repository.LastQuery!;
        Assert.Equal(EngineeringDepartmentId, query.DepartmentId);
        Assert.Equal(5, query.PositionId);
        Assert.Equal(EmployeeStatus.Probation, query.Status);
        Assert.Same(page, query.Page);
    }

    // ---------- GetAsync ----------

    [Fact]
    public async Task GetAsync_Self_ReturnsFullVisibility()
    {
        var service = CreateService(new FakeEmployeeRepository(Employee()));

        var view = await service.GetAsync(SelfId, Self, CancellationToken.None);

        Assert.Equal(SelfId, view.Employee.Id);
        Assert.Equal(EmployeeFieldVisibility.Full, view.Visibility);
    }

    [Fact]
    public async Task GetAsync_ColleagueWithoutSensitivePermission_ReturnsPublicVisibility()
    {
        var service = CreateService(new FakeEmployeeRepository(Employee()));

        var view = await service.GetAsync(SelfId, Colleague, CancellationToken.None);

        Assert.Equal(EmployeeFieldVisibility.Public, view.Visibility);
    }

    [Fact]
    public async Task GetAsync_ActorWithReadSensitive_ReturnsFullVisibility()
    {
        var service = CreateService(new FakeEmployeeRepository(Employee()));

        var view = await service.GetAsync(SelfId, HrManager, CancellationToken.None);

        Assert.Equal(EmployeeFieldVisibility.Full, view.Visibility);
    }

    [Fact]
    public async Task GetAsync_Missing_ThrowsNotFound()
    {
        var service = CreateService(new FakeEmployeeRepository());

        var exception = await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            service.GetAsync(999, HrManager, CancellationToken.None));

        Assert.Equal("Employee", exception.Resource);
        Assert.Equal(999, exception.Id);
    }

    [Fact]
    public async Task GetAsync_OutsideDataScope_ThrowsNotFoundInsteadOfForbidden()
    {
        var service = CreateService(new FakeEmployeeRepository(
            Employee(id: OtherDepartmentEmployeeId, departmentId: FinanceDepartmentId)));

        await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            service.GetAsync(OtherDepartmentEmployeeId, Colleague, CancellationToken.None));
    }

    // ---------- UpdatePersonalProfileAsync ----------

    [Fact]
    public async Task UpdatePersonalProfileAsync_SelfUpdatesPhone_BumpsVersionAndSavesOnce()
    {
        var repository = new FakeEmployeeRepository(Employee(version: 3));
        var service = CreateService(repository);

        var view = await service.UpdatePersonalProfileAsync(
            Command(SelfId, 3, Patch(phone: Set("0987 654 321")), Self),
            CancellationToken.None);

        Assert.Equal("0987 654 321", view.Employee.Phone);
        Assert.Equal(4, view.Employee.Version);
        Assert.Equal(Now, view.Employee.UpdatedAt);
        Assert.Equal(EmployeeFieldVisibility.Full, view.Visibility);
        Assert.Equal(1, repository.SaveCalls);
        Assert.Equal(["phone"], repository.LastChangedFields);
        Assert.Equal(3, repository.LastExpectedVersion);
        Assert.Same(Self, repository.LastSaveActor);
    }

    [Fact]
    public async Task UpdatePersonalProfileAsync_HrWithProfileManage_CanUpdateAnotherEmployee()
    {
        var repository = new FakeEmployeeRepository(Employee(version: 1));
        var service = CreateService(repository);

        var view = await service.UpdatePersonalProfileAsync(
            Command(SelfId, 1, Patch(temporaryAddress: Set("99 Nguyen Hue, HCMC")), HrManager),
            CancellationToken.None);

        Assert.Equal("99 Nguyen Hue, HCMC", view.Employee.TemporaryAddress);
        Assert.Equal(2, view.Employee.Version);
        Assert.Equal(1, repository.SaveCalls);
        Assert.Equal(["temporaryAddress"], repository.LastChangedFields);
    }

    [Fact]
    public async Task UpdatePersonalProfileAsync_ColleagueWithoutManage_ThrowsForbiddenWithoutSaving()
    {
        var repository = new FakeEmployeeRepository(Employee(version: 1));
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() => service.UpdatePersonalProfileAsync(
            Command(SelfId, 1, Patch(phone: Set("0123456789")), Colleague),
            CancellationToken.None));

        Assert.Equal("corehr.employee.profile_forbidden", exception.Code);
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task UpdatePersonalProfileAsync_OutsideScope_ThrowsNotFound()
    {
        var repository = new FakeEmployeeRepository(Employee(id: OtherDepartmentEmployeeId, departmentId: FinanceDepartmentId));
        var service = CreateService(repository);

        await Assert.ThrowsAsync<CoreHrNotFoundException>(() => service.UpdatePersonalProfileAsync(
            Command(OtherDepartmentEmployeeId, 3, Patch(phone: Set("0123456789")), Colleague),
            CancellationToken.None));

        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task UpdatePersonalProfileAsync_StaleVersion_ThrowsConcurrencyBeforeSave()
    {
        var repository = new FakeEmployeeRepository(Employee(version: 5));
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => service.UpdatePersonalProfileAsync(
            Command(SelfId, 4, Patch(phone: Set("0123456789")), Self),
            CancellationToken.None));

        Assert.Equal("employee", exception.Resource);
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task UpdatePersonalProfileAsync_SaveLosesRace_ThrowsConcurrency()
    {
        var repository = new FakeEmployeeRepository(Employee(version: 2)) { SaveSucceeds = false };
        var service = CreateService(repository);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => service.UpdatePersonalProfileAsync(
            Command(SelfId, 2, Patch(phone: Set("0123456789")), Self),
            CancellationToken.None));

        Assert.Equal(1, repository.SaveCalls);
    }

    [Fact]
    public async Task UpdatePersonalProfileAsync_EmptyPatch_ThrowsValidationOnPatch()
    {
        var repository = new FakeEmployeeRepository(Employee(version: 1));
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() => service.UpdatePersonalProfileAsync(
            Command(SelfId, 1, PersonalProfilePatch.Empty, Self),
            CancellationToken.None));

        Assert.Equal(["At least one field is required."], exception.Errors["patch"]);
        Assert.Equal(0, repository.SaveCalls);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("missing@tld")]
    [InlineData("two@@example.com")]
    [InlineData("spaces in@example.com")]
    public async Task UpdatePersonalProfileAsync_InvalidEmail_ThrowsValidationOnPersonalEmail(string email)
    {
        var repository = new FakeEmployeeRepository(Employee(version: 1));
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() => service.UpdatePersonalProfileAsync(
            Command(SelfId, 1, Patch(personalEmail: Set(email)), Self),
            CancellationToken.None));

        Assert.Contains("personalEmail", exception.Errors.Keys);
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task UpdatePersonalProfileAsync_PhoneWithLetters_ThrowsValidationOnPhone()
    {
        var repository = new FakeEmployeeRepository(Employee(version: 1));
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() => service.UpdatePersonalProfileAsync(
            Command(SelfId, 1, Patch(phone: Set("09xx 123")), Self),
            CancellationToken.None));

        Assert.Contains("phone", exception.Errors.Keys);
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task UpdatePersonalProfileAsync_ExplicitNull_ClearsField()
    {
        var repository = new FakeEmployeeRepository(Employee(version: 1, personalEmail: "old@example.com"));
        var service = CreateService(repository);

        var view = await service.UpdatePersonalProfileAsync(
            Command(SelfId, 1, Patch(personalEmail: Set(null)), Self),
            CancellationToken.None);

        Assert.Null(view.Employee.PersonalEmail);
        Assert.Equal(2, view.Employee.Version);
        Assert.Equal(["personalEmail"], repository.LastChangedFields);
    }

    [Fact]
    public async Task UpdatePersonalProfileAsync_EmergencyContactViolations_ThrowValidation()
    {
        var repository = new FakeEmployeeRepository(Employee(version: 1));
        var service = CreateService(repository);

        var tooMany = Enumerable.Range(1, 11).ToDictionary(i => $"k{i}", i => $"v{i}");
        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() => service.UpdatePersonalProfileAsync(
            Command(SelfId, 1, Patch(emergencyContact: SetContact(tooMany)), Self),
            CancellationToken.None));
        Assert.Contains("emergencyContact", exception.Errors.Keys);

        var blankValue = new Dictionary<string, string> { ["name"] = "  " };
        exception = await Assert.ThrowsAsync<CoreHrValidationException>(() => service.UpdatePersonalProfileAsync(
            Command(SelfId, 1, Patch(emergencyContact: SetContact(blankValue)), Self),
            CancellationToken.None));
        Assert.Contains("emergencyContact.name", exception.Errors.Keys);

        var blankKey = new Dictionary<string, string> { [" "] = "Binh" };
        exception = await Assert.ThrowsAsync<CoreHrValidationException>(() => service.UpdatePersonalProfileAsync(
            Command(SelfId, 1, Patch(emergencyContact: SetContact(blankKey)), Self),
            CancellationToken.None));
        Assert.Contains("emergencyContact", exception.Errors.Keys);

        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task UpdatePersonalProfileAsync_ValidEmergencyContact_IsAppliedAndAuditedByFieldNameOnly()
    {
        var repository = new FakeEmployeeRepository(Employee(version: 1));
        var service = CreateService(repository);
        var contact = new Dictionary<string, string> { ["name"] = "Chi Nguyen", ["relationship"] = "sister", ["phone"] = "0911111111" };

        var view = await service.UpdatePersonalProfileAsync(
            Command(SelfId, 1, Patch(emergencyContact: SetContact(contact)), Self),
            CancellationToken.None);

        Assert.Equal("Chi Nguyen", view.Employee.EmergencyContact!["name"]);
        Assert.Equal(["emergencyContact"], repository.LastChangedFields);
    }

    [Fact]
    public async Task UpdatePersonalProfileAsync_NoEffectiveChange_DoesNotSaveOrBumpVersion()
    {
        var repository = new FakeEmployeeRepository(Employee(version: 3, phone: "0123456789"));
        var service = CreateService(repository);

        var view = await service.UpdatePersonalProfileAsync(
            Command(SelfId, 3, Patch(phone: Set(" 0123456789 ")), Self),
            CancellationToken.None);

        Assert.Equal(3, view.Employee.Version);
        Assert.Equal(0, repository.SaveCalls);
    }

    [Fact]
    public async Task UpdatePersonalProfileAsync_MultipleFields_ReportsAllChangedFields()
    {
        var repository = new FakeEmployeeRepository(Employee(version: 1));
        var service = CreateService(repository);

        await service.UpdatePersonalProfileAsync(
            Command(SelfId, 1, Patch(personalEmail: Set("new@example.com"), phone: Set("0999999999"), temporaryAddress: Set(null)), Self),
            CancellationToken.None);

        Assert.Equal(["personalEmail", "phone", "temporaryAddress"], repository.LastChangedFields);
    }

    // ---------- helpers ----------

    private static EmployeeDirectoryService CreateService(FakeEmployeeRepository repository) =>
        new(repository, new FixedTimeProvider(Now));

    private static EmployeeSearchQuery Query(string? search = null, string sort = EmployeeSort.Name) =>
        new(search, null, null, null, sort, PageRequest.Default);

    private static UpdatePersonalProfileCommand Command(long employeeId, long expectedVersion, PersonalProfilePatch patch, CoreHrActor actor) =>
        new(employeeId, expectedVersion, patch, actor);

    private sealed class FakeEmployeeRepository(params Employee[] employees) : IEmployeeRepository
    {
        private readonly List<Employee> _employees = [.. employees];

        public bool SaveSucceeds { get; init; } = true;
        public int SearchCalls { get; private set; }
        public int SaveCalls { get; private set; }
        public EmployeeSearchQuery? LastQuery { get; private set; }
        public CoreHrActor? LastActor { get; private set; }
        public CoreHrActor? LastSaveActor { get; private set; }
        public long LastExpectedVersion { get; private set; }
        public IReadOnlyList<string>? LastChangedFields { get; private set; }

        public Task<PagedResult<Employee>> SearchAsync(EmployeeSearchQuery query, CoreHrActor actor, CancellationToken cancellationToken)
        {
            SearchCalls++;
            LastQuery = query;
            LastActor = actor;
            var visible = _employees.Where(x => actor.CanAccessEmployee(x.Id, x.DepartmentId)).ToList();
            return Task.FromResult(new PagedResult<Employee>(visible, query.Page.Page, query.Page.PageSize, visible.Count));
        }

        public Task<Employee?> GetByIdAsync(long employeeId, CoreHrActor actor, CancellationToken cancellationToken) =>
            Task.FromResult(_employees.SingleOrDefault(x => x.Id == employeeId && actor.CanAccessEmployee(x.Id, x.DepartmentId)));

        public Task<bool> SavePersonalProfileAsync(
            Employee employee,
            long expectedVersion,
            IReadOnlyList<string> changedFields,
            CoreHrActor actor,
            CancellationToken cancellationToken)
        {
            SaveCalls++;
            LastExpectedVersion = expectedVersion;
            LastChangedFields = changedFields;
            LastSaveActor = actor;
            return Task.FromResult(SaveSucceeds);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
