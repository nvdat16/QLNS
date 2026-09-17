using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.Employees;

/// <summary>Use cases of EMP-01: directory search, profile view with field policy, self-service profile patch.</summary>
public sealed class EmployeeDirectoryService(
    IEmployeeRepository repository,
    TimeProvider timeProvider)
{
    public const string ProfileForbiddenCode = "corehr.employee.profile_forbidden";

    public async Task<PagedResult<Employee>> SearchAsync(
        EmployeeSearchQuery query,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(actor);

        var sort = EmployeeSort.Normalize(query.Sort);
        if (!EmployeeSort.IsAllowed(sort))
        {
            throw CoreHrValidationException.For(
                "sort",
                $"sort must be one of {string.Join(", ", EmployeeSort.Allowed)}.");
        }

        var search = query.Search?.Trim();
        var normalized = query with
        {
            Search = string.IsNullOrEmpty(search) ? null : search,
            Sort = sort
        };

        return await repository.SearchAsync(normalized, actor, cancellationToken);
    }

    public async Task<EmployeeView> GetAsync(
        long employeeId,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var employee = await repository.GetByIdAsync(employeeId, actor, cancellationToken)
            ?? throw new CoreHrNotFoundException("Employee", employeeId);

        return new EmployeeView(employee, EmployeeFieldPolicy.Resolve(actor, employee));
    }

    public async Task<EmployeeView> UpdatePersonalProfileAsync(
        UpdatePersonalProfileCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var actor = command.Actor;
        var employee = await repository.GetByIdAsync(command.EmployeeId, actor, cancellationToken)
            ?? throw new CoreHrNotFoundException("Employee", command.EmployeeId);

        if (!actor.IsSelf(employee.Id) && !actor.HasPermission(CoreHrPermissions.EmployeeProfileManage))
        {
            throw new CoreHrForbiddenException(
                ProfileForbiddenCode,
                "Only the employee or an actor with corehr.employee.profile.manage may update this profile.");
        }

        if (employee.Version != command.ExpectedVersion)
        {
            throw new CoreHrConcurrencyConflictException("employee");
        }

        var changedFields = employee.ApplyPersonalProfilePatch(command.Patch, timeProvider.GetUtcNow());
        if (changedFields.Count > 0)
        {
            var saved = await repository.SavePersonalProfileAsync(
                employee,
                command.ExpectedVersion,
                changedFields,
                actor,
                cancellationToken);

            if (!saved)
            {
                throw new CoreHrConcurrencyConflictException("employee");
            }
        }

        return new EmployeeView(employee, EmployeeFieldVisibility.Full);
    }
}
