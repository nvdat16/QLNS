using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.Organization;

/// <summary>
/// Use cases of EMP-02 (departments and their hierarchy, positions). The Organizational Chart tree is out of
/// scope for this delivery (README §2.3); hierarchy rules (parent, cycle prevention, delete restrictions) stay.
/// Departments and positions are
/// organization-wide reference data, so reads are not filtered by the actor's data scope; the actor is
/// still required so every write is audited against the acting user.
/// </summary>
public sealed class OrganizationService(
    IDepartmentRepository departments,
    IPositionRepository positions,
    TimeProvider timeProvider)
{
    public const string DepartmentCodeTaken = "corehr.department.code_taken";
    public const string DepartmentHierarchyCycle = "corehr.department.hierarchy_cycle";
    public const string DepartmentInUse = "corehr.department.in_use";
    public const string PositionCodeTaken = "corehr.position.code_taken";

    public async Task<IReadOnlyList<DepartmentDetail>> ListDepartmentsAsync(
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        var all = await departments.GetAllAsync(cancellationToken);
        var headcounts = await departments.GetHeadcountsAsync(cancellationToken);
        return all
            .OrderBy(d => d.Name, StringComparer.Ordinal)
            .ThenBy(d => d.Id)
            .Select(d => new DepartmentDetail(d, headcounts.GetValueOrDefault(d.Id)))
            .ToList();
    }

    public async Task<IReadOnlyList<Position>> ListPositionsAsync(
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        var all = await positions.GetAllAsync(cancellationToken);
        return all.OrderBy(p => p.Name, StringComparer.Ordinal).ThenBy(p => p.Id).ToList();
    }

    public async Task<DepartmentDetail> CreateDepartmentAsync(
        DepartmentWrite write,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        var normalized = Department.Validate(write);
        await EnsureDepartmentCodeFreeAsync(normalized.Code, excludeId: null, cancellationToken);
        await EnsureParentExistsAsync(normalized.ParentDepartmentId, cancellationToken);

        var department = Department.Create(normalized, timeProvider.GetUtcNow());
        var id = await departments.InsertAsync(department, actor, cancellationToken);
        return new DepartmentDetail(department.WithId(id), Headcount: 0);
    }

    public async Task<DepartmentDetail> ReplaceDepartmentAsync(
        long departmentId,
        long expectedVersion,
        DepartmentWrite write,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        var department = await departments.GetByIdAsync(departmentId, cancellationToken)
            ?? throw new CoreHrNotFoundException("Department", departmentId);

        if (department.Version != expectedVersion)
        {
            throw new CoreHrConcurrencyConflictException("department");
        }

        var normalized = Department.Validate(write);
        await EnsureDepartmentCodeFreeAsync(normalized.Code, departmentId, cancellationToken);
        await EnsureParentExistsAsync(normalized.ParentDepartmentId, cancellationToken);

        if (normalized.ParentDepartmentId.HasValue)
        {
            var all = await departments.GetAllAsync(cancellationToken);
            if (DepartmentHierarchy.WouldCreateCycle(all, departmentId, normalized.ParentDepartmentId))
            {
                throw new CoreHrBusinessRuleException(
                    DepartmentHierarchyCycle,
                    "The selected parent is the department itself or one of its descendants; that would create a cycle in the organization hierarchy.");
            }
        }

        department.Replace(normalized, timeProvider.GetUtcNow());
        if (!await departments.ReplaceAsync(department, expectedVersion, actor, cancellationToken))
        {
            throw new CoreHrConcurrencyConflictException("department");
        }

        var headcounts = await departments.GetHeadcountsAsync(cancellationToken);
        return new DepartmentDetail(department, headcounts.GetValueOrDefault(departmentId));
    }

    public async Task DeleteDepartmentAsync(
        long departmentId,
        long expectedVersion,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        var department = await departments.GetByIdAsync(departmentId, cancellationToken)
            ?? throw new CoreHrNotFoundException("Department", departmentId);

        if (department.Version != expectedVersion)
        {
            throw new CoreHrConcurrencyConflictException("department");
        }

        var dependencies = await departments.GetDependenciesAsync(departmentId, cancellationToken);
        if (dependencies.Any)
        {
            throw new CoreHrBusinessRuleException(
                DepartmentInUse,
                "The department still has child departments, employees or open requisitions. " +
                "Move its employees to another department, close or cancel its requisitions and re-parent or delete its child departments first.")
            {
                Details = new Dictionary<string, object?>
                {
                    ["childDepartments"] = dependencies.ChildDepartments,
                    ["employees"] = dependencies.Employees,
                    ["openRequisitions"] = dependencies.OpenRequisitions
                }
            };
        }

        if (!await departments.DeleteAsync(departmentId, expectedVersion, actor, cancellationToken))
        {
            throw new CoreHrConcurrencyConflictException("department");
        }
    }

    public async Task<Position> CreatePositionAsync(
        PositionWrite write,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        var normalized = Position.Validate(write);
        await EnsurePositionCodeFreeAsync(normalized.Code, excludeId: null, cancellationToken);

        var position = Position.Create(normalized, timeProvider.GetUtcNow());
        var id = await positions.InsertAsync(position, actor, cancellationToken);
        return position.WithId(id);
    }

    public async Task<Position> ReplacePositionAsync(
        long positionId,
        long expectedVersion,
        PositionWrite write,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);
        var position = await positions.GetByIdAsync(positionId, cancellationToken)
            ?? throw new CoreHrNotFoundException("Position", positionId);

        if (position.Version != expectedVersion)
        {
            throw new CoreHrConcurrencyConflictException("position");
        }

        var normalized = Position.Validate(write);
        await EnsurePositionCodeFreeAsync(normalized.Code, positionId, cancellationToken);

        position.Replace(normalized, timeProvider.GetUtcNow());
        if (!await positions.ReplaceAsync(position, expectedVersion, actor, cancellationToken))
        {
            throw new CoreHrConcurrencyConflictException("position");
        }

        return position;
    }

    private async Task EnsureDepartmentCodeFreeAsync(string code, long? excludeId, CancellationToken cancellationToken)
    {
        if (await departments.CodeExistsAsync(code, excludeId, cancellationToken))
        {
            throw new CoreHrBusinessRuleException(
                DepartmentCodeTaken, $"Department code '{code}' is already used by another department.");
        }
    }

    private async Task EnsurePositionCodeFreeAsync(string code, long? excludeId, CancellationToken cancellationToken)
    {
        if (await positions.CodeExistsAsync(code, excludeId, cancellationToken))
        {
            throw new CoreHrBusinessRuleException(
                PositionCodeTaken, $"Position code '{code}' is already used by another position.");
        }
    }

    private async Task EnsureParentExistsAsync(long? parentDepartmentId, CancellationToken cancellationToken)
    {
        if (parentDepartmentId is { } parentId && !await departments.ExistsAsync(parentId, cancellationToken))
        {
            throw CoreHrValidationException.For("parentDepartmentId", $"Parent department {parentId} does not exist.");
        }
    }
}
