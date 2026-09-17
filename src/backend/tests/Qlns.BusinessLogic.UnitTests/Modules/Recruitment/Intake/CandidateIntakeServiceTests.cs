using System.Text;
using Qlns.BusinessLogic.Modules.CoreHr.EmployeeDocuments;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Applications;
using Qlns.BusinessLogic.Modules.Recruitment.Intake;
using Qlns.BusinessLogic.Modules.Recruitment.Requisitions;
using Xunit;

namespace Qlns.BusinessLogic.UnitTests.Modules.Recruitment.Intake;

public sealed class CandidateIntakeServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 17, 8, 30, 0, TimeSpan.Zero);
    private static readonly Guid IntakeId = Guid.Parse("3f2504e0-4f89-41d3-9a0c-0305e82c3301");

    private const long RequisitionId = 20;
    private const long DepartmentId = 3;
    private const long ActorUserId = 7;

    [Fact]
    public async Task StartAsync_WithoutWritePermission_ThrowsForbiddenBeforeAnyWork()
    {
        var repository = new FakeIntakeRepository { Requisition = Requisition(RequisitionStatus.ActiveRecruiting) };
        var scanner = new FakeScanner(MalwareScanResult.Clean);
        var service = CreateService(repository, scanner: scanner);

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() =>
            service.StartAsync(Upload(Actor(IntakePermissions.Read)), CancellationToken.None));

        Assert.Equal(CandidateIntakeService.WriteForbiddenCode, exception.Code);
        Assert.Equal(0, scanner.Calls);
        Assert.Equal(0, repository.InsertCalls);
    }

    [Fact]
    public async Task StartAsync_RequisitionMissingOrOutOfScope_ThrowsNotFoundWithoutScanning()
    {
        var repository = new FakeIntakeRepository();
        var scanner = new FakeScanner(MalwareScanResult.Clean);
        var storage = new FakeStorage();
        var service = CreateService(repository, storage, scanner);

        var exception = await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            service.StartAsync(Upload(Actor()), CancellationToken.None));

        Assert.Equal("Requisition", exception.Resource);
        Assert.Equal(RequisitionId, exception.Id);
        Assert.Equal(0, scanner.Calls);
        Assert.Empty(storage.Stored);
    }

    [Fact]
    public async Task StartAsync_ConsentNotGiven_ThrowsValidationOnConsented()
    {
        var repository = new FakeIntakeRepository { Requisition = Requisition(RequisitionStatus.ActiveRecruiting) };
        var scanner = new FakeScanner(MalwareScanResult.Clean);
        var service = CreateService(repository, scanner: scanner);

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() =>
            service.StartAsync(Upload(Actor()) with { Consented = false }, CancellationToken.None));

        Assert.Equal(["consented"], exception.Errors.Keys);
        Assert.Equal(0, scanner.Calls);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("  ")]
    public async Task StartAsync_MissingPrivacyNoticeVersion_ThrowsValidation(string? version)
    {
        var repository = new FakeIntakeRepository { Requisition = Requisition(RequisitionStatus.ActiveRecruiting) };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() =>
            service.StartAsync(Upload(Actor()) with { PrivacyNoticeVersion = version }, CancellationToken.None));

        Assert.Equal(["privacyNoticeVersion"], exception.Errors.Keys);
    }

    [Theory]
    [InlineData("application/zip")]
    [InlineData("image/png")]
    public async Task StartAsync_UnsupportedContentType_ThrowsValidationOnFile(string contentType)
    {
        var repository = new FakeIntakeRepository { Requisition = Requisition(RequisitionStatus.ActiveRecruiting) };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() =>
            service.StartAsync(Upload(Actor()) with { ContentType = contentType }, CancellationToken.None));

        Assert.Equal(["file"], exception.Errors.Keys);
    }

    [Fact]
    public async Task StartAsync_FileTooLarge_ThrowsValidationOnFile()
    {
        var repository = new FakeIntakeRepository { Requisition = Requisition(RequisitionStatus.ActiveRecruiting) };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() =>
            service.StartAsync(Upload(Actor()) with { SizeBytes = ResumeUploadRules.MaxSizeBytes + 1 }, CancellationToken.None));

        Assert.Equal(["file"], exception.Errors.Keys);
    }

    [Theory]
    [InlineData(RequisitionStatus.Draft)]
    [InlineData(RequisitionStatus.PendingApproval)]
    [InlineData(RequisitionStatus.Approved)]
    [InlineData(RequisitionStatus.Closed)]
    [InlineData(RequisitionStatus.Cancelled)]
    public async Task StartAsync_RequisitionNotActive_ThrowsRequisitionNotOpenWithoutScanning(RequisitionStatus status)
    {
        var repository = new FakeIntakeRepository { Requisition = Requisition(status) };
        var scanner = new FakeScanner(MalwareScanResult.Clean);
        var service = CreateService(repository, scanner: scanner);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            service.StartAsync(Upload(Actor()), CancellationToken.None));

        Assert.Equal(CandidateIntake.RequisitionNotOpenCode, exception.Code);
        Assert.Equal(status.ToContract(), exception.Details["currentStatus"]);
        Assert.Equal(0, scanner.Calls);
    }

    [Fact]
    public async Task StartAsync_InfectedFile_ThrowsValidationOnFileAndStoresNothing()
    {
        var repository = new FakeIntakeRepository { Requisition = Requisition(RequisitionStatus.ActiveRecruiting) };
        var storage = new FakeStorage();
        var parser = new FakeParser();
        var service = CreateService(repository, storage, new FakeScanner(MalwareScanResult.Infected), parser);

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() =>
            service.StartAsync(Upload(Actor()), CancellationToken.None));

        Assert.Equal(["file"], exception.Errors.Keys);
        Assert.Empty(storage.Stored);
        Assert.Equal(0, parser.Calls);
        Assert.Equal(0, repository.InsertCalls);
    }

    [Fact]
    public async Task StartAsync_ScannerUnavailable_ThrowsScanUnavailableAndStoresNothing()
    {
        var repository = new FakeIntakeRepository { Requisition = Requisition(RequisitionStatus.ActiveRecruiting) };
        var storage = new FakeStorage();
        var service = CreateService(repository, storage, new FakeScanner(MalwareScanResult.Unavailable));

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            service.StartAsync(Upload(Actor()), CancellationToken.None));

        Assert.Equal(CandidateIntake.ScanUnavailableCode, exception.Code);
        Assert.Empty(storage.Stored);
        Assert.Equal(0, repository.InsertCalls);
    }

    [Fact]
    public async Task StartAsync_HappyPath_ScansParsesStoresRewoundStreamAndInsertsOnce()
    {
        var repository = new FakeIntakeRepository { Requisition = Requisition(RequisitionStatus.ActiveRecruiting) };
        var storage = new FakeStorage();
        var scanner = new FakeScanner(MalwareScanResult.Clean);
        var parser = new FakeParser
        {
            Result = new ResumeParseResult(
                new CandidateInput("An", "Nguyen", "an@example.com", null, null, null),
                new Dictionary<string, double> { ["email"] = 0.8 },
                "parser/2.0")
        };
        var service = CreateService(repository, storage, scanner, parser);
        var actor = Actor();

        var view = await service.StartAsync(Upload(actor) with { PrivacyNoticeVersion = " v3 ", ContentType = "Application/PDF" }, CancellationToken.None);

        Assert.Equal(1, scanner.Calls);
        Assert.Equal(1, parser.Calls);
        Assert.Equal(0, parser.PositionAtParse);
        Assert.Equal("application/pdf", parser.LastContentType);

        var stored = Assert.Single(storage.Stored);
        Assert.StartsWith($"resumes/{RequisitionId}/", stored.ObjectKey, StringComparison.Ordinal);
        Assert.Equal("application/pdf", stored.ContentType);
        Assert.Equal(0, stored.PositionAtStore);
        Assert.Equal("hello", stored.Content);

        Assert.Equal(1, repository.InsertCalls);
        var inserted = repository.LastInserted!;
        Assert.Same(actor, repository.LastActor);
        Assert.Equal(stored.ObjectKey, inserted.ObjectKey);
        Assert.Equal(RequisitionId, inserted.RequisitionId);
        Assert.Equal(IntakeStatus.AwaitingConfirmation, inserted.Status);
        Assert.Equal(CandidateIntake.ScanClean, inserted.MalwareScanStatus);
        Assert.Equal(CandidateIntake.ParserCompleted, inserted.ParserStatus);
        Assert.Equal("parser/2.0", inserted.ParserVersion);
        Assert.Equal("An", inserted.ParsedCandidate!.FirstName);
        Assert.Equal(0.8, inserted.Confidence["email"]);
        Assert.Equal("v3", inserted.PrivacyNoticeVersion);
        Assert.Equal(Now, inserted.ConsentedAt);
        Assert.Equal(ActorUserId, inserted.UploadedBy);
        Assert.Equal(Now, inserted.UploadedAt);
        Assert.Equal("cv.pdf", inserted.OriginalFileName);

        Assert.Equal(500, view.Intake.Id);
        Assert.Empty(view.DuplicateCandidates);
        Assert.Empty(storage.Deleted);
    }

    [Fact]
    public async Task StartAsync_InsertFails_DeletesStoredObjectAndRethrows()
    {
        var repository = new FakeIntakeRepository { Requisition = Requisition(RequisitionStatus.ActiveRecruiting), InsertFails = true };
        var storage = new FakeStorage();
        var service = CreateService(repository, storage);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.StartAsync(Upload(Actor()), CancellationToken.None));

        var stored = Assert.Single(storage.Stored);
        Assert.Equal([stored.ObjectKey], storage.Deleted);
    }

    [Fact]
    public async Task GetAsync_MissingOrOutOfScope_ThrowsNotFound()
    {
        var service = CreateService(new FakeIntakeRepository());

        var exception = await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            service.GetAsync(IntakeId, Actor(IntakePermissions.Read), CancellationToken.None));

        Assert.Equal("Intake", exception.Resource);
    }

    [Fact]
    public async Task GetAsync_DuplicateReview_ResolvesDuplicateCandidateSummaries()
    {
        var intake = Intake(IntakeStatus.DuplicateReview, duplicateIds: [55, 56]);
        var repository = new FakeIntakeRepository
        {
            Intake = intake,
            Candidates = [ExistingCandidate(55, "an@example.com", "84912345678"), ExistingCandidate(56, "other@example.com", null)]
        };
        var service = CreateService(repository);

        var view = await service.GetAsync(IntakeId, Actor(IntakePermissions.Read), CancellationToken.None);

        Assert.Same(intake, view.Intake);
        Assert.Equal([55L, 56L], view.DuplicateCandidates.Select(candidate => candidate.Id));
        Assert.Equal([55L, 56L], repository.LastRequestedCandidateIds);
    }

    [Fact]
    public async Task GetAsync_WithoutDuplicates_DoesNotQueryCandidates()
    {
        var repository = new FakeIntakeRepository { Intake = Intake(IntakeStatus.AwaitingConfirmation) };
        var service = CreateService(repository);

        var view = await service.GetAsync(IntakeId, Actor(IntakePermissions.Read), CancellationToken.None);

        Assert.Empty(view.DuplicateCandidates);
        Assert.Null(repository.LastRequestedCandidateIds);
    }

    [Fact]
    public async Task ConfirmAsync_WithoutWritePermission_ThrowsForbidden()
    {
        var repository = new FakeIntakeRepository { Intake = Intake(IntakeStatus.AwaitingConfirmation), Requisition = Requisition(RequisitionStatus.ActiveRecruiting) };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() =>
            service.ConfirmAsync(Confirm(Actor(IntakePermissions.Read)), CancellationToken.None));

        Assert.Equal(CandidateIntakeService.WriteForbiddenCode, exception.Code);
        Assert.Equal(0, repository.ConfirmCalls);
    }

    [Fact]
    public async Task ConfirmAsync_IntakeMissingOrOutOfScope_ThrowsNotFound()
    {
        var service = CreateService(new FakeIntakeRepository());

        var exception = await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            service.ConfirmAsync(Confirm(Actor()), CancellationToken.None));

        Assert.Equal("Intake", exception.Resource);
    }

    [Fact]
    public async Task ConfirmAsync_AlreadyCompleted_ReplaysExistingApplicationWithoutWriting()
    {
        var existingApplication = new RecruitmentApplication(900, 55, RequisitionId, 5, ApplicationStage.AiScreening, 70m, "direct", Now.AddDays(-1), 3, Now.AddHours(-1));
        var existingCandidate = ExistingCandidate(55, "an@example.com", null);
        var requisition = Requisition(RequisitionStatus.ActiveRecruiting);
        var repository = new FakeIntakeRepository
        {
            Intake = Intake(IntakeStatus.Completed),
            Requisition = requisition,
            Confirmation = new IntakeConfirmation(existingApplication, existingCandidate)
        };
        var service = CreateService(repository);

        var result = await service.ConfirmAsync(Confirm(Actor()), CancellationToken.None);

        Assert.Same(existingApplication, result.Application);
        Assert.Same(existingCandidate, result.Candidate);
        Assert.Same(requisition, result.Requisition);
        Assert.Equal(0, repository.ConfirmCalls);
        Assert.Equal(0, repository.DuplicateReviewSaves);
        Assert.Equal(0, repository.DuplicateSearches);
    }

    [Theory]
    [InlineData(IntakeStatus.Rejected)]
    [InlineData(IntakeStatus.Failed)]
    [InlineData(IntakeStatus.Scanning)]
    public async Task ConfirmAsync_NotConfirmableStatus_ThrowsNotConfirmable(IntakeStatus status)
    {
        var repository = new FakeIntakeRepository { Intake = Intake(status), Requisition = Requisition(RequisitionStatus.ActiveRecruiting) };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            service.ConfirmAsync(Confirm(Actor()), CancellationToken.None));

        Assert.Equal(CandidateIntake.NotConfirmableCode, exception.Code);
        Assert.Equal(status.ToContract(), exception.Details["currentStatus"]);
        Assert.Equal(0, repository.ConfirmCalls);
    }

    [Fact]
    public async Task ConfirmAsync_InvalidCandidate_ThrowsValidationBeforeDuplicateSearch()
    {
        var repository = new FakeIntakeRepository { Intake = Intake(IntakeStatus.AwaitingConfirmation), Requisition = Requisition(RequisitionStatus.ActiveRecruiting) };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() =>
            service.ConfirmAsync(Confirm(Actor()) with { Candidate = ValidCandidate() with { Email = "nope" } }, CancellationToken.None));

        Assert.Equal(["candidate.email"], exception.Errors.Keys);
        Assert.Equal(0, repository.DuplicateSearches);
        Assert.Equal(0, repository.ConfirmCalls);
    }

    [Fact]
    public async Task ConfirmAsync_SourceTooLong_ThrowsValidationOnSource()
    {
        var repository = new FakeIntakeRepository { Intake = Intake(IntakeStatus.AwaitingConfirmation), Requisition = Requisition(RequisitionStatus.ActiveRecruiting) };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() =>
            service.ConfirmAsync(Confirm(Actor()) with { Source = new string('s', 81) }, CancellationToken.None));

        Assert.Equal(["source"], exception.Errors.Keys);
        Assert.Equal(0, repository.ConfirmCalls);
    }

    [Fact]
    public async Task ConfirmAsync_DuplicatesWithoutChoice_PersistsDuplicateReviewAndThrows409WithCandidates()
    {
        var intake = Intake(IntakeStatus.AwaitingConfirmation);
        var repository = new FakeIntakeRepository
        {
            Intake = intake,
            Requisition = Requisition(RequisitionStatus.ActiveRecruiting),
            Candidates = [ExistingCandidate(55, "an@example.com", null), ExistingCandidate(56, "other@example.com", "84912345678")]
        };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            service.ConfirmAsync(Confirm(Actor()), CancellationToken.None));

        Assert.Equal(CandidateIntake.DuplicateReviewCode, exception.Code);
        Assert.Equal(("an@example.com", "84912345678"), repository.LastDuplicateSearch);
        Assert.Equal(IntakeStatus.DuplicateReview, intake.Status);
        Assert.Equal([55L, 56L], intake.DuplicateCandidateIds);
        Assert.Equal(1, repository.DuplicateReviewSaves);
        Assert.Same(intake, repository.LastDuplicateReviewIntake);
        Assert.Equal(Now, repository.LastDuplicateReviewAt);
        var duplicates = Assert.IsAssignableFrom<IReadOnlyList<DuplicateCandidate>>(exception.Details["duplicateCandidates"]);
        Assert.Equal([55L, 56L], duplicates.Select(duplicate => duplicate.Id));
        Assert.Equal(0, repository.ConfirmCalls);
    }

    [Fact]
    public async Task ConfirmAsync_ExistingCandidateIdNotAmongDuplicates_ThrowsValidationOnExistingCandidateId()
    {
        var repository = new FakeIntakeRepository
        {
            Intake = Intake(IntakeStatus.DuplicateReview, duplicateIds: [55]),
            Requisition = Requisition(RequisitionStatus.ActiveRecruiting),
            Candidates = [ExistingCandidate(55, "an@example.com", null)]
        };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() =>
            service.ConfirmAsync(Confirm(Actor()) with { ExistingCandidateId = 99 }, CancellationToken.None));

        Assert.Equal(["existingCandidateId"], exception.Errors.Keys);
        Assert.Equal(0, repository.ConfirmCalls);
        Assert.Equal(0, repository.DuplicateReviewSaves);
    }

    [Fact]
    public async Task ConfirmAsync_ExistingCandidateChosen_LinksRefreshedCandidateWithExpectedVersion()
    {
        var existing = ExistingCandidate(55, "an@example.com", null, version: 4);
        var repository = new FakeIntakeRepository
        {
            Intake = Intake(IntakeStatus.DuplicateReview, duplicateIds: [55]),
            Requisition = Requisition(RequisitionStatus.ActiveRecruiting),
            Candidates = [existing]
        };
        var service = CreateService(repository);

        var result = await service.ConfirmAsync(
            Confirm(Actor()) with { ExistingCandidateId = 55, Candidate = ValidCandidate() with { Phone = "0999 000 111", LinkedinUrl = "https://linkedin.com/in/an" } },
            CancellationToken.None);

        Assert.Equal(1, repository.ConfirmCalls);
        Assert.Same(existing, repository.LastConfirmCandidate);
        Assert.Equal(4, repository.LastExpectedCandidateVersion);
        Assert.Equal(5, existing.Version);
        Assert.Equal("0999 000 111", existing.Phone);
        Assert.Equal("84999000111", existing.NormalizedPhone);
        Assert.Equal("https://linkedin.com/in/an", existing.LinkedinUrl);
        Assert.Equal("direct", repository.LastConfirmSource);
        Assert.Equal(Now, repository.LastConfirmNow);
        Assert.Equal(55, result.Application.CandidateId);
        Assert.Equal(1, repository.ApplicationExistsChecks);
    }

    [Fact]
    public async Task ConfirmAsync_NoDuplicates_CreatesCandidateWithConsentFromUploadAndNormalizedSource()
    {
        var intake = Intake(IntakeStatus.AwaitingConfirmation);
        var requisition = Requisition(RequisitionStatus.ActiveRecruiting);
        var repository = new FakeIntakeRepository { Intake = intake, Requisition = requisition };
        var service = CreateService(repository);
        var actor = Actor();

        var result = await service.ConfirmAsync(Confirm(actor) with { Source = "  referral " }, CancellationToken.None);

        Assert.Equal(1, repository.ConfirmCalls);
        Assert.Same(intake, repository.LastConfirmIntake);
        Assert.Same(actor, repository.LastActor);
        var candidate = repository.LastConfirmCandidate!;
        Assert.Equal(0, candidate.Id);
        Assert.Equal("an@example.com", candidate.NormalizedEmail);
        Assert.Equal("84912345678", candidate.NormalizedPhone);
        Assert.Equal(intake.PrivacyNoticeVersion, candidate.PrivacyNoticeVersion);
        Assert.Equal(intake.ConsentedAt, candidate.ConsentedAt);
        Assert.Null(repository.LastExpectedCandidateVersion);
        Assert.Equal("referral", repository.LastConfirmSource);
        Assert.Equal(0, repository.ApplicationExistsChecks);
        Assert.Equal(0, repository.DuplicateReviewSaves);
        Assert.Same(requisition, result.Requisition);
        Assert.Equal(ApplicationStage.SourcedApplied, result.Application.Stage);
        Assert.Equal(intake.Id, result.Application.ResumeId);
    }

    [Fact]
    public async Task ConfirmAsync_ExistingCandidateAlreadyApplied_ThrowsAlreadyExistsWithoutWriting()
    {
        var repository = new FakeIntakeRepository
        {
            Intake = Intake(IntakeStatus.DuplicateReview, duplicateIds: [55]),
            Requisition = Requisition(RequisitionStatus.ActiveRecruiting),
            Candidates = [ExistingCandidate(55, "an@example.com", null)],
            ApplicationExists = true
        };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            service.ConfirmAsync(Confirm(Actor()) with { ExistingCandidateId = 55 }, CancellationToken.None));

        Assert.Equal(CandidateIntakeService.ApplicationAlreadyExistsCode, exception.Code);
        Assert.Equal(55L, exception.Details["candidateId"]);
        Assert.Equal(RequisitionId, exception.Details["requisitionId"]);
        Assert.Equal(0, repository.ConfirmCalls);
    }

    [Fact]
    public async Task ConfirmAsync_CandidateUpdateLosesRace_ThrowsConcurrency()
    {
        var repository = new FakeIntakeRepository
        {
            Intake = Intake(IntakeStatus.DuplicateReview, duplicateIds: [55]),
            Requisition = Requisition(RequisitionStatus.ActiveRecruiting),
            Candidates = [ExistingCandidate(55, "an@example.com", null)],
            ConfirmSucceeds = false
        };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrConcurrencyConflictException>(() =>
            service.ConfirmAsync(Confirm(Actor()) with { ExistingCandidateId = 55 }, CancellationToken.None));

        Assert.Equal("candidate", exception.Resource);
        Assert.Equal(1, repository.ConfirmCalls);
    }

    private static CandidateIntakeService CreateService(
        FakeIntakeRepository repository,
        FakeStorage? storage = null,
        FakeScanner? scanner = null,
        FakeParser? parser = null) => new(
        repository,
        storage ?? new FakeStorage(),
        scanner ?? new FakeScanner(MalwareScanResult.Clean),
        parser ?? new FakeParser(),
        new FixedTimeProvider(Now));

    private static StartIntakeCommand Upload(CoreHrActor actor)
    {
        var content = new MemoryStream(Encoding.UTF8.GetBytes("hello"));
        return new StartIntakeCommand(RequisitionId, "cv.pdf", "application/pdf", content.Length, content, "v1", Consented: true, actor);
    }

    private static ConfirmIntakeCommand Confirm(CoreHrActor actor) =>
        new(IntakeId, ValidCandidate(), ExistingCandidateId: null, Source: null, actor);

    private static CandidateInput ValidCandidate() =>
        new("An", "Nguyen", "An@Example.com", "0912 345 678", null, null);

    private static CoreHrActor Actor(params string[] permissions) => new(
        UserId: ActorUserId,
        EmployeeId: null,
        DataScope: CoreHrDataScope.Departments(DepartmentId),
        Permissions: (permissions.Length == 0 ? [IntakePermissions.Write] : permissions).ToHashSet(StringComparer.Ordinal),
        CorrelationId: "test-correlation");

    private static Requisition Requisition(RequisitionStatus status) => new(
        RequisitionId, "REQ-2026-00020", "Senior Engineer", DepartmentId, null, null, null, null,
        EmploymentType.FullTime, null, null, 1, status, null, null, 7, 1, Now.AddDays(-5), Now.AddDays(-5));

    private static CandidateIntake Intake(IntakeStatus status, long[]? duplicateIds = null) => new(
        5, IntakeId, RequisitionId, candidateId: null, "resumes/20/abc", "cv.pdf", "application/pdf", 100,
        status, CandidateIntake.ScanClean, CandidateIntake.ParserCompleted, parsedCandidate: null,
        new Dictionary<string, double>(), "parser/1.0", "v1", Now.AddHours(-2), duplicateIds ?? [],
        ActorUserId, Now.AddHours(-2), confirmedBy: null, confirmedAt: null);

    private static Candidate ExistingCandidate(long id, string email, string? normalizedPhone, long version = 1) => new(
        id, "Existing", "Person", email, email, normalizedPhone, normalizedPhone, null, null, "v0", Now.AddDays(-40),
        version, Now.AddDays(-40), Now.AddDays(-40));

    private sealed class FakeIntakeRepository : ICandidateIntakeRepository
    {
        public Requisition? Requisition { get; init; }
        public CandidateIntake? Intake { get; init; }
        public List<Candidate> Candidates { get; init; } = [];
        public bool ApplicationExists { get; init; }
        public IntakeConfirmation? Confirmation { get; init; }
        public bool ConfirmSucceeds { get; init; } = true;
        public bool InsertFails { get; init; }

        public int InsertCalls { get; private set; }
        public int ConfirmCalls { get; private set; }
        public int DuplicateReviewSaves { get; private set; }
        public int DuplicateSearches { get; private set; }
        public int ApplicationExistsChecks { get; private set; }
        public CandidateIntake? LastInserted { get; private set; }
        public CoreHrActor? LastActor { get; private set; }
        public IReadOnlyCollection<long>? LastRequestedCandidateIds { get; private set; }
        public (string Email, string? Phone)? LastDuplicateSearch { get; private set; }
        public CandidateIntake? LastDuplicateReviewIntake { get; private set; }
        public DateTimeOffset? LastDuplicateReviewAt { get; private set; }
        public CandidateIntake? LastConfirmIntake { get; private set; }
        public Candidate? LastConfirmCandidate { get; private set; }
        public long? LastExpectedCandidateVersion { get; private set; }
        public string? LastConfirmSource { get; private set; }
        public DateTimeOffset? LastConfirmNow { get; private set; }

        public Task<Requisition?> GetVisibleRequisitionAsync(long requisitionId, CoreHrActor actor, CancellationToken cancellationToken) =>
            Task.FromResult(Requisition?.Id == requisitionId && actor.DataScope.CoversDepartment(Requisition.DepartmentId) ? Requisition : null);

        public Task<CandidateIntake?> GetByIntakeIdAsync(Guid intakeId, CoreHrActor actor, CancellationToken cancellationToken) =>
            Task.FromResult(Intake?.IntakeId == intakeId ? Intake : null);

        public Task<IReadOnlyList<Candidate>> GetCandidatesAsync(IReadOnlyCollection<long> candidateIds, CancellationToken cancellationToken)
        {
            LastRequestedCandidateIds = candidateIds;
            return Task.FromResult<IReadOnlyList<Candidate>>(Candidates.Where(candidate => candidateIds.Contains(candidate.Id)).OrderBy(candidate => candidate.Id).ToList());
        }

        public Task<IReadOnlyList<Candidate>> FindDuplicatesAsync(string normalizedEmail, string? normalizedPhone, CancellationToken cancellationToken)
        {
            DuplicateSearches++;
            LastDuplicateSearch = (normalizedEmail, normalizedPhone);
            return Task.FromResult<IReadOnlyList<Candidate>>(Candidates
                .Where(candidate => candidate.NormalizedEmail == normalizedEmail || (normalizedPhone is not null && candidate.NormalizedPhone == normalizedPhone))
                .OrderBy(candidate => candidate.Id)
                .ToList());
        }

        public Task<bool> ApplicationExistsAsync(long candidateId, long requisitionId, CancellationToken cancellationToken)
        {
            ApplicationExistsChecks++;
            return Task.FromResult(ApplicationExists);
        }

        public Task<IntakeConfirmation?> GetConfirmationAsync(CandidateIntake intake, CancellationToken cancellationToken) =>
            Task.FromResult(Confirmation);

        public Task<CandidateIntake> InsertAsync(CandidateIntake intake, CoreHrActor actor, CancellationToken cancellationToken)
        {
            InsertCalls++;
            LastInserted = intake;
            LastActor = actor;
            if (InsertFails)
            {
                throw new InvalidOperationException("database unavailable");
            }

            return Task.FromResult(new CandidateIntake(
                500, intake.IntakeId, intake.RequisitionId, intake.CandidateId, intake.ObjectKey, intake.OriginalFileName,
                intake.ContentType, intake.SizeBytes, intake.Status, intake.MalwareScanStatus, intake.ParserStatus,
                intake.ParsedCandidate, intake.Confidence, intake.ParserVersion, intake.PrivacyNoticeVersion, intake.ConsentedAt,
                intake.DuplicateCandidateIds, intake.UploadedBy, intake.UploadedAt, intake.ConfirmedBy, intake.ConfirmedAt));
        }

        public Task SaveDuplicateReviewAsync(CandidateIntake intake, CoreHrActor actor, DateTimeOffset occurredAt, CancellationToken cancellationToken)
        {
            DuplicateReviewSaves++;
            LastDuplicateReviewIntake = intake;
            LastDuplicateReviewAt = occurredAt;
            return Task.CompletedTask;
        }

        public Task<IntakeConfirmation?> ConfirmAsync(CandidateIntake intake, Candidate candidate, long? expectedCandidateVersion, string source, DateTimeOffset now, CoreHrActor actor, CancellationToken cancellationToken)
        {
            ConfirmCalls++;
            LastConfirmIntake = intake;
            LastConfirmCandidate = candidate;
            LastExpectedCandidateVersion = expectedCandidateVersion;
            LastConfirmSource = source;
            LastConfirmNow = now;
            LastActor = actor;
            if (!ConfirmSucceeds)
            {
                return Task.FromResult<IntakeConfirmation?>(null);
            }

            var candidateId = candidate.Id == 0 ? 77 : candidate.Id;
            var application = new RecruitmentApplication(900, candidateId, intake.RequisitionId, intake.Id, ApplicationStage.SourcedApplied, null, source, now, 1, now);
            return Task.FromResult<IntakeConfirmation?>(new IntakeConfirmation(application, candidate));
        }
    }

    private sealed class FakeStorage : IDocumentStorage
    {
        public List<(string ObjectKey, string ContentType, long PositionAtStore, string Content)> Stored { get; } = [];
        public List<string> Deleted { get; } = [];

        public async Task StoreAsync(string objectKey, Stream content, string contentType, CancellationToken cancellationToken)
        {
            var position = content.Position;
            using var reader = new StreamReader(content, Encoding.UTF8, leaveOpen: true);
            var text = await reader.ReadToEndAsync(cancellationToken);
            Stored.Add((objectKey, contentType, position, text));
        }

        public Task<Uri> CreateSignedDownloadUrlAsync(string objectKey, string downloadFileName, DateTimeOffset expiresAt, CancellationToken cancellationToken) =>
            Task.FromResult(new Uri($"https://storage.test/{objectKey}"));

        public Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
        {
            Deleted.Add(objectKey);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeScanner(MalwareScanResult result) : IMalwareScanner
    {
        public int Calls { get; private set; }

        public async Task<MalwareScanResult> ScanAsync(Stream content, string fileName, CancellationToken cancellationToken)
        {
            Calls++;
            // Consume the stream like a real scanner would, so the service must rewind before parsing and storing.
            await content.CopyToAsync(Stream.Null, cancellationToken);
            return result;
        }
    }

    private sealed class FakeParser : IResumeParser
    {
        public ResumeParseResult Result { get; init; } = ResumeParseResult.Empty("fake/0");
        public int Calls { get; private set; }
        public long PositionAtParse { get; private set; }
        public string? LastContentType { get; private set; }

        public async Task<ResumeParseResult> ParseAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken)
        {
            Calls++;
            PositionAtParse = content.Position;
            LastContentType = contentType;
            await content.CopyToAsync(Stream.Null, cancellationToken);
            return Result;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
