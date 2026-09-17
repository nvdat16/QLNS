namespace Qlns.BusinessLogic.Modules.Recruitment.Intake;

/// <summary>
/// Outcome of résumé parsing: the suggested candidate fields (null when nothing could be extracted), a 0..1 confidence
/// per extracted field and the parser version recorded on the intake. Parsed data is a suggestion the recruiter
/// reviews and may correct before confirmation (REC-02).
/// </summary>
public sealed record ResumeParseResult(
    CandidateInput? Parsed,
    IReadOnlyDictionary<string, double> Confidence,
    string ParserVersion)
{
    public static ResumeParseResult Empty(string parserVersion) =>
        new(null, new Dictionary<string, double>(), parserVersion);
}

/// <summary>Résumé parsing port (AI/OCR extraction). Runs synchronously inside the intake request in this delivery.</summary>
public interface IResumeParser
{
    Task<ResumeParseResult> ParseAsync(Stream content, string fileName, string contentType, CancellationToken cancellationToken);
}
