using Qlns.BusinessLogic.Modules.CoreHr.Organization;
using Xunit;

namespace Qlns.BusinessLogic.UnitTests.Modules.CoreHr.Organization;

public sealed class DepartmentHierarchyTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 7, 0, 0, TimeSpan.Zero);

    // 1 Board
    //   2 Engineering
    //     4 Platform
    //     5 Apps
    //   3 Finance
    // 6 Sales (second root)
    private static readonly IReadOnlyList<Department> Company =
    [
        Dept(1, "Board", null),
        Dept(2, "Engineering", 1),
        Dept(3, "Finance", 1),
        Dept(4, "Platform", 2),
        Dept(5, "Apps", 2),
        Dept(6, "Sales", null)
    ];

    [Fact]
    public void WouldCreateCycle_SelfParent_ReturnsTrue()
    {
        Assert.True(DepartmentHierarchy.WouldCreateCycle(Company, departmentId: 2, newParentId: 2));
    }

    [Fact]
    public void WouldCreateCycle_DescendantAsParent_ReturnsTrue()
    {
        // Moving Board (1) under Platform (4), which is a grandchild of Board.
        Assert.True(DepartmentHierarchy.WouldCreateCycle(Company, departmentId: 1, newParentId: 4));
        // Moving Engineering (2) under its direct child Apps (5).
        Assert.True(DepartmentHierarchy.WouldCreateCycle(Company, departmentId: 2, newParentId: 5));
    }

    [Fact]
    public void WouldCreateCycle_UnrelatedParent_ReturnsFalse()
    {
        Assert.False(DepartmentHierarchy.WouldCreateCycle(Company, departmentId: 2, newParentId: 6));
        Assert.False(DepartmentHierarchy.WouldCreateCycle(Company, departmentId: 4, newParentId: 3));
    }

    [Fact]
    public void WouldCreateCycle_NullParent_ReturnsFalse()
    {
        Assert.False(DepartmentHierarchy.WouldCreateCycle(Company, departmentId: 2, newParentId: null));
    }

    [Fact]
    public void WouldCreateCycle_PreExistingLoopElsewhere_TerminatesWithFalse()
    {
        IReadOnlyList<Department> cyclic =
        [
            Dept(10, "A", 11),
            Dept(11, "B", 10),
            Dept(20, "Fresh", null)
        ];

        Assert.False(DepartmentHierarchy.WouldCreateCycle(cyclic, departmentId: 20, newParentId: 10));
    }

    private static Department Dept(long id, string name, long? parentId) => new(
        id,
        $"D{id}",
        name,
        parentId,
        costCenter: null,
        description: null,
        version: 1,
        createdAt: Now,
        updatedAt: Now);
}
