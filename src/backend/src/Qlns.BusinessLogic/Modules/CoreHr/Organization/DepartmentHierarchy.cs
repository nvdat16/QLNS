namespace Qlns.BusinessLogic.Modules.CoreHr.Organization;

/// <summary>Pure helpers over the department parent/child graph: tree building and cycle detection.</summary>
public static class DepartmentHierarchy
{
    public const int MinDepth = 1;
    public const int MaxDepth = 10;

    /// <summary>
    /// Builds the chart. Depth 1 returns the root level only. Roots are departments without a parent,
    /// or the single department identified by <paramref name="rootDepartmentId"/>. A visited set makes the
    /// walk safe against corrupt (cyclic) data; siblings are ordered by name then id for stable output.
    /// </summary>
    public static IReadOnlyList<OrganizationNode> BuildTree(
        IReadOnlyList<Department> all,
        IReadOnlyDictionary<long, int> headcounts,
        long? rootDepartmentId,
        int depth)
    {
        ArgumentNullException.ThrowIfNull(all);
        ArgumentNullException.ThrowIfNull(headcounts);
        if (depth is < MinDepth or > MaxDepth)
        {
            throw new ArgumentOutOfRangeException(nameof(depth), $"depth must be between {MinDepth} and {MaxDepth}.");
        }

        var childrenByParent = all
            .Where(d => d.ParentDepartmentId.HasValue)
            .GroupBy(d => d.ParentDepartmentId!.Value)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<Department>)Ordered(g).ToList());

        IEnumerable<Department> roots = rootDepartmentId.HasValue
            ? all.Where(d => d.Id == rootDepartmentId.Value).Take(1)
            : all.Where(d => d.IsRootUnit());

        var visited = new HashSet<long>();
        return Ordered(roots)
            .Select(root => Build(root, 1))
            .Where(node => node is not null)
            .Select(node => node!)
            .ToList();

        OrganizationNode? Build(Department department, int level)
        {
            if (!visited.Add(department.Id))
            {
                return null;
            }

            IReadOnlyList<OrganizationNode> children = [];
            if (level < depth && childrenByParent.TryGetValue(department.Id, out var directChildren))
            {
                children = directChildren
                    .Select(child => Build(child, level + 1))
                    .Where(node => node is not null)
                    .Select(node => node!)
                    .ToList();
            }

            headcounts.TryGetValue(department.Id, out var headcount);
            return new OrganizationNode(department, headcount, children);
        }
    }

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

    private static IOrderedEnumerable<Department> Ordered(IEnumerable<Department> departments) =>
        departments.OrderBy(d => d.Name, StringComparer.Ordinal).ThenBy(d => d.Id);
}
