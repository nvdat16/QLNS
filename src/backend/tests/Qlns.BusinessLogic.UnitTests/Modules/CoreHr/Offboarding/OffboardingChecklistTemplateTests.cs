using Qlns.BusinessLogic.Modules.CoreHr.Offboarding;
using Xunit;
using static Qlns.BusinessLogic.UnitTests.Modules.CoreHr.Offboarding.OffboardingTestData;

namespace Qlns.BusinessLogic.UnitTests.Modules.CoreHr.Offboarding;

public sealed class OffboardingChecklistTemplateTests
{
    private static readonly HashSet<string> NoKeys = new(StringComparer.Ordinal);

    [Fact]
    public void Items_HaveUniqueKeysAndCoverAllFiveCategories()
    {
        var keys = OffboardingChecklistTemplate.Items.Select(item => item.TemplateKey).ToList();

        Assert.Equal(keys.Count, keys.Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(
            Enum.GetValues<OffboardingTaskCategory>().Order(),
            OffboardingChecklistTemplate.Items.Select(item => item.Category).Distinct().Order());
        Assert.All(OffboardingChecklistTemplate.Items, item =>
            Assert.StartsWith(item.Category.ToContract() + ".", item.TemplateKey, StringComparison.Ordinal));
    }

    [Fact]
    public void Items_BlockingKeysMatchTheSpecification()
    {
        var blocking = OffboardingChecklistTemplate.Items
            .Where(item => item.BlocksLastWorkingDay)
            .Select(item => item.TemplateKey)
            .Order()
            .ToList();

        Assert.Equal(["admin.badge", "finance.settlement", "it.accounts", "it.devices", "manager.handover"], blocking);
    }

    [Fact]
    public void Generate_NoExistingTasks_BuildsOnePendingUnsavedTaskPerItem()
    {
        var tasks = OffboardingChecklistTemplate.Generate(CaseId, LastWorkingDate, ManagerUserId, NoKeys, Now);

        Assert.Equal(OffboardingChecklistTemplate.Items.Count, tasks.Count);
        Assert.Equal(
            OffboardingChecklistTemplate.Items.Select(item => item.TemplateKey),
            tasks.Select(task => task.TemplateKey));
        Assert.All(tasks, task =>
        {
            Assert.Equal(0, task.Id);
            Assert.Equal(CaseId, task.OffboardingCaseId);
            Assert.Equal(OffboardingTaskStatus.Pending, task.Status);
            Assert.Equal(1, task.Version);
            Assert.Equal(Now, task.CreatedAt);
            Assert.Equal(new DateTimeOffset(2026, 10, 15, 23, 59, 59, TimeSpan.Zero), task.DueAt);
            Assert.Null(task.CompletedAt);
            Assert.False(string.IsNullOrWhiteSpace(task.Description));
        });
    }

    [Fact]
    public void Generate_AssignsManagerTasksToManagerUserOnly()
    {
        var tasks = OffboardingChecklistTemplate.Generate(CaseId, LastWorkingDate, ManagerUserId, NoKeys, Now);

        var managerTasks = tasks.Where(task => task.Category == OffboardingTaskCategory.Manager).ToList();
        Assert.NotEmpty(managerTasks);
        Assert.All(managerTasks, task => Assert.Equal(ManagerUserId, task.AssignedToUserId));
        Assert.All(tasks.Where(task => task.Category != OffboardingTaskCategory.Manager), task => Assert.Null(task.AssignedToUserId));
    }

    [Fact]
    public void Generate_WithoutManagerUser_LeavesManagerTasksUnassigned()
    {
        var tasks = OffboardingChecklistTemplate.Generate(CaseId, LastWorkingDate, managerUserId: null, NoKeys, Now);

        Assert.All(tasks, task => Assert.Null(task.AssignedToUserId));
    }

    [Fact]
    public void Generate_SkipsTemplateKeysThatAlreadyExist()
    {
        var existing = new HashSet<string>(StringComparer.Ordinal) { "it.devices", "manager.handover", "unknown.key" };

        var tasks = OffboardingChecklistTemplate.Generate(CaseId, LastWorkingDate, ManagerUserId, existing, Now);

        Assert.Equal(OffboardingChecklistTemplate.Items.Count - 2, tasks.Count);
        Assert.DoesNotContain(tasks, task => task.TemplateKey is "it.devices" or "manager.handover");
    }

    [Fact]
    public void Generate_AllKeysExist_ReturnsEmpty()
    {
        var existing = OffboardingChecklistTemplate.Items.Select(item => item.TemplateKey).ToHashSet(StringComparer.Ordinal);

        Assert.Empty(OffboardingChecklistTemplate.Generate(CaseId, LastWorkingDate, ManagerUserId, existing, Now));
    }

    [Fact]
    public void Generate_BlockingFlagFollowsTemplate()
    {
        var tasks = OffboardingChecklistTemplate.Generate(CaseId, LastWorkingDate, null, NoKeys, Now);

        Assert.Equal(5, tasks.Count(task => task.BlocksLastWorkingDay));
        Assert.True(tasks.Single(task => task.TemplateKey == "finance.settlement").BlocksLastWorkingDay);
        Assert.False(tasks.Single(task => task.TemplateKey == "hr.exit_interview").BlocksLastWorkingDay);
    }
}

public sealed class NoticePeriodRuleTests
{
    private static readonly DateOnly LastDay = new(2026, 10, 15);

    [Fact]
    public void ShortfallDays_NoNoticeDate_IsNull()
    {
        Assert.Null(NoticePeriodRule.ShortfallDays(null, LastDay, 30));
    }

    [Fact]
    public void ShortfallDays_NoContractualNotice_IsNull()
    {
        Assert.Null(NoticePeriodRule.ShortfallDays(LastDay.AddDays(-5), LastDay, null));
    }

    [Theory]
    [InlineData(45, 30, 0)]
    [InlineData(30, 30, 0)]
    [InlineData(10, 30, 20)]
    [InlineData(0, 30, 30)]
    public void ShortfallDays_ComparesNoticeGivenWithRequired(int daysBefore, int required, int expected)
    {
        Assert.Equal(expected, NoticePeriodRule.ShortfallDays(LastDay.AddDays(-daysBefore), LastDay, required));
    }
}
