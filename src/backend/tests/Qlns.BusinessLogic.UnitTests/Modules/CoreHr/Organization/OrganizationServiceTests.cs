using Qlns.BusinessLogic.Modules.CoreHr.Organization;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Xunit;

namespace Qlns.BusinessLogic.UnitTests.Modules.CoreHr.Organization;

public sealed class OrganizationServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 7, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Earlier = Now.AddDays(-30);

    private static readonly CoreHrActor Actor = new(
        UserId: 7,
        EmployeeId: null,
        DataScope: CoreHrDataScope.Organization,
        Permissions: new HashSet<string> { CoreHrPermissions.OrganizationManage },
        CorrelationId: "test-correlation");

    [Fact]
    public async Task CreateDepartmentAsync_ValidPayload_InsertsOnceWithVersionOne()
    {
        var departments = new FakeDepartmentRepository(Dept(1, "HQ", null));
        var service = CreateService(departments);

        var result = await service.CreateDepartmentAsync(
            new DepartmentWrite("  ENG-01 ", " Engineering ", 1, "CC-100", null),
            Actor,
            CancellationToken.None);

        Assert.Equal(1, departments.InsertCalls);
        Assert.Equal(100, result.Department.Id);
        Assert.Equal("ENG-01", result.Department.Code);
        Assert.Equal("Engineering", result.Department.Name);
        Assert.Equal(1, result.Department.ParentDepartmentId);
        Assert.Equal(1, result.Department.Version);
        Assert.Equal(Now, result.Department.CreatedAt);
        Assert.Equal(Now, result.Department.UpdatedAt);
        Assert.Equal(0, result.Headcount);
        Assert.Same(Actor, departments.LastActor);
    }

    [Fact]
    public async Task CreateDepartmentAsync_DuplicateCode_ThrowsCodeTakenWithoutInsert()
    {
        var departments = new FakeDepartmentRepository(Dept(1, "HQ", null, code: "ENG"));
        var service = CreateService(departments);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() => service.CreateDepartmentAsync(
            new DepartmentWrite("eng", "Engineering", null, null, null),
            Actor,
            CancellationToken.None));

        Assert.Equal("corehr.department.code_taken", exception.Code);
        Assert.Equal(0, departments.InsertCalls);
    }

    [Fact]
    public async Task CreateDepartmentAsync_UnknownParent_ThrowsValidationOnParentField()
    {
        var departments = new FakeDepartmentRepository(Dept(1, "HQ", null));
        var service = CreateService(departments);

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() => service.CreateDepartmentAsync(
            new DepartmentWrite("ENG", "Engineering", 999, null, null),
            Actor,
            CancellationToken.None));

        Assert.Contains("parentDepartmentId", exception.Errors.Keys);
        Assert.Equal(0, departments.InsertCalls);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("-ENG")]
    [InlineData("ENG 01")]
    [InlineData("ENG.01")]
    public async Task CreateDepartmentAsync_InvalidCode_ThrowsValidationOnCodeField(string code)
    {
        var departments = new FakeDepartmentRepository();
        var service = CreateService(departments);

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() => service.CreateDepartmentAsync(
            new DepartmentWrite(code, "Engineering", null, null, null),
            Actor,
            CancellationToken.None));

        Assert.Contains("code", exception.Errors.Keys);
        Assert.Equal(0, departments.CodeExistsCalls);
        Assert.Equal(0, departments.InsertCalls);
    }

    [Fact]
    public async Task ReplaceDepartmentAsync_StaleVersion_ThrowsConcurrencyWithoutSaving()
    {
        var departments = new FakeDepartmentRepository(Dept(1, "HQ", null, version: 3));
        var service = CreateService(departments);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => service.ReplaceDepartmentAsync(
            1,
            expectedVersion: 2,
            new DepartmentWrite("HQ", "Headquarters", null, null, null),
            Actor,
            CancellationToken.None));

        Assert.Equal(0, departments.ReplaceCalls);
    }

    [Fact]
    public async Task ReplaceDepartmentAsync_ParentIsDescendant_ThrowsHierarchyCycleWithoutSaving()
    {
        var departments = new FakeDepartmentRepository(
            Dept(1, "Board", null),
            Dept(2, "Engineering", 1),
            Dept(3, "Platform", 2));
        var service = CreateService(departments);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() => service.ReplaceDepartmentAsync(
            1,
            expectedVersion: 1,
            new DepartmentWrite("D1", "Board", 3, null, null),
            Actor,
            CancellationToken.None));

        Assert.Equal("corehr.department.hierarchy_cycle", exception.Code);
        Assert.Equal(0, departments.ReplaceCalls);
    }

    [Fact]
    public async Task ReplaceDepartmentAsync_Valid_BumpsVersionAndSavesWithExpectedVersion()
    {
        var departments = new FakeDepartmentRepository(Dept(1, "Board", null, version: 4), Dept(2, "Engineering", 1))
        {
            Headcounts = { [2] = 9 }
        };
        var service = CreateService(departments);

        var result = await service.ReplaceDepartmentAsync(
            2,
            expectedVersion: 1,
            new DepartmentWrite("ENG", "Engineering & Product", 1, "CC-200", "desc"),
            Actor,
            CancellationToken.None);

        Assert.Equal(1, departments.ReplaceCalls);
        Assert.Equal(1, departments.LastExpectedVersion);
        Assert.Equal(2, result.Department.Version);
        Assert.Equal("ENG", result.Department.Code);
        Assert.Equal(Now, result.Department.UpdatedAt);
        Assert.Equal(9, result.Headcount);
    }

    [Fact]
    public async Task ReplaceDepartmentAsync_ConcurrentWriteDuringSave_ThrowsConcurrency()
    {
        var departments = new FakeDepartmentRepository(Dept(1, "Board", null)) { WriteSucceeds = false };
        var service = CreateService(departments);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => service.ReplaceDepartmentAsync(
            1,
            expectedVersion: 1,
            new DepartmentWrite("BOARD", "Board", null, null, null),
            Actor,
            CancellationToken.None));

        Assert.Equal(1, departments.ReplaceCalls);
    }

    [Fact]
    public async Task ReplaceDepartmentAsync_UnknownDepartment_ThrowsNotFound()
    {
        var service = CreateService(new FakeDepartmentRepository());

        var exception = await Assert.ThrowsAsync<CoreHrNotFoundException>(() => service.ReplaceDepartmentAsync(
            42,
            expectedVersion: 1,
            new DepartmentWrite("X", "X", null, null, null),
            Actor,
            CancellationToken.None));

        Assert.Equal("Department", exception.Resource);
        Assert.Equal(42, exception.Id);
    }

    [Fact]
    public async Task DeleteDepartmentAsync_WithEmployees_ThrowsInUseWithDependencyCounts()
    {
        var departments = new FakeDepartmentRepository(Dept(1, "Engineering", null))
        {
            Dependencies = new DepartmentDependencies(ChildDepartments: 1, Employees: 12, OpenRequisitions: 2)
        };
        var service = CreateService(departments);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            service.DeleteDepartmentAsync(1, expectedVersion: 1, Actor, CancellationToken.None));

        Assert.Equal("corehr.department.in_use", exception.Code);
        Assert.Equal(1, exception.Details["childDepartments"]);
        Assert.Equal(12, exception.Details["employees"]);
        Assert.Equal(2, exception.Details["openRequisitions"]);
        Assert.Contains("employees", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("requisitions", exception.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(0, departments.DeleteCalls);
    }

    [Fact]
    public async Task DeleteDepartmentAsync_EmptyDepartment_Deletes()
    {
        var departments = new FakeDepartmentRepository(Dept(1, "Engineering", null, version: 2));
        var service = CreateService(departments);

        await service.DeleteDepartmentAsync(1, expectedVersion: 2, Actor, CancellationToken.None);

        Assert.Equal(1, departments.DeleteCalls);
        Assert.Equal(2, departments.LastExpectedVersion);
        Assert.Same(Actor, departments.LastActor);
    }

    [Fact]
    public async Task DeleteDepartmentAsync_StaleVersion_ThrowsConcurrencyBeforeDependencyCheck()
    {
        var departments = new FakeDepartmentRepository(Dept(1, "Engineering", null, version: 2));
        var service = CreateService(departments);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() =>
            service.DeleteDepartmentAsync(1, expectedVersion: 1, Actor, CancellationToken.None));

        Assert.Equal(0, departments.DependencyCalls);
        Assert.Equal(0, departments.DeleteCalls);
    }

    [Fact]
    public async Task CreatePositionAsync_DuplicateCode_ThrowsCodeTaken()
    {
        var positions = new FakePositionRepository(Pos(1, "SE1", "Software Engineer I"));
        var service = CreateService(positions: positions);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() => service.CreatePositionAsync(
            new PositionWrite("se1", "Duplicate", null, null),
            Actor,
            CancellationToken.None));

        Assert.Equal("corehr.position.code_taken", exception.Code);
        Assert.Equal(0, positions.InsertCalls);
    }

    [Fact]
    public async Task CreatePositionAsync_Valid_InsertsWithVersionOne()
    {
        var positions = new FakePositionRepository();
        var service = CreateService(positions: positions);

        var result = await service.CreatePositionAsync(
            new PositionWrite("SE2", "Software Engineer II", " L2 ", null),
            Actor,
            CancellationToken.None);

        Assert.Equal(1, positions.InsertCalls);
        Assert.Equal(500, result.Id);
        Assert.Equal("L2", result.Level);
        Assert.Equal(1, result.Version);
        Assert.Equal(Now, result.CreatedAt);
    }

    [Fact]
    public async Task ReplacePositionAsync_Valid_BumpsVersion()
    {
        var positions = new FakePositionRepository(Pos(1, "SE1", "Software Engineer I", version: 3));
        var service = CreateService(positions: positions);

        var result = await service.ReplacePositionAsync(
            1,
            expectedVersion: 3,
            new PositionWrite("SE1", "Software Engineer I (renamed)", "L1", null),
            Actor,
            CancellationToken.None);

        Assert.Equal(1, positions.ReplaceCalls);
        Assert.Equal(4, result.Version);
        Assert.Equal("Software Engineer I (renamed)", result.Name);
    }

    [Fact]
    public async Task ReplacePositionAsync_StaleVersion_ThrowsConcurrencyWithoutSaving()
    {
        var positions = new FakePositionRepository(Pos(1, "SE1", "Software Engineer I", version: 3));
        var service = CreateService(positions: positions);

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => service.ReplacePositionAsync(
            1,
            expectedVersion: 2,
            new PositionWrite("SE1", "Renamed", null, null),
            Actor,
            CancellationToken.None));

        Assert.Equal(0, positions.ReplaceCalls);
    }

    [Fact]
    public async Task ListDepartmentsAsync_ReturnsHeadcountPerDepartment()
    {
        var departments = new FakeDepartmentRepository(Dept(1, "HQ", null), Dept(2, "Engineering", 1))
        {
            Headcounts = { [2] = 15 }
        };
        var service = CreateService(departments);

        var list = await service.ListDepartmentsAsync(Actor, CancellationToken.None);

        Assert.Equal(2, list.Count);
        Assert.Equal(15, list.Single(item => item.Department.Id == 2).Headcount);
        Assert.Equal(0, list.Single(item => item.Department.Id == 1).Headcount);
    }

    private static OrganizationService CreateService(
        FakeDepartmentRepository? departments = null,
        FakePositionRepository? positions = null) => new(
        departments ?? new FakeDepartmentRepository(),
        positions ?? new FakePositionRepository(),
        new FixedTimeProvider(Now));

    private static Department Dept(long id, string name, long? parentId, string? code = null, long version = 1) => new(
        id,
        code ?? $"D{id}",
        name,
        parentId,
        costCenter: null,
        description: null,
        version,
        createdAt: Earlier,
        updatedAt: Earlier);

    private static Position Pos(long id, string code, string name, long version = 1) => new(
        id, code, name, level: null, description: null, version, createdAt: Earlier, updatedAt: Earlier);

    private sealed class FakeDepartmentRepository(params Department[] seed) : IDepartmentRepository
    {
        private readonly List<Department> _departments = [.. seed];

        public Dictionary<long, int> Headcounts { get; } = [];
        public DepartmentDependencies Dependencies { get; init; } = DepartmentDependencies.None;
        public bool WriteSucceeds { get; init; } = true;

        public int CodeExistsCalls { get; private set; }
        public int DependencyCalls { get; private set; }
        public int InsertCalls { get; private set; }
        public int ReplaceCalls { get; private set; }
        public int DeleteCalls { get; private set; }
        public long? LastExpectedVersion { get; private set; }
        public CoreHrActor? LastActor { get; private set; }

        public Task<IReadOnlyList<Department>> GetAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Department>>(_departments.ToList());

        public Task<Department?> GetByIdAsync(long departmentId, CancellationToken cancellationToken) =>
            Task.FromResult(_departments.SingleOrDefault(d => d.Id == departmentId));

        public Task<IReadOnlyDictionary<long, int>> GetHeadcountsAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<long, int>>(Headcounts);

        public Task<bool> CodeExistsAsync(string code, long? excludeId, CancellationToken cancellationToken)
        {
            CodeExistsCalls++;
            return Task.FromResult(_departments.Any(d =>
                string.Equals(d.Code, code, StringComparison.OrdinalIgnoreCase) && d.Id != excludeId));
        }

        public Task<bool> ExistsAsync(long departmentId, CancellationToken cancellationToken) =>
            Task.FromResult(_departments.Any(d => d.Id == departmentId));

        public Task<DepartmentDependencies> GetDependenciesAsync(long departmentId, CancellationToken cancellationToken)
        {
            DependencyCalls++;
            return Task.FromResult(Dependencies);
        }

        public Task<long> InsertAsync(Department department, CoreHrActor actor, CancellationToken cancellationToken)
        {
            InsertCalls++;
            LastActor = actor;
            return Task.FromResult(100L);
        }

        public Task<bool> ReplaceAsync(Department department, long expectedVersion, CoreHrActor actor, CancellationToken cancellationToken)
        {
            ReplaceCalls++;
            LastExpectedVersion = expectedVersion;
            LastActor = actor;
            return Task.FromResult(WriteSucceeds);
        }

        public Task<bool> DeleteAsync(long departmentId, long expectedVersion, CoreHrActor actor, CancellationToken cancellationToken)
        {
            DeleteCalls++;
            LastExpectedVersion = expectedVersion;
            LastActor = actor;
            return Task.FromResult(WriteSucceeds);
        }
    }

    private sealed class FakePositionRepository(params Position[] seed) : IPositionRepository
    {
        private readonly List<Position> _positions = [.. seed];

        public bool WriteSucceeds { get; init; } = true;
        public int InsertCalls { get; private set; }
        public int ReplaceCalls { get; private set; }

        public Task<IReadOnlyList<Position>> GetAllAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Position>>(_positions.ToList());

        public Task<Position?> GetByIdAsync(long positionId, CancellationToken cancellationToken) =>
            Task.FromResult(_positions.SingleOrDefault(p => p.Id == positionId));

        public Task<bool> CodeExistsAsync(string code, long? excludeId, CancellationToken cancellationToken) =>
            Task.FromResult(_positions.Any(p =>
                string.Equals(p.Code, code, StringComparison.OrdinalIgnoreCase) && p.Id != excludeId));

        public Task<long> InsertAsync(Position position, CoreHrActor actor, CancellationToken cancellationToken)
        {
            InsertCalls++;
            return Task.FromResult(500L);
        }

        public Task<bool> ReplaceAsync(Position position, long expectedVersion, CoreHrActor actor, CancellationToken cancellationToken)
        {
            ReplaceCalls++;
            return Task.FromResult(WriteSucceeds);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
