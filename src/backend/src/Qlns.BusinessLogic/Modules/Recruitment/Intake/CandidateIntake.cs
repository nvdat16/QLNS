using System.Globalization;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Recruitment.Intake;

/// <summary>
/// Résumé intake (REC-02, table resumes): the stored file, its scan/parse outcome, the privacy acknowledgement and
/// the duplicate-review state, identified publicly by <see cref="IntakeId"/>. A candidate exists only after
/// <see cref="Complete"/>. In this delivery scanning and parsing run inside the upload request, so a persisted intake
/// is never in <c>scanning</c> or <c>parsing</c>.
/// </summary>
public sealed class CandidateIntake
{
    public const string ScanClean = "clean";
    public const string ParserCompleted = "completed";
    public const string ParserConfirmed = "confirmed";

    public const string RequisitionNotOpenCode = "recruitment.intake.requisition_not_open";
    public const string ScanUnavailableCode = "recruitment.intake.scan_unavailable";
    public const string NotConfirmableCode = "recruitment.intake.not_confirmable";
    public const string DuplicateReviewCode = "recruitment.intake.duplicate_review";

    public long Id { get; }
    public Guid IntakeId { get; }
    public long RequisitionId { get; }
    public long? CandidateId { get; private set; }
    public string ObjectKey { get; }
    public string OriginalFileName { get; }
    public string ContentType { get; }
    public long SizeBytes { get; }
    public IntakeStatus Status { get; private set; }
    public string MalwareScanStatus { get; }
    public string ParserStatus { get; private set; }
    public CandidateInput? ParsedCandidate { get; }
    public IReadOnlyDictionary<string, double> Confidence { get; }
    public string? ParserVersion { get; }
    public string PrivacyNoticeVersion { get; }
    public DateTimeOffset ConsentedAt { get; }
    public IReadOnlyList<long> DuplicateCandidateIds { get; private set; }
    public long UploadedBy { get; }
    public DateTimeOffset UploadedAt { get; }
    public long? ConfirmedBy { get; private set; }
    public DateTimeOffset? ConfirmedAt { get; private set; }

    public CandidateIntake(
        long id,
        Guid intakeId,
        long requisitionId,
        long? candidateId,
        string objectKey,
        string originalFileName,
        string contentType,
        long sizeBytes,
        IntakeStatus status,
        string malwareScanStatus,
        string parserStatus,
        CandidateInput? parsedCandidate,
        IReadOnlyDictionary<string, double> confidence,
        string? parserVersion,
        string privacyNoticeVersion,
        DateTimeOffset consentedAt,
        IReadOnlyList<long> duplicateCandidateIds,
        long uploadedBy,
        DateTimeOffset uploadedAt,
        long? confirmedBy,
        DateTimeOffset? confirmedAt)
    {
        if (id < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Identifier must be zero (transient) or positive.");
        }

        if (intakeId == Guid.Empty)
        {
            throw new ArgumentException("intakeId must not be empty.", nameof(intakeId));
        }

        if (requisitionId <= 0 || uploadedBy <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requisitionId), "Persistent identifiers must be positive.");
        }

        if (candidateId is <= 0 || confirmedBy is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(candidateId), "Persistent identifiers must be positive.");
        }

        if (sizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Size must be positive.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(malwareScanStatus);
        ArgumentException.ThrowIfNullOrWhiteSpace(parserStatus);
        ArgumentException.ThrowIfNullOrWhiteSpace(privacyNoticeVersion);
        ArgumentNullException.ThrowIfNull(confidence);
        ArgumentNullException.ThrowIfNull(duplicateCandidateIds);

        Id = id;
        IntakeId = intakeId;
        RequisitionId = requisitionId;
        CandidateId = candidateId;
        ObjectKey = objectKey;
        OriginalFileName = originalFileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        Status = status;
        MalwareScanStatus = malwareScanStatus;
        ParserStatus = parserStatus;
        ParsedCandidate = parsedCandidate;
        Confidence = confidence;
        ParserVersion = parserVersion;
        PrivacyNoticeVersion = privacyNoticeVersion;
        ConsentedAt = consentedAt;
        DuplicateCandidateIds = duplicateCandidateIds;
        UploadedBy = uploadedBy;
        UploadedAt = uploadedAt;
        ConfirmedBy = confirmedBy;
        ConfirmedAt = confirmedAt;
    }

    /// <summary>
    /// Transient intake for a file that passed the malware scan and was parsed; awaits recruiter confirmation.
    /// Consent is recorded at upload time (<paramref name="now"/>).
    /// </summary>
    public static CandidateIntake Start(
        long requisitionId,
        string objectKey,
        string originalFileName,
        string contentType,
        long sizeBytes,
        ResumeParseResult parse,
        string privacyNoticeVersion,
        long uploadedBy,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(parse);

        return new CandidateIntake(
            id: 0,
            Guid.CreateVersion7(now),
            requisitionId,
            candidateId: null,
            objectKey,
            originalFileName,
            contentType,
            sizeBytes,
            IntakeStatus.AwaitingConfirmation,
            ScanClean,
            ParserCompleted,
            parse.Parsed,
            parse.Confidence,
            parse.ParserVersion,
            privacyNoticeVersion.Trim(),
            consentedAt: now,
            duplicateCandidateIds: [],
            uploadedBy,
            uploadedAt: now,
            confirmedBy: null,
            confirmedAt: null);
    }

    /// <summary>Private object key: <c>resumes/{requisitionId}/{guid}</c>; never leaves the backend.</summary>
    public static string BuildObjectKey(long requisitionId) =>
        string.Create(CultureInfo.InvariantCulture, $"resumes/{requisitionId}/{Guid.NewGuid():N}");

    public bool IsConfirmable => Status is IntakeStatus.AwaitingConfirmation or IntakeStatus.DuplicateReview;

    /// <summary>Records the candidates that share the confirmed e-mail or phone; the recruiter must resolve them explicitly.</summary>
    public void MarkDuplicateReview(IReadOnlyList<long> duplicateCandidateIds)
    {
        ArgumentNullException.ThrowIfNull(duplicateCandidateIds);
        if (duplicateCandidateIds.Count == 0)
        {
            throw new ArgumentException("At least one duplicate candidate is required.", nameof(duplicateCandidateIds));
        }

        RequireConfirmable("mark for duplicate review");
        Status = IntakeStatus.DuplicateReview;
        DuplicateCandidateIds = duplicateCandidateIds;
    }

    /// <summary>awaiting_confirmation | duplicate_review → completed, linked to the created or chosen candidate.</summary>
    public void Complete(long candidateId, long confirmedBy, DateTimeOffset now)
    {
        if (candidateId <= 0 || confirmedBy <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(candidateId), "Persistent identifiers must be positive.");
        }

        RequireConfirmable("confirm");
        Status = IntakeStatus.Completed;
        ParserStatus = ParserConfirmed;
        CandidateId = candidateId;
        ConfirmedBy = confirmedBy;
        ConfirmedAt = now;
    }

    private void RequireConfirmable(string verb)
    {
        if (!IsConfirmable)
        {
            throw new CoreHrBusinessRuleException(
                NotConfirmableCode,
                $"Cannot {verb} an intake in status {Status.ToContract()}; only awaiting_confirmation or duplicate_review intakes can be confirmed.")
            {
                Details = new Dictionary<string, object?> { ["currentStatus"] = Status.ToContract() }
            };
        }
    }
}
