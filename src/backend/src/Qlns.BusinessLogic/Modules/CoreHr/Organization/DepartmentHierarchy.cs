namespace Qlns.BusinessLogic.Modules.CoreHr.Organization;

/// <summary>
/// Pure helpers over the department parent/child graph. Only cycle detection remains: the chart tree
/// (Organizational Chart) was removed from the delivery scope, the hierarchy itself is still in scope.
/// </summary>
public static class DepartmentHierarchy
{
    /// <summary>
    /// True when re-parenting <paramref name="departmentId"/> under <paramref name="newParentId"/> would close a
    /// loop: the new parent is the department itself or one of its descendants (an ancestor walk from the new
    /// parent reaches the department). A null parent never creates a cycle.
    /// </summary>
    public static bool WouldCreateCycle(IReadOnlyList<Department> all, long departmentId, long? newParentId)
    {
        ArgumentNullException.ThrowIfNull(all);
        if (newParentId is null)
        {
            return false;
        }

        if (newParentId.Value == departmentId)
        {
            return true;
        }

        var parentById = new Dictionary<long, long?>();
        foreach (var department in all)
        {
            parentById[department.Id] = department.ParentDepartmentId;
        }

        var visited = new HashSet<long>();
        long? current = newParentId;
        while (current.HasValue)
        {
            if (current.Value == departmentId)
            {
                return true;
            }

            if (!visited.Add(current.Value) || !parentById.TryGetValue(current.Value, out var parent))
            {
                // Pre-existing loop elsewhere or unknown ancestor: this change does not introduce a new cycle.
                return false;
            }

            current = parent;
        }

        return false;
    }
}
