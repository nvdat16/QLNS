using System.ComponentModel.DataAnnotations;
using Qlns.Api.Modules.Recruitment.Requisitions;
using Qlns.BusinessLogic.Modules.Recruitment.Applications;
using Qlns.BusinessLogic.Modules.Recruitment.Intake;

namespace Qlns.Api.Modules.Recruitment.Intake;

/// <summary>OpenAPI <c>ResumeUpload</c> (multipart/form-data). <c>consented</c> must be true (enforced by the service, 422).</summary>
public sealed record ResumeUploadRequest(
    IFormFile? File,
    long RequisitionId,
    [Required, MaxLength(Candidate.PrivacyNoticeVersionMaxLength)] string PrivacyNoticeVersion,
    bool Consented);

/// <summary>OpenAPI <c>CandidateInput</c> as a request body. Format rules (e-mail, URLs, phone digits) are validated by the domain (422).</summary>
public sealed record CandidateInputRequest(
    [Required, MaxLength(Candidate.NameMaxLength)] string FirstName,
    [Required, MaxLength(Candidate.NameMaxLength)] string LastName,
    [Required, MaxLength(Candidate.EmailMaxLength)] string Email,
    [MaxLength(Candidate.PhoneMaxLength)] string? Phone,
    [MaxLength(Candidate.UrlMaxLength)] string? LinkedinUrl,
    [MaxLength(Candidate.UrlMaxLength)] string? PortfolioUrl)
{
    public CandidateInput ToInput() => new(FirstName, LastName, Email, Phone, LinkedinUrl, PortfolioUrl);
}

/// <summary>OpenAPI <c>ConfirmIntake</c>.</summary>
public sealed record ConfirmIntakeRequest(
    [Required] CandidateInputRequest Candidate,
    long? ExistingCandidateId,
    [MaxLength(RecruitmentApplication.SourceMaxLength)] string? Source);

/// <summary>OpenAPI <c>CandidateInput</c> as the parser suggestion; fields the parser could not extract are null.</summary>
public sealed record CandidateInputResponse(
    string? FirstName,
    string? LastName,
    string? Email,
    string? Phone,
    string? LinkedinUrl,
    string? PortfolioUrl)
{
    public static CandidateInputResponse From(CandidateInput input) => new(
        input.FirstName,
        input.LastName,
        input.Email,
        input.Phone,
        input.LinkedinUrl,
        input.PortfolioUrl);
}

/// <summary>OpenAPI <c>CandidateSummary</c>.</summary>
public sealed record CandidateSummaryResponse(
    long Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? LinkedinUrl,
    string? PortfolioUrl)
{
    public static CandidateSummaryResponse From(CandidateSummary summary) => new(
        summary.Id,
        summary.FirstName,
        summary.LastName,
        summary.Email,
        summary.Phone,
        summary.LinkedinUrl,
        summary.PortfolioUrl);

    public static CandidateSummaryResponse From(Candidate candidate) => From(CandidateSummary.From(candidate));
}

/// <summary>OpenAPI <c>CandidateIntake</c>. Deliberately excludes the internal object key.</summary>
public sealed record CandidateIntakeResponse(
    Guid Id,
    long RequisitionId,
    string Status,
    CandidateInputResponse? ParsedCandidate,
    IReadOnlyDictionary<string, double> Confidence,
    IReadOnlyList<CandidateSummaryResponse> DuplicateCandidates,
    DateTimeOffset UploadedAt)
{
    public static CandidateIntakeResponse From(CandidateIntakeView view) => new(
        view.Intake.IntakeId,
        view.Intake.RequisitionId,
        view.Intake.Status.ToContract(),
        view.Intake.ParsedCandidate is { } parsed ? CandidateInputResponse.From(parsed) : null,
        view.Intake.Confidence,
        view.DuplicateCandidates.Select(CandidateSummaryResponse.From).ToList(),
        view.Intake.UploadedAt);
}

/// <summary>OpenAPI <c>RecruitmentApplicationDetail</c>: the application plus its candidate and requisition.</summary>
public sealed record RecruitmentApplicationDetailResponse(
    long Id,
    long CandidateId,
    long JobPostingId,
    long? ResumeId,
    string Stage,
    decimal? AiScore,
    string Source,
    DateTimeOffset AppliedAt,
    long Version,
    DateTimeOffset UpdatedAt,
    CandidateSummaryResponse Candidate,
    RequisitionResponse Requisition)
{
    public static RecruitmentApplicationDetailResponse From(ConfirmedApplication confirmed) => new(
        confirmed.Application.Id,
        confirmed.Application.CandidateId,
        confirmed.Application.JobPostingId,
        confirmed.Application.ResumeId,
        confirmed.Application.Stage.ToContract(),
        confirmed.Application.AiScore,
        confirmed.Application.Source,
        confirmed.Application.AppliedAt,
        confirmed.Application.Version,
        confirmed.Application.UpdatedAt,
        CandidateSummaryResponse.From(confirmed.Candidate),
        RequisitionResponse.From(confirmed.Requisition));
}
