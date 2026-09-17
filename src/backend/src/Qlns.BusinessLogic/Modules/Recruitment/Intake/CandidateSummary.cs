namespace Qlns.BusinessLogic.Modules.Recruitment.Intake;

/// <summary>Persisted candidate as exposed to recruiters (OpenAPI CandidateSummary).</summary>
public sealed record CandidateSummary(
    long Id,
    string FirstName,
    string LastName,
    string Email,
    string? Phone,
    string? LinkedinUrl,
    string? PortfolioUrl)
{
    public static CandidateSummary From(Candidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return new CandidateSummary(
            candidate.Id,
            candidate.FirstName,
            candidate.LastName,
            candidate.Email,
            candidate.Phone,
            candidate.LinkedinUrl,
            candidate.PortfolioUrl);
    }
}

/// <summary>Minimal identity of a possible duplicate, returned in the 409 <c>duplicateCandidates</c> detail (data minimisation).</summary>
public sealed record DuplicateCandidate(long Id, string FirstName, string LastName, string Email)
{
    public static DuplicateCandidate From(Candidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);
        return new DuplicateCandidate(candidate.Id, candidate.FirstName, candidate.LastName, candidate.Email);
    }
}
