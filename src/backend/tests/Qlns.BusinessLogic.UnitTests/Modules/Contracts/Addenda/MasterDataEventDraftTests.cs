using Qlns.BusinessLogic.Modules.Contracts.Addenda;
using Qlns.BusinessLogic.Modules.CoreHr.EmployeeEvents;
using Xunit;
using static Qlns.BusinessLogic.UnitTests.Modules.Contracts.Addenda.AddendumTestData;

namespace Qlns.BusinessLogic.UnitTests.Modules.Contracts.Addenda;

public sealed class MasterDataEventDraftTests
{
    private const long EmployeeId = 10;

    [Fact]
    public void From_SalaryOnly_IsSalaryAdjustmentCarryingTheAddendumEffectiveDateAndReason()
    {
        var addendum = Addendum(afterTerms: Terms(("salary", 30_000_000m)), beforeTerms: Terms(("salary", 25_000_000m)));

        var draft = MasterDataEventDraft.From(addendum, EmployeeId);

        Assert.NotNull(draft);
        Assert.Equal(EmployeeEventType.SalaryAdjustment, draft.EventType);
        Assert.Equal(EmployeeId, draft.EmployeeId);
        Assert.Equal(addendum.EffectiveDate, draft.EffectiveDate);
        Assert.Equal(addendum.Reason, draft.Reason);
        Assert.Equal(30_000_000m, draft.AfterData["salary"]!.GetValue<decimal>());
        Assert.Equal(25_000_000m, draft.BeforeData["salary"]!.GetValue<decimal>());
    }

    [Fact]
    public void From_DepartmentAndSalary_IsTransfer()
    {
        var addendum = Addendum(afterTerms: Terms(("salary", 1), ("departmentId", 4)));

        Assert.Equal(EmployeeEventType.Transfer, MasterDataEventDraft.From(addendum, EmployeeId)!.EventType);
    }

    [Fact]
    public void From_PositionWins_OverDepartmentAndSalary()
    {
        var addendum = Addendum(afterTerms: Terms(("salary", 1), ("departmentId", 4), ("positionId", 9)));

        Assert.Equal(EmployeeEventType.Promotion, MasterDataEventDraft.From(addendum, EmployeeId)!.EventType);
    }

    [Fact]
    public void From_KeepsOnlyMasterDataKeysInBeforeAndAfter()
    {
        var addendum = Addendum(
            beforeTerms: Terms(("positionId", 5), ("workLocation", "Hanoi"), ("allowance", 100)),
            afterTerms: Terms(("positionId", 9), ("workLocation", "Da Nang"), ("allowance", 200), ("other", "x")));

        var draft = MasterDataEventDraft.From(addendum, EmployeeId)!;

        Assert.Equal(["positionId"], draft.AfterData.Select(pair => pair.Key));
        Assert.Equal(["positionId"], draft.BeforeData.Select(pair => pair.Key));
        Assert.Equal(9, draft.AfterData["positionId"]!.GetValue<int>());
        Assert.Equal(5, draft.BeforeData["positionId"]!.GetValue<int>());
    }

    [Fact]
    public void From_BeforeTermsWithoutMasterData_YieldsEmptyBeforeData()
    {
        var addendum = Addendum(beforeTerms: Terms(), afterTerms: Terms(("salary", 1)));

        Assert.Empty(MasterDataEventDraft.From(addendum, EmployeeId)!.BeforeData);
    }

    [Fact]
    public void From_NoMasterDataTerm_ReturnsNull()
    {
        var addendum = Addendum(afterTerms: Terms(("workLocation", "Da Nang"), ("allowance", 500_000), ("noticePeriodDays", 45)));

        Assert.Null(MasterDataEventDraft.From(addendum, EmployeeId));
    }

    [Fact]
    public void From_DoesNotShareNodesWithTheAddendum()
    {
        var addendum = Addendum(afterTerms: Terms(("salary", 1)));

        var draft = MasterDataEventDraft.From(addendum, EmployeeId)!;
        draft.AfterData["salary"] = 2;

        Assert.Equal(1, addendum.AfterTerms["salary"]!.GetValue<int>());
    }

    [Fact]
    public void From_NonPositiveEmployee_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MasterDataEventDraft.From(Addendum(), 0));
    }
}
