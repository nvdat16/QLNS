using Qlns.BusinessLogic.Modules.Recruitment.Intake;

namespace Qlns.DataAccess.Modules.Recruitment.Intake;

/// <summary>
/// DEVELOPMENT-ONLY <see cref="IResumeParser"/> that extracts nothing: every intake starts with an empty
/// <c>parsedCandidate</c> and the recruiter types the candidate data at confirmation. It exists so the intake
/// pipeline runs locally; it MUST be replaced by the selected AI/OCR parsing adapter before any shared environment (REC-02).
/// </summary>
public sealed class DevelopmentOnlyResumeParser : IResumeParser
{
    public const string ParserVersion = "development-only/0.0";

    public Task<ResumeParseResult> ParseAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken) =>
        Task.FromResult(ResumeParseResult.Empty(ParserVersion));
}
