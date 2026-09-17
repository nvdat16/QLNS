using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Recruitment.Intake;

/// <summary>Multipart upload of <c>POST /recruitment/resumes</c> as received by the business layer; the caller owns <see cref="Content"/>.</summary>
public sealed record StartIntakeCommand(
    long RequisitionId,
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream Content,
    string? PrivacyNoticeVersion,
    bool Consented,
    CoreHrActor Actor);

/// <summary>Body of <c>POST /recruitment/intakes/{intakeId}/confirm</c> (OpenAPI ConfirmIntake).</summary>
public sealed record ConfirmIntakeCommand(
    Guid IntakeId,
    CandidateInput Candidate,
    long? ExistingCandidateId,
    string? Source,
    CoreHrActor Actor);
