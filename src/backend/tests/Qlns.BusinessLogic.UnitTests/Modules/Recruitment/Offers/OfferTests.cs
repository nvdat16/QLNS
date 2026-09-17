using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Offers;
using Xunit;

namespace Qlns.BusinessLogic.UnitTests.Modules.Recruitment.Offers;

public sealed class OfferTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 7, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 9, 15);
    private static readonly DateOnly StartDate = new(2026, 10, 1);
    private static readonly DateOnly ExpirationDate = new(2026, 9, 25);

    [Fact]
    public void Draft_Valid_BuildsUnsavedDraftWithDefaultCurrencyAndTrimmedTemplate()
    {
        var offer = Offer.Draft(ValidWrite() with { Currency = null, TemplateVersion = "  offer-v3  " }, Today, Now);

        Assert.Equal(0, offer.Id);
        Assert.Equal(1, offer.Version);
        Assert.Equal(OfferStatus.Draft, offer.Status);
        Assert.Equal(Offer.DefaultCurrency, offer.Currency);
        Assert.Equal("offer-v3", offer.TemplateVersion);
        Assert.Equal(EmploymentTypeValues.FullTime, offer.EmploymentType);
        Assert.Equal(StartDate, offer.StartDate);
        Assert.Equal(ExpirationDate, offer.ExpirationDate);
        Assert.Equal(Now, offer.CreatedAt);
        Assert.False(offer.HasDocument);
        Assert.Null(offer.ApprovedBy);
    }

    [Fact]
    public void Draft_ExplicitCurrency_IsKept()
    {
        var offer = Offer.Draft(ValidWrite() with { Currency = "USD" }, Today, Now);

        Assert.Equal("USD", offer.Currency);
    }

    [Fact]
    public void Draft_NegativeAmounts_ReportsEachField()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Offer.Draft(ValidWrite() with { BaseSalary = -1m, BonusAmount = -1m, AllowanceAmount = -0.01m }, Today, Now));

        Assert.Equal(["allowanceAmount", "baseSalary", "bonusAmount"], exception.Errors.Keys.Order());
    }

    [Fact]
    public void Draft_ZeroAmounts_AreAccepted()
    {
        var offer = Offer.Draft(ValidWrite() with { BaseSalary = 0m, BonusAmount = 0m, AllowanceAmount = null }, Today, Now);

        Assert.Equal(0m, offer.BaseSalary);
        Assert.Null(offer.AllowanceAmount);
    }

    [Theory]
    [InlineData("vnd")]
    [InlineData("US")]
    [InlineData("USDX")]
    [InlineData("12A")]
    public void Draft_InvalidCurrency_ThrowsOnCurrency(string currency)
    {
        var exception = Assert.Throws<CoreHrValidationException>(() => Offer.Draft(ValidWrite() with { Currency = currency }, Today, Now));

        Assert.True(exception.Errors.ContainsKey("currency"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("fulltime")]
    [InlineData("FULL_TIME")]
    public void Draft_UnknownEmploymentType_ThrowsOnEmploymentType(string? employmentType)
    {
        var exception = Assert.Throws<CoreHrValidationException>(() => Offer.Draft(ValidWrite() with { EmploymentType = employmentType }, Today, Now));

        Assert.True(exception.Errors.ContainsKey("employmentType"));
    }

    [Fact]
    public void Draft_AllEmploymentTypes_AreAccepted()
    {
        foreach (var type in EmploymentTypeValues.All)
        {
            Assert.Equal(type, Offer.Draft(ValidWrite() with { EmploymentType = type }, Today, Now).EmploymentType);
        }
    }

    [Fact]
    public void Draft_ExpirationInPast_ThrowsOnExpirationDate()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Offer.Draft(ValidWrite() with { ExpirationDate = Today.AddDays(-1) }, Today, Now));

        Assert.True(exception.Errors.ContainsKey("expirationDate"));
    }

    [Fact]
    public void Draft_ExpirationToday_IsAccepted()
    {
        var offer = Offer.Draft(ValidWrite() with { ExpirationDate = Today }, Today, Now);

        Assert.Equal(Today, offer.ExpirationDate);
    }

    [Fact]
    public void Draft_ExpirationAfterStart_ThrowsOnExpirationDate()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Offer.Draft(ValidWrite() with { ExpirationDate = StartDate.AddDays(1) }, Today, Now));

        Assert.True(exception.Errors.ContainsKey("expirationDate"));
    }

    [Fact]
    public void Draft_ExpirationEqualToStart_IsAccepted()
    {
        var offer = Offer.Draft(ValidWrite() with { ExpirationDate = StartDate }, Today, Now);

        Assert.Equal(StartDate, offer.ExpirationDate);
    }

    [Fact]
    public void Draft_MissingDates_ThrowsOnBoth()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() =>
            Offer.Draft(ValidWrite() with { StartDate = default, ExpirationDate = default }, Today, Now));

        Assert.True(exception.Errors.ContainsKey("startDate"));
        Assert.True(exception.Errors.ContainsKey("expirationDate"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public void Draft_MissingTemplateVersion_ThrowsOnTemplateVersion(string? templateVersion)
    {
        var exception = Assert.Throws<CoreHrValidationException>(() => Offer.Draft(ValidWrite() with { TemplateVersion = templateVersion }, Today, Now));

        Assert.True(exception.Errors.ContainsKey("templateVersion"));
    }

    [Fact]
    public void Draft_NonPositiveApplication_ThrowsOnApplicationId()
    {
        var exception = Assert.Throws<CoreHrValidationException>(() => Offer.Draft(ValidWrite() with { ApplicationId = 0 }, Today, Now));

        Assert.True(exception.Errors.ContainsKey("applicationId"));
    }

    [Fact]
    public void Approve_Draft_SetsApproverAndBumpsVersion()
    {
        var offer = CreateOffer(OfferStatus.Draft, version: 1);

        offer.Approve(approverUserId: 1, Now);

        Assert.Equal(OfferStatus.Approved, offer.Status);
        Assert.Equal(1, offer.ApprovedBy);
        Assert.Equal(Now, offer.ApprovedAt);
        Assert.Equal(2, offer.Version);
        Assert.Equal(Now, offer.UpdatedAt);
    }

    [Theory]
    [InlineData(OfferStatus.Approved)]
    [InlineData(OfferStatus.Sent)]
    [InlineData(OfferStatus.Accepted)]
    [InlineData(OfferStatus.Declined)]
    [InlineData(OfferStatus.Expired)]
    [InlineData(OfferStatus.Cancelled)]
    public void Approve_NotDraft_ThrowsInvalidTransition(OfferStatus status)
    {
        var offer = CreateOffer(status, version: 1);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => offer.Approve(1, Now));

        Assert.Equal(Offer.InvalidTransitionCode, exception.Code);
        Assert.Equal(status.ToContract(), exception.Details["currentStatus"]);
        Assert.Equal("approve", exception.Details["action"]);
        Assert.Equal(1, offer.Version);
    }

    [Fact]
    public void Send_Approved_SetsSentAt()
    {
        var offer = CreateOffer(OfferStatus.Approved, version: 2);

        offer.Send(Now);

        Assert.Equal(OfferStatus.Sent, offer.Status);
        Assert.Equal(Now, offer.SentAt);
        Assert.Equal(3, offer.Version);
    }

    [Theory]
    [InlineData(OfferStatus.Draft)]
    [InlineData(OfferStatus.Sent)]
    [InlineData(OfferStatus.Cancelled)]
    public void Send_NotApproved_ThrowsInvalidTransition(OfferStatus status)
    {
        var offer = CreateOffer(status, version: 1);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => offer.Send(Now));

        Assert.Equal(Offer.InvalidTransitionCode, exception.Code);
    }

    [Fact]
    public void Extend_Sent_MovesExpirationAndBumpsVersion()
    {
        var offer = CreateOffer(OfferStatus.Sent, version: 3);

        offer.Extend(new DateOnly(2026, 9, 30), Today, Now);

        Assert.Equal(OfferStatus.Sent, offer.Status);
        Assert.Equal(new DateOnly(2026, 9, 30), offer.ExpirationDate);
        Assert.Equal(4, offer.Version);
    }

    [Fact]
    public void Extend_Expired_ReturnsToSent()
    {
        var offer = CreateOffer(OfferStatus.Expired, version: 4, expirationDate: Today.AddDays(-2));

        offer.Extend(Today.AddDays(5), Today, Now);

        Assert.Equal(OfferStatus.Sent, offer.Status);
        Assert.Equal(Today.AddDays(5), offer.ExpirationDate);
        Assert.False(offer.IsExpired(Today));
    }

    [Fact]
    public void Extend_WithoutDate_ThrowsValidationOnExpirationDate()
    {
        var offer = CreateOffer(OfferStatus.Sent, version: 1);

        var exception = Assert.Throws<CoreHrValidationException>(() => offer.Extend(null, Today, Now));

        Assert.True(exception.Errors.ContainsKey("expirationDate"));
        Assert.Equal(ExpirationDate, offer.ExpirationDate);
    }

    [Fact]
    public void Extend_DateInPast_ThrowsValidation()
    {
        var offer = CreateOffer(OfferStatus.Sent, version: 1);

        var exception = Assert.Throws<CoreHrValidationException>(() => offer.Extend(Today.AddDays(-1), Today, Now));

        Assert.True(exception.Errors.ContainsKey("expirationDate"));
    }

    [Fact]
    public void Extend_DateAfterStart_ThrowsValidation()
    {
        var offer = CreateOffer(OfferStatus.Sent, version: 1);

        var exception = Assert.Throws<CoreHrValidationException>(() => offer.Extend(StartDate.AddDays(1), Today, Now));

        Assert.True(exception.Errors.ContainsKey("expirationDate"));
    }

    [Theory]
    [InlineData(OfferStatus.Draft)]
    [InlineData(OfferStatus.Approved)]
    [InlineData(OfferStatus.Accepted)]
    [InlineData(OfferStatus.Declined)]
    [InlineData(OfferStatus.Cancelled)]
    public void Extend_NotSentOrExpired_ThrowsInvalidTransition(OfferStatus status)
    {
        var offer = CreateOffer(status, version: 1);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => offer.Extend(Today.AddDays(5), Today, Now));

        Assert.Equal(Offer.InvalidTransitionCode, exception.Code);
    }

    [Theory]
    [InlineData(OfferStatus.Draft)]
    [InlineData(OfferStatus.Approved)]
    [InlineData(OfferStatus.Sent)]
    public void Cancel_FromOpenInternalStatus_Cancels(OfferStatus status)
    {
        var offer = CreateOffer(status, version: 1);

        offer.Cancel("Position closed", Now);

        Assert.Equal(OfferStatus.Cancelled, offer.Status);
        Assert.Equal(2, offer.Version);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void Cancel_WithoutReason_ThrowsValidationOnReason(string? reason)
    {
        var offer = CreateOffer(OfferStatus.Draft, version: 1);

        var exception = Assert.Throws<CoreHrValidationException>(() => offer.Cancel(reason, Now));

        Assert.True(exception.Errors.ContainsKey("reason"));
        Assert.Equal(OfferStatus.Draft, offer.Status);
    }

    [Theory]
    [InlineData(OfferStatus.Accepted)]
    [InlineData(OfferStatus.Declined)]
    [InlineData(OfferStatus.Expired)]
    [InlineData(OfferStatus.Cancelled)]
    public void Cancel_FromClosedStatus_ThrowsInvalidTransition(OfferStatus status)
    {
        var offer = CreateOffer(status, version: 1);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => offer.Cancel("reason", Now));

        Assert.Equal(Offer.InvalidTransitionCode, exception.Code);
    }

    [Fact]
    public void Expire_Sent_Expires()
    {
        var offer = CreateOffer(OfferStatus.Sent, version: 2);

        offer.Expire(Now);

        Assert.Equal(OfferStatus.Expired, offer.Status);
        Assert.Equal(3, offer.Version);
    }

    [Theory]
    [InlineData(OfferStatus.Draft)]
    [InlineData(OfferStatus.Approved)]
    [InlineData(OfferStatus.Accepted)]
    public void Expire_NotSent_ThrowsInvalidTransition(OfferStatus status)
    {
        var offer = CreateOffer(status, version: 1);

        Assert.Throws<CoreHrBusinessRuleException>(() => offer.Expire(Now));
    }

    [Fact]
    public void Accept_Sent_SetsRespondedAt()
    {
        var offer = CreateOffer(OfferStatus.Sent, version: 3);

        offer.Accept(Now);

        Assert.Equal(OfferStatus.Accepted, offer.Status);
        Assert.Equal(Now, offer.RespondedAt);
        Assert.Equal(4, offer.Version);
    }

    [Fact]
    public void Decline_Sent_SetsRespondedAt()
    {
        var offer = CreateOffer(OfferStatus.Sent, version: 3);

        offer.Decline(Now);

        Assert.Equal(OfferStatus.Declined, offer.Status);
        Assert.Equal(Now, offer.RespondedAt);
        Assert.Equal(4, offer.Version);
    }

    [Theory]
    [InlineData(OfferStatus.Draft)]
    [InlineData(OfferStatus.Approved)]
    [InlineData(OfferStatus.Expired)]
    [InlineData(OfferStatus.Cancelled)]
    public void AcceptOrDecline_NotSent_ThrowsInvalidTransition(OfferStatus status)
    {
        Assert.Throws<CoreHrBusinessRuleException>(() => CreateOffer(status, 1).Accept(Now));
        Assert.Throws<CoreHrBusinessRuleException>(() => CreateOffer(status, 1).Decline(Now));
    }

    [Fact]
    public void IsExpired_SentPastExpiration_IsTrueAndNotOpen()
    {
        var offer = CreateOffer(OfferStatus.Sent, version: 1, expirationDate: Today.AddDays(-1));

        Assert.True(offer.IsExpired(Today));
        Assert.False(offer.IsOpenForResponse(Today));
    }

    [Fact]
    public void IsExpired_SentOnExpirationDay_IsStillOpen()
    {
        var offer = CreateOffer(OfferStatus.Sent, version: 1, expirationDate: Today);

        Assert.False(offer.IsExpired(Today));
        Assert.True(offer.IsOpenForResponse(Today));
    }

    [Theory]
    [InlineData(OfferStatus.Draft)]
    [InlineData(OfferStatus.Approved)]
    [InlineData(OfferStatus.Accepted)]
    [InlineData(OfferStatus.Declined)]
    [InlineData(OfferStatus.Expired)]
    [InlineData(OfferStatus.Cancelled)]
    public void IsExpired_NotSent_IsFalseAndNotOpen(OfferStatus status)
    {
        var offer = CreateOffer(status, version: 1, expirationDate: Today.AddDays(-10));

        Assert.False(offer.IsExpired(Today));
        Assert.False(offer.IsOpenForResponse(Today));
    }

    [Fact]
    public void Snapshot_CapturesStatusExpirationAndVersion()
    {
        var snapshot = CreateOffer(OfferStatus.Sent, version: 5).Snapshot();

        Assert.Equal(new OfferSnapshot(OfferStatus.Sent, ExpirationDate, 5), snapshot);
    }

    [Fact]
    public void Constructor_InvalidArguments_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Offer(
            0, 1, 1000m, null, null, "VND", "full_time", StartDate, ExpirationDate, OfferStatus.Sent, "v1", null, null, null, null, null, 1, Now, Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Offer(
            1, 0, 1000m, null, null, "VND", "full_time", StartDate, ExpirationDate, OfferStatus.Draft, "v1", null, null, null, null, null, 1, Now, Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Offer(
            1, 1, 1000m, null, null, "VND", "full_time", StartDate, ExpirationDate, OfferStatus.Draft, "v1", null, null, null, null, null, 0, Now, Now));
    }

    internal static OfferWrite ValidWrite() => new(
        ApplicationId: 10,
        BaseSalary: 25_000_000m,
        BonusAmount: 5_000_000m,
        AllowanceAmount: null,
        Currency: "VND",
        EmploymentType: EmploymentTypeValues.FullTime,
        StartDate: StartDate,
        ExpirationDate: ExpirationDate,
        TemplateVersion: "offer-v3");

    internal static Offer CreateOffer(
        OfferStatus status,
        long version,
        DateOnly? expirationDate = null,
        DateOnly? startDate = null,
        long id = 42) => new(
        id,
        applicationId: 10,
        baseSalary: 25_000_000m,
        bonusAmount: 5_000_000m,
        allowanceAmount: null,
        currency: "VND",
        employmentType: EmploymentTypeValues.FullTime,
        startDate ?? StartDate,
        expirationDate ?? ExpirationDate,
        status,
        templateVersion: "offer-v3",
        documentObjectKey: null,
        approvedBy: status == OfferStatus.Draft ? null : 1,
        approvedAt: status == OfferStatus.Draft ? null : Now.AddDays(-3),
        sentAt: status is OfferStatus.Draft or OfferStatus.Approved ? null : Now.AddDays(-2),
        respondedAt: status is OfferStatus.Accepted or OfferStatus.Declined ? Now.AddDays(-1) : null,
        version,
        createdAt: Now.AddDays(-5),
        updatedAt: Now.AddDays(-1));
}
