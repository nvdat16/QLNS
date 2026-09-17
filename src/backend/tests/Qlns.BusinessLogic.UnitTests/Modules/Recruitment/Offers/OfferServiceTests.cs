using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Offers;
using Xunit;

namespace Qlns.BusinessLogic.UnitTests.Modules.Recruitment.Offers;

public sealed class OfferServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 7, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 9, 15);
    private static readonly OfferApplication OfferStageApplication = new(10, 20, 30, 4, OfferApplicationStages.OfferLetter);

    [Fact]
    public async Task CreateAsync_Valid_InsertsDraftAndReturnsPersisted()
    {
        var repository = new FakeOfferRepository { Application = OfferStageApplication };
        var service = CreateService(repository);

        var created = await service.CreateAsync(new CreateOfferCommand(OfferTests.ValidWrite(), Actor(OfferPermissions.Write)), CancellationToken.None);

        Assert.Equal(100, created.Id);
        Assert.Equal(OfferStatus.Draft, created.Status);
        Assert.Equal(1, repository.InsertCalls);
        Assert.Equal(0, repository.Inserted?.Id);
    }

    [Fact]
    public async Task CreateAsync_ApplicationNotVisible_ThrowsNotFound()
    {
        var repository = new FakeOfferRepository { Application = null };

        var exception = await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            CreateService(repository).CreateAsync(new CreateOfferCommand(OfferTests.ValidWrite(), Actor(OfferPermissions.Write)), CancellationToken.None));

        Assert.Equal("Application", exception.Resource);
        Assert.Equal(0, repository.InsertCalls);
    }

    [Fact]
    public async Task CreateAsync_WithoutWritePermission_ThrowsForbidden()
    {
        var repository = new FakeOfferRepository { Application = OfferStageApplication };

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() =>
            CreateService(repository).CreateAsync(new CreateOfferCommand(OfferTests.ValidWrite(), Actor(OfferPermissions.Read)), CancellationToken.None));

        Assert.Equal(OfferService.WriteForbiddenCode, exception.Code);
        Assert.Equal(0, repository.InsertCalls);
    }

    [Theory]
    [InlineData("executive_round")]
    [InlineData("hired_ready")]
    public async Task CreateAsync_ApplicationNotInOfferStage_ThrowsBusinessRule(string stage)
    {
        var repository = new FakeOfferRepository { Application = OfferStageApplication with { Stage = stage } };

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            CreateService(repository).CreateAsync(new CreateOfferCommand(OfferTests.ValidWrite(), Actor(OfferPermissions.Write)), CancellationToken.None));

        Assert.Equal(OfferService.StageNotOfferCode, exception.Code);
        Assert.Equal(0, repository.InsertCalls);
    }

    [Fact]
    public async Task CreateAsync_OpenOfferExists_ThrowsAlreadyOpenWithoutInserting()
    {
        var repository = new FakeOfferRepository { Application = OfferStageApplication, HasOpenOffer = true };

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            CreateService(repository).CreateAsync(new CreateOfferCommand(OfferTests.ValidWrite(), Actor(OfferPermissions.Write)), CancellationToken.None));

        Assert.Equal(OfferService.AlreadyOpenCode, exception.Code);
        Assert.Equal(0, repository.InsertCalls);
    }

    [Fact]
    public async Task CreateAsync_InsertRejectedByUniqueIndex_ThrowsAlreadyOpen()
    {
        var repository = new FakeOfferRepository { Application = OfferStageApplication, InsertSucceeds = false };

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            CreateService(repository).CreateAsync(new CreateOfferCommand(OfferTests.ValidWrite(), Actor(OfferPermissions.Write)), CancellationToken.None));

        Assert.Equal(OfferService.AlreadyOpenCode, exception.Code);
        Assert.Equal(1, repository.InsertCalls);
    }

    [Fact]
    public async Task CreateAsync_InvalidPayload_ThrowsValidationWithoutInserting()
    {
        var repository = new FakeOfferRepository { Application = OfferStageApplication };

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() => CreateService(repository).CreateAsync(
            new CreateOfferCommand(OfferTests.ValidWrite() with { ExpirationDate = Today.AddDays(-1) }, Actor(OfferPermissions.Write)),
            CancellationToken.None));

        Assert.True(exception.Errors.ContainsKey("expirationDate"));
        Assert.Equal(0, repository.InsertCalls);
    }

    [Fact]
    public async Task GetAsync_SentPastExpiration_LazilyExpiresAndAudits()
    {
        var repository = new FakeOfferRepository { Offer = OfferTests.CreateOffer(OfferStatus.Sent, version: 3, expirationDate: Today.AddDays(-1)) };

        var offer = await CreateService(repository).GetAsync(42, Actor(OfferPermissions.Read), CancellationToken.None);

        Assert.Equal(OfferStatus.Expired, offer.Status);
        Assert.Equal(4, offer.Version);
        Assert.Equal([OfferTransition.Expire], repository.Transitions);
        Assert.Equal(3, repository.LastBefore?.Version);
    }

    [Fact]
    public async Task GetAsync_SentNotExpired_DoesNotWrite()
    {
        var repository = new FakeOfferRepository { Offer = OfferTests.CreateOffer(OfferStatus.Sent, version: 3, expirationDate: Today) };

        var offer = await CreateService(repository).GetAsync(42, Actor(OfferPermissions.Read), CancellationToken.None);

        Assert.Equal(OfferStatus.Sent, offer.Status);
        Assert.Empty(repository.Transitions);
    }

    [Fact]
    public async Task GetAsync_LazyExpiryLosesRace_ReloadsOffer()
    {
        var repository = new FakeOfferRepository
        {
            Offer = OfferTests.CreateOffer(OfferStatus.Sent, version: 3, expirationDate: Today.AddDays(-1)),
            SaveSucceeds = false,
            OfferAfterReload = OfferTests.CreateOffer(OfferStatus.Accepted, version: 4, expirationDate: Today.AddDays(-1))
        };

        var offer = await CreateService(repository).GetAsync(42, Actor(OfferPermissions.Read), CancellationToken.None);

        Assert.Equal(OfferStatus.Accepted, offer.Status);
        Assert.Equal(4, offer.Version);
    }

    [Fact]
    public async Task GetAsync_NotVisible_ThrowsNotFound()
    {
        var exception = await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            CreateService(new FakeOfferRepository()).GetAsync(42, Actor(OfferPermissions.Read), CancellationToken.None));

        Assert.Equal("Offer", exception.Resource);
        Assert.Equal(42, exception.Id);
    }

    [Fact]
    public async Task TransitionAsync_Approve_WithApprovePermission_SetsApproverAndSaves()
    {
        var repository = new FakeOfferRepository { Offer = OfferTests.CreateOffer(OfferStatus.Draft, version: 1) };

        var updated = await CreateService(repository).TransitionAsync(
            new TransitionOfferCommand(42, OfferAction.Approve, 1, null, null, Actor(OfferPermissions.Approve, userId: 5)),
            CancellationToken.None);

        Assert.Equal(OfferStatus.Approved, updated.Status);
        Assert.Equal(5, updated.ApprovedBy);
        Assert.Equal([OfferTransition.Approve], repository.Transitions);
        Assert.Null(repository.LastResponseToken);
        Assert.Equal(1, repository.LastBefore?.Version);
    }

    [Fact]
    public async Task TransitionAsync_Approve_WithoutApprovePermission_ThrowsForbidden()
    {
        var repository = new FakeOfferRepository { Offer = OfferTests.CreateOffer(OfferStatus.Draft, version: 1) };

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() => CreateService(repository).TransitionAsync(
            new TransitionOfferCommand(42, OfferAction.Approve, 1, null, null, Actor(OfferPermissions.Write)),
            CancellationToken.None));

        Assert.Equal(OfferService.ApproveForbiddenCode, exception.Code);
        Assert.Empty(repository.Transitions);
    }

    [Fact]
    public async Task TransitionAsync_Send_IssuesTokenBoundToExpirationAndPassesItToRepository()
    {
        var offer = OfferTests.CreateOffer(OfferStatus.Approved, version: 2);
        var repository = new FakeOfferRepository { Offer = offer };
        var tokens = new FakeTokenService();

        var updated = await CreateService(repository, tokens).TransitionAsync(
            new TransitionOfferCommand(42, OfferAction.Send, 2, null, null, Actor(OfferPermissions.Write)),
            CancellationToken.None);

        Assert.Equal(OfferStatus.Sent, updated.Status);
        Assert.Equal(Now, updated.SentAt);
        Assert.Equal([OfferTransition.Send], repository.Transitions);
        Assert.Equal(tokens.LastIssued, repository.LastResponseToken);
        Assert.Equal(42, tokens.LastOfferId);
        Assert.Equal(OfferResponseTokenLifetime.ExpiresAt(offer.ExpirationDate), tokens.LastExpiresAt);
    }

    [Fact]
    public async Task TransitionAsync_Send_WithoutWritePermission_ThrowsForbidden()
    {
        var repository = new FakeOfferRepository { Offer = OfferTests.CreateOffer(OfferStatus.Approved, version: 2) };

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() => CreateService(repository).TransitionAsync(
            new TransitionOfferCommand(42, OfferAction.Send, 2, null, null, Actor(OfferPermissions.Approve)),
            CancellationToken.None));

        Assert.Equal(OfferService.WriteForbiddenCode, exception.Code);
    }

    [Fact]
    public async Task TransitionAsync_Send_FromDraft_ThrowsInvalidTransitionWithoutSaving()
    {
        var repository = new FakeOfferRepository { Offer = OfferTests.CreateOffer(OfferStatus.Draft, version: 1) };

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() => CreateService(repository).TransitionAsync(
            new TransitionOfferCommand(42, OfferAction.Send, 1, null, null, Actor(OfferPermissions.Write)),
            CancellationToken.None));

        Assert.Equal(Offer.InvalidTransitionCode, exception.Code);
        Assert.Empty(repository.Transitions);
    }

    [Fact]
    public async Task TransitionAsync_Extend_ExpiredOffer_RequiresApproveAndReissuesToken()
    {
        var repository = new FakeOfferRepository { Offer = OfferTests.CreateOffer(OfferStatus.Expired, version: 4, expirationDate: Today.AddDays(-3)) };
        var tokens = new FakeTokenService();
        var newExpiration = Today.AddDays(7);

        var updated = await CreateService(repository, tokens).TransitionAsync(
            new TransitionOfferCommand(42, OfferAction.Extend, 4, null, newExpiration, Actor(OfferPermissions.Approve)),
            CancellationToken.None);

        Assert.Equal(OfferStatus.Sent, updated.Status);
        Assert.Equal(newExpiration, updated.ExpirationDate);
        Assert.Equal([OfferTransition.Extend], repository.Transitions);
        Assert.Equal(tokens.LastIssued, repository.LastResponseToken);
        Assert.Equal(OfferResponseTokenLifetime.ExpiresAt(newExpiration), tokens.LastExpiresAt);
    }

    [Fact]
    public async Task TransitionAsync_Extend_WithoutApprovePermission_ThrowsForbidden()
    {
        var repository = new FakeOfferRepository { Offer = OfferTests.CreateOffer(OfferStatus.Sent, version: 3) };

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() => CreateService(repository).TransitionAsync(
            new TransitionOfferCommand(42, OfferAction.Extend, 3, null, Today.AddDays(7), Actor(OfferPermissions.Write)),
            CancellationToken.None));

        Assert.Equal(OfferService.ApproveForbiddenCode, exception.Code);
    }

    [Fact]
    public async Task TransitionAsync_Extend_WithoutDate_ThrowsValidation()
    {
        var repository = new FakeOfferRepository { Offer = OfferTests.CreateOffer(OfferStatus.Sent, version: 3) };

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() => CreateService(repository).TransitionAsync(
            new TransitionOfferCommand(42, OfferAction.Extend, 3, null, null, Actor(OfferPermissions.Approve)),
            CancellationToken.None));

        Assert.True(exception.Errors.ContainsKey("expirationDate"));
        Assert.Empty(repository.Transitions);
    }

    [Fact]
    public async Task TransitionAsync_Cancel_PassesTrimmedReasonToRepository()
    {
        var repository = new FakeOfferRepository { Offer = OfferTests.CreateOffer(OfferStatus.Approved, version: 2) };

        var updated = await CreateService(repository).TransitionAsync(
            new TransitionOfferCommand(42, OfferAction.Cancel, 2, "  Position closed  ", null, Actor(OfferPermissions.Write)),
            CancellationToken.None);

        Assert.Equal(OfferStatus.Cancelled, updated.Status);
        Assert.Equal([OfferTransition.Cancel], repository.Transitions);
        Assert.Equal("Position closed", repository.LastReason);
    }

    [Fact]
    public async Task TransitionAsync_Cancel_WithoutReason_ThrowsValidationWithoutSaving()
    {
        var repository = new FakeOfferRepository { Offer = OfferTests.CreateOffer(OfferStatus.Approved, version: 2) };

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() => CreateService(repository).TransitionAsync(
            new TransitionOfferCommand(42, OfferAction.Cancel, 2, null, null, Actor(OfferPermissions.Write)),
            CancellationToken.None));

        Assert.True(exception.Errors.ContainsKey("reason"));
        Assert.Empty(repository.Transitions);
    }

    [Fact]
    public async Task TransitionAsync_StaleVersion_ThrowsConcurrencyWithoutSaving()
    {
        var repository = new FakeOfferRepository { Offer = OfferTests.CreateOffer(OfferStatus.Draft, version: 2) };

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => CreateService(repository).TransitionAsync(
            new TransitionOfferCommand(42, OfferAction.Approve, 1, null, null, Actor(OfferPermissions.Approve)),
            CancellationToken.None));

        Assert.Empty(repository.Transitions);
    }

    [Fact]
    public async Task TransitionAsync_LazyExpiryRunsBeforeVersionCheck()
    {
        var repository = new FakeOfferRepository { Offer = OfferTests.CreateOffer(OfferStatus.Sent, version: 3, expirationDate: Today.AddDays(-1)) };

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => CreateService(repository).TransitionAsync(
            new TransitionOfferCommand(42, OfferAction.Cancel, 3, "late", null, Actor(OfferPermissions.Write)),
            CancellationToken.None));

        Assert.Equal([OfferTransition.Expire], repository.Transitions);
    }

    [Fact]
    public async Task TransitionAsync_SaveLosesRace_ThrowsConcurrency()
    {
        var repository = new FakeOfferRepository { Offer = OfferTests.CreateOffer(OfferStatus.Draft, version: 1), SaveSucceeds = false };

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() => CreateService(repository).TransitionAsync(
            new TransitionOfferCommand(42, OfferAction.Approve, 1, null, null, Actor(OfferPermissions.Approve)),
            CancellationToken.None));

        Assert.Equal([OfferTransition.Approve], repository.Transitions);
    }

    [Fact]
    public async Task TransitionAsync_NotVisible_ThrowsNotFound()
    {
        var exception = await Assert.ThrowsAsync<CoreHrNotFoundException>(() => CreateService(new FakeOfferRepository()).TransitionAsync(
            new TransitionOfferCommand(42, OfferAction.Approve, 1, null, null, Actor(OfferPermissions.Approve)),
            CancellationToken.None));

        Assert.Equal("Offer", exception.Resource);
    }

    [Fact]
    public async Task RespondAsync_Accept_FirstTime_RunsHandoffInOneSaveAndReturnsIdentifiers()
    {
        var repository = new FakeOfferRepository
        {
            Offer = OfferTests.CreateOffer(OfferStatus.Sent, version: 3),
            HandoffContext = OfferHandoffTests.ValidContext()
        };

        var result = await CreateService(repository).RespondAsync(Respond(OfferDecision.Accept), CancellationToken.None);

        Assert.False(result.Replayed);
        Assert.Equal(OfferStatus.Accepted, result.Status);
        Assert.Equal(42, result.OfferId);
        Assert.Equal(900, result.EmployeeId);
        Assert.Equal(901, result.InitialContractId);
        Assert.Equal([1001L, 1002L, 1003L, 1004L, 1005L], result.OnboardingTaskIds);
        Assert.Equal(1, repository.AcceptanceCalls);
        Assert.Equal(3, repository.LastBefore?.Version);
        Assert.Equal(OfferStatus.Accepted, repository.Offer!.Status);
        Assert.Equal(Now, repository.Offer.RespondedAt);
        Assert.Equal(OfferApplicationStages.HiredReady, repository.LastPlan?.Application.ToStage);
        Assert.Equal(7, repository.LastPlan?.Application.NewVersion);
        Assert.Empty(repository.Transitions);
        Assert.Equal(0, repository.LastActor?.UserId);
        Assert.Equal("corr-1", repository.LastActor?.CorrelationId);
    }

    [Fact]
    public async Task RespondAsync_Accept_OfferAlreadyAccepted_ReplaysExistingHandoff()
    {
        var repository = new FakeOfferRepository
        {
            Offer = OfferTests.CreateOffer(OfferStatus.Accepted, version: 4),
            ExistingHandoff = new OfferHandoffResult(900, 901, [1001, 1002])
        };

        var result = await CreateService(repository).RespondAsync(Respond(OfferDecision.Accept), CancellationToken.None);

        Assert.True(result.Replayed);
        Assert.Equal(OfferStatus.Accepted, result.Status);
        Assert.Equal(900, result.EmployeeId);
        Assert.Equal(901, result.InitialContractId);
        Assert.Equal([1001L, 1002L], result.OnboardingTaskIds);
        Assert.Equal(0, repository.AcceptanceCalls);
        Assert.Empty(repository.Transitions);
    }

    [Fact]
    public async Task RespondAsync_Accept_EmployeeAlreadyLinked_ReplaysWithoutWriting()
    {
        var repository = new FakeOfferRepository
        {
            Offer = OfferTests.CreateOffer(OfferStatus.Sent, version: 3),
            ExistingHandoff = new OfferHandoffResult(900, 901, [1001])
        };

        var result = await CreateService(repository).RespondAsync(Respond(OfferDecision.Accept), CancellationToken.None);

        Assert.True(result.Replayed);
        Assert.Equal(900, result.EmployeeId);
        Assert.Equal(0, repository.AcceptanceCalls);
        Assert.Equal(0, repository.HandoffContextCalls);
    }

    [Theory]
    [InlineData(OfferStatus.Draft)]
    [InlineData(OfferStatus.Approved)]
    [InlineData(OfferStatus.Declined)]
    [InlineData(OfferStatus.Expired)]
    [InlineData(OfferStatus.Cancelled)]
    public async Task RespondAsync_Accept_OfferNotOpen_ThrowsNotOpenWithoutWriting(OfferStatus status)
    {
        var repository = new FakeOfferRepository { Offer = OfferTests.CreateOffer(status, version: 2) };

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            CreateService(repository).RespondAsync(Respond(OfferDecision.Accept), CancellationToken.None));

        Assert.Equal(OfferService.NotOpenCode, exception.Code);
        Assert.Equal(status.ToContract(), exception.Details["currentStatus"]);
        Assert.Equal(0, repository.AcceptanceCalls);
        Assert.Empty(repository.Transitions);
    }

    [Fact]
    public async Task RespondAsync_Accept_SentPastExpiration_ExpiresThenThrowsNotOpen()
    {
        var repository = new FakeOfferRepository { Offer = OfferTests.CreateOffer(OfferStatus.Sent, version: 3, expirationDate: Today.AddDays(-1)) };

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            CreateService(repository).RespondAsync(Respond(OfferDecision.Accept), CancellationToken.None));

        Assert.Equal(OfferService.NotOpenCode, exception.Code);
        Assert.Equal("expired", exception.Details["currentStatus"]);
        Assert.Equal([OfferTransition.Expire], repository.Transitions);
        Assert.Equal(0, repository.AcceptanceCalls);
    }

    [Fact]
    public async Task RespondAsync_Accept_JobPostingWithoutPosition_ThrowsPositionRequiredWithoutWriting()
    {
        var context = OfferHandoffTests.ValidContext();
        var repository = new FakeOfferRepository
        {
            Offer = OfferTests.CreateOffer(OfferStatus.Sent, version: 3),
            HandoffContext = context with { JobPosting = new HandoffJobPosting(4, null) }
        };

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            CreateService(repository).RespondAsync(Respond(OfferDecision.Accept), CancellationToken.None));

        Assert.Equal(OfferHandoff.PositionRequiredCode, exception.Code);
        Assert.Equal(0, repository.AcceptanceCalls);
    }

    [Fact]
    public async Task RespondAsync_Accept_LostRaceToConcurrentAccept_ReplaysWinner()
    {
        var repository = new FakeOfferRepository
        {
            Offer = OfferTests.CreateOffer(OfferStatus.Sent, version: 3),
            HandoffContext = OfferHandoffTests.ValidContext(),
            AcceptanceSucceeds = false,
            HandoffAfterLostRace = new OfferHandoffResult(950, 951, [1101])
        };

        var result = await CreateService(repository).RespondAsync(Respond(OfferDecision.Accept), CancellationToken.None);

        Assert.True(result.Replayed);
        Assert.Equal(950, result.EmployeeId);
        Assert.Equal(1, repository.AcceptanceCalls);
    }

    [Fact]
    public async Task RespondAsync_Accept_LostRaceWithoutEmployee_ThrowsConcurrency()
    {
        var repository = new FakeOfferRepository
        {
            Offer = OfferTests.CreateOffer(OfferStatus.Sent, version: 3),
            HandoffContext = OfferHandoffTests.ValidContext(),
            AcceptanceSucceeds = false
        };

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() =>
            CreateService(repository).RespondAsync(Respond(OfferDecision.Accept), CancellationToken.None));

        Assert.Equal(1, repository.AcceptanceCalls);
    }

    [Fact]
    public async Task RespondAsync_Decline_FirstTime_SavesDeclineWithReason()
    {
        var repository = new FakeOfferRepository { Offer = OfferTests.CreateOffer(OfferStatus.Sent, version: 3) };

        var result = await CreateService(repository).RespondAsync(Respond(OfferDecision.Decline, "  Salary below expectation "), CancellationToken.None);

        Assert.False(result.Replayed);
        Assert.Equal(OfferStatus.Declined, result.Status);
        Assert.Null(result.EmployeeId);
        Assert.Empty(result.OnboardingTaskIds);
        Assert.Equal([OfferTransition.Decline], repository.Transitions);
        Assert.Equal("Salary below expectation", repository.LastReason);
        Assert.Equal(OfferStatus.Declined, repository.Offer!.Status);
        Assert.Equal(0, repository.AcceptanceCalls);
    }

    [Fact]
    public async Task RespondAsync_Decline_AlreadyDeclined_Replays()
    {
        var repository = new FakeOfferRepository { Offer = OfferTests.CreateOffer(OfferStatus.Declined, version: 4) };

        var result = await CreateService(repository).RespondAsync(Respond(OfferDecision.Decline), CancellationToken.None);

        Assert.True(result.Replayed);
        Assert.Equal(OfferStatus.Declined, result.Status);
        Assert.Empty(repository.Transitions);
    }

    [Fact]
    public async Task RespondAsync_Decline_AcceptedOffer_ThrowsNotOpen()
    {
        var repository = new FakeOfferRepository { Offer = OfferTests.CreateOffer(OfferStatus.Accepted, version: 4) };

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            CreateService(repository).RespondAsync(Respond(OfferDecision.Decline), CancellationToken.None));

        Assert.Equal(OfferService.NotOpenCode, exception.Code);
    }

    [Fact]
    public async Task RespondAsync_Decline_SaveLosesRace_ThrowsConcurrency()
    {
        var repository = new FakeOfferRepository { Offer = OfferTests.CreateOffer(OfferStatus.Sent, version: 3), SaveSucceeds = false };

        await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() =>
            CreateService(repository).RespondAsync(Respond(OfferDecision.Decline), CancellationToken.None));
    }

    [Fact]
    public async Task RespondAsync_UnknownOffer_ThrowsNotFound()
    {
        var exception = await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            CreateService(new FakeOfferRepository()).RespondAsync(Respond(OfferDecision.Accept), CancellationToken.None));

        Assert.Equal("Offer", exception.Resource);
    }

    [Fact]
    public async Task ExpireDueOffersAsync_ExpiresEachDueOfferAndReportsRaces()
    {
        var first = OfferTests.CreateOffer(OfferStatus.Sent, version: 2, expirationDate: Today.AddDays(-2), id: 1);
        var second = OfferTests.CreateOffer(OfferStatus.Sent, version: 5, expirationDate: Today.AddDays(-1), id: 2);
        var repository = new FakeOfferRepository { ExpiredSent = [first, second], FailSaveForOfferIds = [2] };

        var result = await CreateService(repository).ExpireDueOffersAsync(Today, CoreHrActor.System("worker"), CancellationToken.None);

        Assert.Equal(1, result.Expired);
        Assert.Equal([2L], result.Conflicted);
        Assert.Equal([OfferTransition.Expire, OfferTransition.Expire], repository.Transitions);
        Assert.Equal(OfferStatus.Expired, first.Status);
        Assert.Equal(3, first.Version);
    }

    private static OfferService CreateService(FakeOfferRepository repository, FakeTokenService? tokens = null) =>
        new(repository, tokens ?? new FakeTokenService(), new FixedTimeProvider(Now));

    private static RespondToOfferCommand Respond(OfferDecision decision, string? reason = null) =>
        new(42, decision, reason, "corr-1");

    private static CoreHrActor Actor(string permission, long userId = 1) => new(
        userId,
        EmployeeId: null,
        CoreHrDataScope.Organization,
        new HashSet<string>(StringComparer.Ordinal) { permission },
        "test-correlation");

    private sealed class FakeTokenService : IOfferResponseTokenService
    {
        public string? LastIssued { get; private set; }
        public long? LastOfferId { get; private set; }
        public DateTimeOffset? LastExpiresAt { get; private set; }

        public string Issue(long offerId, DateTimeOffset expiresAt)
        {
            LastOfferId = offerId;
            LastExpiresAt = expiresAt;
            LastIssued = $"token-{offerId}-{expiresAt.ToUnixTimeSeconds()}";
            return LastIssued;
        }

        public bool TryValidate(string? token, long offerId, DateTimeOffset now) => token == LastIssued;
    }

    private sealed class FakeOfferRepository : IOfferRepository
    {
        public OfferApplication? Application { get; init; }
        public Offer? Offer { get; set; }
        public Offer? OfferAfterReload { get; init; }
        public bool HasOpenOffer { get; init; }
        public bool InsertSucceeds { get; init; } = true;
        public bool SaveSucceeds { get; init; } = true;
        public bool AcceptanceSucceeds { get; init; } = true;
        public HashSet<long> FailSaveForOfferIds { get; init; } = [];
        public OfferHandoffContext? HandoffContext { get; init; }
        public OfferHandoffResult? ExistingHandoff { get; set; }
        public OfferHandoffResult? HandoffAfterLostRace { get; init; }
        public List<Offer> ExpiredSent { get; init; } = [];

        public int InsertCalls { get; private set; }
        public int AcceptanceCalls { get; private set; }
        public int HandoffContextCalls { get; private set; }
        public Offer? Inserted { get; private set; }
        public List<OfferTransition> Transitions { get; } = [];
        public OfferSnapshot? LastBefore { get; private set; }
        public string? LastReason { get; private set; }
        public string? LastResponseToken { get; private set; }
        public OfferHandoffPlan? LastPlan { get; private set; }
        public CoreHrActor? LastActor { get; private set; }

        private bool _reloaded;

        public Task<PagedResult<Offer>> SearchAsync(OfferSearchQuery query, CoreHrActor actor, CancellationToken cancellationToken) =>
            Task.FromResult(PagedResult<Offer>.Empty(query.Page));

        public Task<Offer?> GetByIdAsync(long offerId, CoreHrActor actor, CancellationToken cancellationToken) => Load(offerId);

        public Task<Offer?> GetForCandidateAsync(long offerId, CancellationToken cancellationToken) => Load(offerId);

        public Task<OfferApplication?> GetApplicationAsync(long applicationId, CoreHrActor actor, CancellationToken cancellationToken) =>
            Task.FromResult(Application?.Id == applicationId ? Application : null);

        public Task<bool> HasOpenOfferAsync(long applicationId, CancellationToken cancellationToken) => Task.FromResult(HasOpenOffer);

        public Task<Offer?> InsertAsync(Offer offer, CoreHrActor actor, CancellationToken cancellationToken)
        {
            InsertCalls++;
            Inserted = offer;
            if (!InsertSucceeds)
            {
                return Task.FromResult<Offer?>(null);
            }

            return Task.FromResult<Offer?>(new Offer(
                100, offer.ApplicationId, offer.BaseSalary, offer.BonusAmount, offer.AllowanceAmount, offer.Currency,
                offer.EmploymentType, offer.StartDate, offer.ExpirationDate, offer.Status, offer.TemplateVersion,
                offer.DocumentObjectKey, offer.ApprovedBy, offer.ApprovedAt, offer.SentAt, offer.RespondedAt,
                offer.Version, offer.CreatedAt, offer.UpdatedAt));
        }

        public Task<bool> SaveTransitionAsync(
            Offer offer,
            OfferTransition transition,
            OfferSnapshot before,
            string? reason,
            string? responseToken,
            CoreHrActor actor,
            CancellationToken cancellationToken)
        {
            Transitions.Add(transition);
            LastBefore = before;
            LastReason = reason;
            LastResponseToken = responseToken;
            LastActor = actor;
            return Task.FromResult(SaveSucceeds && !FailSaveForOfferIds.Contains(offer.Id));
        }

        public Task<IReadOnlyList<Offer>> ListExpiredSentAsync(DateOnly today, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Offer>>(ExpiredSent);

        public Task<OfferHandoffContext?> GetHandoffContextAsync(long applicationId, CancellationToken cancellationToken)
        {
            HandoffContextCalls++;
            return Task.FromResult(HandoffContext);
        }

        public Task<OfferHandoffResult?> FindHandoffAsync(long applicationId, CancellationToken cancellationToken) =>
            Task.FromResult(ExistingHandoff);

        public Task<OfferHandoffResult?> SaveAcceptanceAsync(
            Offer offer,
            OfferSnapshot before,
            OfferHandoffPlan plan,
            CoreHrActor actor,
            CancellationToken cancellationToken)
        {
            AcceptanceCalls++;
            LastBefore = before;
            LastPlan = plan;
            LastActor = actor;
            if (!AcceptanceSucceeds)
            {
                ExistingHandoff = HandoffAfterLostRace;
                return Task.FromResult<OfferHandoffResult?>(null);
            }

            var taskIds = plan.Tasks.Select((_, index) => 1001L + index).ToList();
            return Task.FromResult<OfferHandoffResult?>(new OfferHandoffResult(900, 901, taskIds));
        }

        private Task<Offer?> Load(long offerId)
        {
            if (Offer?.Id != offerId)
            {
                return Task.FromResult<Offer?>(null);
            }

            if (_reloaded && OfferAfterReload is not null)
            {
                return Task.FromResult<Offer?>(OfferAfterReload);
            }

            _reloaded = true;
            return Task.FromResult<Offer?>(Offer);
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
