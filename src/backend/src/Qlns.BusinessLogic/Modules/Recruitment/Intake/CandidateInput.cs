namespace Qlns.BusinessLogic.Modules.Recruitment.Intake;

/// <summary>Client-supplied or parser-suggested candidate fields (OpenAPI CandidateInput). Validated by <see cref="Candidate"/>.</summary>
public sealed record CandidateInput(
    string? FirstName,
    string? LastName,
    string? Email,
    string? Phone,
    string? LinkedinUrl,
    string? PortfolioUrl);
