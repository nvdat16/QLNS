using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Qlns.BusinessLogic.Modules.CoreHr.Employees;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.DataAccess.Modules.CoreHr.Shared;
using Qlns.DataAccess.Modules.Recruitment.Applications;

namespace Qlns.DataAccess.Modules.CoreHr.Employees;

public sealed class EmployeeRepository(QlnsDbContext dbContext) : IEmployeeRepository
{
    public const string ProfileUpdateAction = "corehr.employee.profile.update";
    private const string EntityType = "employee";
    private const string LikeEscape = "\\";

    public async Task<PagedResult<Employee>> SearchAsync(
        EmployeeSearchQuery query,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        var employees = ApplyScope(dbContext.Set<EmployeeEntity>().AsNoTracking(), actor);

        if (query.DepartmentId is { } departmentId)
        {
            employees = employees.Where(x => x.DepartmentId == departmentId);
        }

        if (query.PositionId is { } positionId)
        {
            employees = employees.Where(x => x.PositionId == positionId);
        }

        if (query.Status is { } status)
        {
            var statusValue = status.ToContract();
            employees = employees.Where(x => x.Status == statusValue);
        }

        if (!string.IsNullOrEmpty(query.Search))
        {
            var pattern = $"%{EscapeLike(query.Search)}%";
            employees = employees.Where(x =>
                EF.Functions.ILike(x.FirstName, pattern, LikeEscape) ||
                EF.Functions.ILike(x.LastName, pattern, LikeEscape) ||
                EF.Functions.ILike(x.EmployeeCode, pattern, LikeEscape) ||
                (x.WorkEmail != null && EF.Functions.ILike(x.WorkEmail, pattern, LikeEscape)));
        }

        var totalItems = await employees.LongCountAsync(cancellationToken);
        if (totalItems == 0)
        {
            return PagedResult<Employee>.Empty(query.Page);
        }

        var entities = await ApplySort(employees, query.Sort)
            .Skip(query.Page.Skip)
            .Take(query.Page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Employee>(
            entities.Select(ToDomain).ToList(),
            query.Page.Page,
            query.Page.PageSize,
            totalItems);
    }

    public async Task<Employee?> GetByIdAsync(
        long employeeId,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        var entity = await ApplyScope(dbContext.Set<EmployeeEntity>().AsNoTracking(), actor)
            .SingleOrDefaultAsync(x => x.Id == employeeId, cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<bool> SavePersonalProfileAsync(
        Employee employee,
        long expectedVersion,
        IReadOnlyList<string> changedFields,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        var emergencyContact = employee.EmergencyContact is null
            ? null
            : JsonSerializer.Serialize(employee.EmergencyContact, CoreHrAudit.JsonOptions);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var rows = await dbContext.Set<EmployeeEntity>()
            .Where(x => x.Id == employee.Id && x.Version == expectedVersion)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.PersonalEmail, employee.PersonalEmail)
                    .SetProperty(x => x.Phone, employee.Phone)
                    .SetProperty(x => x.TemporaryAddress, employee.TemporaryAddress)
                    .SetProperty(x => x.EmergencyContact, emergencyContact)
                    .SetProperty(x => x.UpdatedAt, employee.UpdatedAt)
                    .SetProperty(x => x.Version, employee.Version),
                cancellationToken);

        if (rows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        // Restricted data (addresses, emergency contact values) never enters the audit payload;
        // only the field names that changed and the version transition are recorded.
        dbContext.Set<AuditLogEntity>().Add(CoreHrAudit.Entry(
            actor,
            ProfileUpdateAction,
            EntityType,
            employee.Id,
            before: new { version = expectedVersion, fields = changedFields },
            after: new { version = employee.Version, fields = changedFields },
            occurredAt: employee.UpdatedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    /// <summary>Organization-wide scope, department scope, or the actor's own record. Never leaks other rows.</summary>
    private static IQueryable<EmployeeEntity> ApplyScope(IQueryable<EmployeeEntity> employees, CoreHrActor actor)
    {
        if (actor.DataScope.OrganizationWide)
        {
            return employees;
        }

        var departmentIds = actor.DataScope.DepartmentIds.ToArray();
        var selfId = actor.EmployeeId ?? 0;
        return employees.Where(x => departmentIds.Contains(x.DepartmentId) || x.Id == selfId);
    }

    private static IOrderedQueryable<EmployeeEntity> ApplySort(IQueryable<EmployeeEntity> employees, string sort) => sort switch
    {
        EmployeeSort.Name => employees.OrderBy(x => x.LastName).ThenBy(x => x.FirstName).ThenBy(x => x.Id),
        EmployeeSort.NameDescending => employees.OrderByDescending(x => x.LastName).ThenByDescending(x => x.FirstName).ThenByDescending(x => x.Id),
        EmployeeSort.EmployeeCode => employees.OrderBy(x => x.EmployeeCode).ThenBy(x => x.Id),
        EmployeeSort.HireDate => employees.OrderBy(x => x.HireDate).ThenBy(x => x.LastName).ThenBy(x => x.FirstName).ThenBy(x => x.Id),
        _ => throw new ArgumentOutOfRangeException(nameof(sort), sort, "Unsupported employee sort.")
    };

    private static string EscapeLike(string term) => term
        .Replace(LikeEscape, LikeEscape + LikeEscape, StringComparison.Ordinal)
        .Replace("%", LikeEscape + "%", StringComparison.Ordinal)
        .Replace("_", LikeEscape + "_", StringComparison.Ordinal);

    private static Employee ToDomain(EmployeeEntity entity)
    {
        if (!EmployeeStatusNames.TryParseContract(entity.Status, out var status))
        {
            throw new InvalidOperationException($"Employee {entity.Id} has an unknown status '{entity.Status}'.");
        }

        return new Employee(
            entity.Id,
            entity.EmployeeCode,
            entity.SourceApplicationId,
            entity.UserId,
            entity.FirstName,
            entity.LastName,
            entity.WorkEmail,
            entity.PersonalEmail,
            entity.Phone,
            entity.DateOfBirth,
            entity.Gender,
            entity.OfficeLocation,
            entity.PermanentAddress,
            entity.TemporaryAddress,
            ParseEmergencyContact(entity),
            entity.ManagerId,
            entity.DepartmentId,
            entity.PositionId,
            entity.HireDate,
            status,
            entity.Version,
            entity.CreatedAt,
            entity.UpdatedAt);
    }

    private static IReadOnlyDictionary<string, string>? ParseEmergencyContact(EmployeeEntity entity)
    {
        if (string.IsNullOrWhiteSpace(entity.EmergencyContact))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(entity.EmergencyContact, CoreHrAudit.JsonOptions);
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"Employee {entity.Id} has an emergency_contact payload that is not an object of strings.",
                exception);
        }
    }
}
