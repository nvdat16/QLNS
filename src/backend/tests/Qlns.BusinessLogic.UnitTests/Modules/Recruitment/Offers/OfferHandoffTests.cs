using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Offers;
using Xunit;

namespace Qlns.BusinessLogic.UnitTests.Modules.Recruitment.Offers;

public sealed class OfferHandoffTests
{
    private static readonly DateOnly StartDate = new(2026, 10, 1);

    [Fact]
    public void Plan_Valid_BuildsHireTransitionEmployeeContractAndChecklist()
    {
        var offer = OfferTests.CreateOffer(OfferStatus.Sent, version: 3);

        var plan = OfferHandoff.Plan(offer, ValidContext());

        Assert.Equal(new ApplicationHireTransition(10, 20, OfferApplicationStages.OfferLetter, OfferApplicationStages.HiredReady, 6, 7), plan.Application);

        Assert.Equal(10, plan.Employee.SourceApplicationId);
        Assert.Equal("Lan", plan.Employee.FirstName);
        Assert.Equal("Nguyen", plan.Employee.LastName);
        Assert.Equal("lan@example.com", plan.Employee.PersonalEmail);
        Assert.Equal("+84900000000", plan.Employee.Phone);
        Assert.Equal(4, plan.Employee.DepartmentId);
        Assert.Equal(9, plan.Employee.PositionId);
        Assert.Equal(StartDate, plan.Employee.HireDate);
        Assert.Equal(OfferHandoff.ProbationEmployeeStatus, plan.Employee.Status);

        Assert.Equal(OfferHandoff.ProbationContractType, plan.Contract.ContractType);
        Assert.Equal(StartDate, plan.Contract.StartDate);
        Assert.Equal(StartDate.AddDays(OfferHandoff.ProbationDays), plan.Contract.EndDate);
        Assert.Equal(offer.BaseSalary, plan.Contract.Salary);
        Assert.Equal("VND", plan.Contract.Currency);
        Assert.Equal(OfferHandoff.ProbationNoticePeriodDays, plan.Contract.NoticePeriodDays);
        Assert.True(plan.Contract.IsPrimary);
        Assert.Equal(OfferHandoff.DraftContractStatus, plan.Contract.Status);

        Assert.Equal(OnboardingChecklistTemplate.TemplateKeys, plan.Tasks.Select(task => task.TemplateKey));
    }

    [Fact]
    public void Plan_PositionMissingOnJobPosting_ThrowsPositionRequired()
    {
        var context = ValidContext() with { JobPosting = new HandoffJobPosting(4, null) };

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => OfferHandoff.Plan(OfferTests.CreateOffer(OfferStatus.Sent, 1), context));

        Assert.Equal(OfferHandoff.PositionRequiredCode, exception.Code);
    }

    [Theory]
    [InlineData("executive_round")]
    [InlineData("hired_ready")]
    [InlineData("rejected")]
    [InlineData("withdrawn")]
    public void Plan_ApplicationNotInOfferStage_ThrowsBusinessRule(string stage)
    {
        var context = ValidContext();
        context = context with { Application = context.Application with { Stage = stage } };

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => OfferHandoff.Plan(OfferTests.CreateOffer(OfferStatus.Sent, 1), context));

        Assert.Equal(OfferHandoff.ApplicationNotInOfferStageCode, exception.Code);
        Assert.Equal(stage, exception.Details["currentStage"]);
    }

    [Fact]
    public void Plan_ContextOfAnotherApplication_ThrowsArgument()
    {
        var context = ValidContext();
        context = context with { Application = context.Application with { Id = 99 } };

        Assert.Throws<ArgumentException>(() => OfferHandoff.Plan(OfferTests.CreateOffer(OfferStatus.Sent, 1), context));
    }

    [Theory]
    [InlineData(1, "EMP-00001")]
    [InlineData(128, "EMP-00128")]
    [InlineData(99999, "EMP-99999")]
    [InlineData(123456, "EMP-123456")]
    public void EmployeeCode_FormatsSequenceWithFiveDigitPadding(long sequenceValue, string expected)
    {
        Assert.Equal(expected, OfferHandoff.EmployeeCode(sequenceValue));
    }

    [Fact]
    public void EmployeeCode_NonPositive_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => OfferHandoff.EmployeeCode(0));
    }

    [Fact]
    public void ProbationContractNumber_PrefixesEmployeeCode()
    {
        Assert.Equal("HD-TV-EMP-00128", OfferHandoff.ProbationContractNumber("EMP-00128"));
    }

    [Fact]
    public void ChecklistTemplate_DueDatesAreRelativeToStartDateAtNineUtc()
    {
        var tasks = OnboardingChecklistTemplate.ForStart(StartDate).ToDictionary(task => task.TemplateKey);

        Assert.Equal(5, tasks.Count);
        Assert.Equal(new DateTimeOffset(2026, 9, 30, 9, 0, 0, TimeSpan.Zero), tasks["it.email"].DueAt);
        Assert.Equal(new DateTimeOffset(2026, 9, 30, 9, 0, 0, TimeSpan.Zero), tasks["it.laptop"].DueAt);
        Assert.Equal(new DateTimeOffset(2026, 10, 1, 9, 0, 0, TimeSpan.Zero), tasks["admin.badge"].DueAt);
        Assert.Equal(new DateTimeOffset(2026, 9, 29, 9, 0, 0, TimeSpan.Zero), tasks["hr.contract"].DueAt);
        Assert.Equal(new DateTimeOffset(2026, 10, 4, 9, 0, 0, TimeSpan.Zero), tasks["manager.buddy"].DueAt);
        Assert.All(tasks.Values, task => Assert.False(string.IsNullOrWhiteSpace(task.TaskName)));
    }

    [Fact]
    public void ResponseTokenLifetime_EndsOneGraceDayAfterExpirationDate()
    {
        var expiresAt = OfferResponseTokenLifetime.ExpiresAt(new DateOnly(2026, 9, 20));

        Assert.Equal(new DateTimeOffset(2026, 9, 22, 0, 0, 0, TimeSpan.Zero), expiresAt);
    }

    internal static OfferHandoffContext ValidContext() => new(
        new HandoffApplication(10, 20, 30, OfferApplicationStages.OfferLetter, 6),
        new HandoffCandidate("Lan", "Nguyen", "lan@example.com", "+84900000000"),
        new HandoffJobPosting(4, 9));
}
