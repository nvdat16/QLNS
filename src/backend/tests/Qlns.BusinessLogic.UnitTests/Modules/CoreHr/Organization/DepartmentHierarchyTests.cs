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

    private static readonly IReadOnlyDictionary<long, int> Headcounts = new Dictionary<long, int>
    {
        [1] = 3,
        [2] = 1,
        [4] = 12,
        [5] = 8,
        [6] = 20
    };

    [Fact]
    public void BuildTree_FullDepth_BuildsNestedTreeFromRoots()
    {
        var tree = DepartmentHierarchy.BuildTree(Company, Headcounts, rootDepartmentId: null, depth: 10);

        Assert.Equal(2, tree.Count);
        var board = Assert.Single(tree, node => node.Department.Id == 1);
        Assert.Equal(2, board.Children.Count);

        var engineering = Assert.Single(board.Children, node => node.Department.Id == 2);
        Assert.Equal([5L, 4L], engineering.Children.Select(node => node.Department.Id));
        Assert.All(engineering.Children, node => Assert.Empty(node.Children));

        var finance = Assert.Single(board.Children, node => node.Department.Id == 3);
        Assert.Empty(finance.Children);
    }

    [Fact]
    public void BuildTree_DepthOne_ReturnsRootsWithoutChildren()
    {
        var tree = DepartmentHierarchy.BuildTree(Company, Headcounts, rootDepartmentId: null, depth: 1);

        Assert.Equal([1L, 6L], tree.Select(node => node.Department.Id));
        Assert.All(tree, node => Assert.Empty(node.Children));
    }

    [Fact]
    public void BuildTree_DepthTwo_StopsAtSecondLevel()
    {
        var tree = DepartmentHierarchy.BuildTree(Company, Headcounts, rootDepartmentId: null, depth: 2);

        var board = Assert.Single(tree, node => node.Department.Id == 1);
        Assert.Equal(2, board.Children.Count);
        Assert.All(board.Children, node => Assert.Empty(node.Children));
    }

    [Fact]
    public void BuildTree_RootDepartmentId_StartsFromThatDepartment()
    {
        var tree = DepartmentHierarchy.BuildTree(Company, Headcounts, rootDepartmentId: 2, depth: 10);

        var engineering = Assert.Single(tree);
        Assert.Equal(2, engineering.Department.Id);
        Assert.Equal([5L, 4L], engineering.Children.Select(node => node.Department.Id));
    }

    [Fact]
    public void BuildTree_UnknownRootDepartmentId_ReturnsEmpty()
    {
        var tree = DepartmentHierarchy.BuildTree(Company, Headcounts, rootDepartmentId: 999, depth: 10);

        Assert.Empty(tree);
    }

    [Fact]
    public void BuildTree_CyclicData_TerminatesAndVisitsEachDepartmentOnce()
    {
        // 10 <-> 11 form a loop; 12 hangs off 11. Neither 10 nor 11 is a root, so start from 10 explicitly.
        IReadOnlyList<Department> cyclic =
        [
            Dept(10, "A", 11),
            Dept(11, "B", 10),
            Dept(12, "C", 11)
        ];

        var tree = DepartmentHierarchy.BuildTree(cyclic, new Dictionary<long, int>(), rootDepartmentId: 10, depth: 10);

        var a = Assert.Single(tree);
        var b = Assert.Single(a.Children);
        Assert.Equal(11, b.Department.Id);
        var c = Assert.Single(b.Children);
        Assert.Equal(12, c.Department.Id);
        Assert.Empty(c.Children);
    }

    [Fact]
    public void BuildTree_OrdersChildrenByNameThenId()
    {
        IReadOnlyList<Department> departments =
        [
            Dept(1, "Root", null),
            Dept(5, "Zeta", 1),
            Dept(4, "Alpha", 1),
            Dept(3, "Alpha", 1),
            Dept(2, "Beta", 1)
        ];

        var tree = DepartmentHierarchy.BuildTree(departments, new Dictionary<long, int>(), null, 10);

        var root = Assert.Single(tree);
        Assert.Equal([3L, 4L, 2L, 5L], root.Children.Select(node => node.Department.Id));
    }

    [Fact]
    public void BuildTree_MapsHeadcountAndDefaultsToZero()
    {
        var tree = DepartmentHierarchy.BuildTree(Company, Headcounts, rootDepartmentId: null, depth: 10);

        var board = Assert.Single(tree, node => node.Department.Id == 1);
        Assert.Equal(3, board.Headcount);
        var finance = Assert.Single(board.Children, node => node.Department.Id == 3);
        Assert.Equal(0, finance.Headcount);
        var platform = Assert.Single(board.Children.Single(node => node.Department.Id == 2).Children, node => node.Department.Id == 4);
        Assert.Equal(12, platform.Headcount);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(11)]
    public void BuildTree_DepthOutOfRange_Throws(int depth)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            DepartmentHierarchy.BuildTree(Company, Headcounts, null, depth));
    }

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
