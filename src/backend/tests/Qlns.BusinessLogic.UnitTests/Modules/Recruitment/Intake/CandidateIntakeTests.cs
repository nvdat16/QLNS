using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Intake;
using Xunit;

namespace Qlns.BusinessLogic.UnitTests.Modules.Recruitment.Intake;

public sealed class CandidateIntakeTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 17, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Start_AfterCleanScanAndParse_AwaitsConfirmationAndRecordsConsentNow()
    {
        var parse = new ResumeParseResult(
            new CandidateInput("An", null, "an@example.com", null, null, null),
            new Dictionary<string, double> { ["firstName"] = 0.9, ["email"] = 0.7 },
            "parser/1.2");

        var intake = CandidateIntake.Start(20, "resumes/20/abc", "cv.pdf", "application/pdf", 1234, parse, "  v3  ", 7, Now);

        Assert.Equal(0, intake.Id);
        Assert.NotEqual(Guid.Empty, intake.IntakeId);
        Assert.Equal(20, intake.RequisitionId);
        Assert.Null(intake.CandidateId);
        Assert.Equal("resumes/20/abc", intake.ObjectKey);
        Assert.Equal("cv.pdf", intake.OriginalFileName);
        Assert.Equal("application/pdf", intake.ContentType);
        Assert.Equal(1234, intake.SizeBytes);
        Assert.Equal(IntakeStatus.AwaitingConfirmation, intake.Status);
        Assert.Equal(CandidateIntake.ScanClean, intake.MalwareScanStatus);
        Assert.Equal(CandidateIntake.ParserCompleted, intake.ParserStatus);
        Assert.Same(parse.Parsed, intake.ParsedCandidate);
        Assert.Equal(0.9, intake.Confidence["firstName"]);
        Assert.Equal("parser/1.2", intake.ParserVersion);
        Assert.Equal("v3", intake.PrivacyNoticeVersion);
        Assert.Equal(Now, intake.ConsentedAt);
        Assert.Empty(intake.DuplicateCandidateIds);
        Assert.Equal(7, intake.UploadedBy);
        Assert.Equal(Now, intake.UploadedAt);
        Assert.Null(intake.ConfirmedBy);
        Assert.Null(intake.ConfirmedAt);
        Assert.True(intake.IsConfirmable);
    }

    [Fact]
    public void Start_TwoIntakes_GetDistinctIdentifiers()
    {
        var first = CandidateIntake.Start(20, "k1", "cv.pdf", "application/pdf", 1, ResumeParseResult.Empty("p"), "v1", 7, Now);
        var second = CandidateIntake.Start(20, "k2", "cv.pdf", "application/pdf", 1, ResumeParseResult.Empty("p"), "v1", 7, Now);

        Assert.NotEqual(first.IntakeId, second.IntakeId);
    }

    [Fact]
    public void BuildObjectKey_UsesRequisitionFolderAndRandomName()
    {
        var first = CandidateIntake.BuildObjectKey(20);
        var second = CandidateIntake.BuildObjectKey(20);

        Assert.StartsWith("resumes/20/", first, StringComparison.Ordinal);
        Assert.Equal(11 + 32, first.Length);
        Assert.NotEqual(first, second);
    }

    [Theory]
    [InlineData(IntakeStatus.AwaitingConfirmation)]
    [InlineData(IntakeStatus.DuplicateReview)]
    public void MarkDuplicateReview_FromConfirmableStatus_RecordsIds(IntakeStatus status)
    {
        var intake = Create(status);

        intake.MarkDuplicateReview([5, 9]);

        Assert.Equal(IntakeStatus.DuplicateReview, intake.Status);
        Assert.Equal([5L, 9L], intake.DuplicateCandidateIds);
        Assert.True(intake.IsConfirmable);
    }

    [Fact]
    public void MarkDuplicateReview_WithoutIds_ThrowsArgument()
    {
        var intake = Create(IntakeStatus.AwaitingConfirmation);

        Assert.Throws<ArgumentException>(() => intake.MarkDuplicateReview([]));
        Assert.Equal(IntakeStatus.AwaitingConfirmation, intake.Status);
    }

    [Theory]
    [InlineData(IntakeStatus.Completed)]
    [InlineData(IntakeStatus.Rejected)]
    [InlineData(IntakeStatus.Failed)]
    public void MarkDuplicateReview_FromNonConfirmableStatus_ThrowsNotConfirmable(IntakeStatus status)
    {
        var intake = Create(status);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => intake.MarkDuplicateReview([5]));

        Assert.Equal(CandidateIntake.NotConfirmableCode, exception.Code);
        Assert.Equal(status.ToContract(), exception.Details["currentStatus"]);
    }

    [Theory]
    [InlineData(IntakeStatus.AwaitingConfirmation)]
    [InlineData(IntakeStatus.DuplicateReview)]
    public void Complete_FromConfirmableStatus_LinksCandidateAndRecordsConfirmation(IntakeStatus status)
    {
        var intake = Create(status);

        intake.Complete(candidateId: 77, confirmedBy: 8, Now);

        Assert.Equal(IntakeStatus.Completed, intake.Status);
        Assert.Equal(CandidateIntake.ParserConfirmed, intake.ParserStatus);
        Assert.Equal(77, intake.CandidateId);
        Assert.Equal(8, intake.ConfirmedBy);
        Assert.Equal(Now, intake.ConfirmedAt);
        Assert.False(intake.IsConfirmable);
    }

    [Fact]
    public void Complete_Twice_ThrowsNotConfirmable()
    {
        var intake = Create(IntakeStatus.AwaitingConfirmation);
        intake.Complete(77, 8, Now);

        var exception = Assert.Throws<CoreHrBusinessRuleException>(() => intake.Complete(78, 8, Now));

        Assert.Equal(CandidateIntake.NotConfirmableCode, exception.Code);
        Assert.Equal("completed", exception.Details["currentStatus"]);
        Assert.Equal(77, intake.CandidateId);
    }

    [Fact]
    public void Complete_NonPositiveIdentifiers_ThrowsArgument()
    {
        var intake = Create(IntakeStatus.AwaitingConfirmation);

        Assert.Throws<ArgumentOutOfRangeException>(() => intake.Complete(0, 8, Now));
        Assert.Throws<ArgumentOutOfRangeException>(() => intake.Complete(77, 0, Now));
    }

    [Theory]
    [InlineData(IntakeStatus.Scanning, false)]
    [InlineData(IntakeStatus.Parsing, false)]
    [InlineData(IntakeStatus.AwaitingConfirmation, true)]
    [InlineData(IntakeStatus.DuplicateReview, true)]
    [InlineData(IntakeStatus.Completed, false)]
    [InlineData(IntakeStatus.Rejected, false)]
    [InlineData(IntakeStatus.Failed, false)]
    public void IsConfirmable_OnlyForAwaitingConfirmationAndDuplicateReview(IntakeStatus status, bool expected)
    {
        Assert.Equal(expected, Create(status).IsConfirmable);
    }

    [Fact]
    public void Constructor_InvalidArguments_Throw()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(id: -1));
        Assert.Throws<ArgumentException>(() => Build(intakeId: Guid.Empty));
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(requisitionId: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(uploadedBy: 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Build(sizeBytes: 0));
        Assert.Throws<ArgumentException>(() => Build(objectKey: " "));
        Assert.Throws<ArgumentException>(() => Build(privacyNoticeVersion: ""));
    }

    private static CandidateIntake Create(IntakeStatus status) => Build(status: status);

    private static CandidateIntake Build(
        long id = 5,
        Guid? intakeId = null,
        long requisitionId = 20,
        string objectKey = "resumes/20/abc",
        long sizeBytes = 100,
        IntakeStatus status = IntakeStatus.AwaitingConfirmation,
        string privacyNoticeVersion = "v1",
        long uploadedBy = 7) => new(
        id,
        intakeId ?? Guid.Parse("3f2504e0-4f89-41d3-9a0c-0305e82c3301"),
        requisitionId,
        candidateId: null,
        objectKey,
        "cv.pdf",
        "application/pdf",
        sizeBytes,
        status,
        CandidateIntake.ScanClean,
        CandidateIntake.ParserCompleted,
        parsedCandidate: null,
        new Dictionary<string, double>(),
        "parser/1.0",
        privacyNoticeVersion,
        Now.AddHours(-1),
        duplicateCandidateIds: [],
        uploadedBy,
        Now.AddHours(-1),
        confirmedBy: null,
        confirmedAt: null);
}
