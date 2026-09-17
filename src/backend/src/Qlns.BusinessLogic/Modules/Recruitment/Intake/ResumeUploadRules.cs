using Qlns.BusinessLogic.Modules.CoreHr.EmployeeDocuments;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Recruitment.Intake;

/// <summary>
/// Upload constraints of REC-02.1: PDF, DOC or DOCX up to 10 MiB, plus the privacy notice acknowledgement.
/// The presentation layer answers 413/415 early from the same constants; the service re-validates (422).
/// </summary>
public static class ResumeUploadRules
{
    public const long MaxSizeBytes = 10 * 1024 * 1024;
    public const int FileNameMaxLength = 255;

    public static readonly IReadOnlySet<string> AllowedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    public static bool IsAllowedContentType(string? contentType) =>
        DocumentUploadRules.NormalizeContentType(contentType) is { } normalized && AllowedContentTypes.Contains(normalized);

    /// <summary>Throws <see cref="CoreHrValidationException"/> with contract field names when any rule fails.</summary>
    public static void Validate(
        string? fileName,
        string? contentType,
        long sizeBytes,
        string? privacyNoticeVersion,
        bool consented)
    {
        var errors = new ValidationErrors();

        if (string.IsNullOrWhiteSpace(fileName))
        {
            errors.Add("file", "A file name is required.");
        }
        else if (fileName.Length > FileNameMaxLength)
        {
            errors.Add("file", $"File name must not exceed {FileNameMaxLength} characters.");
        }
        else if (!DocumentUploadRules.IsSafeFileName(fileName))
        {
            errors.Add("file", "File name must not contain path separators.");
        }

        if (!IsAllowedContentType(contentType))
        {
            errors.Add("file", $"Content type must be one of: {string.Join(", ", AllowedContentTypes)}.");
        }

        if (sizeBytes <= 0)
        {
            errors.Add("file", "The file is empty.");
        }
        else if (sizeBytes > MaxSizeBytes)
        {
            errors.Add("file", $"The file exceeds the maximum size of {MaxSizeBytes} bytes.");
        }

        var notice = privacyNoticeVersion?.Trim();
        if (string.IsNullOrEmpty(notice))
        {
            errors.Add("privacyNoticeVersion", "privacyNoticeVersion is required.");
        }
        else if (notice.Length > Candidate.PrivacyNoticeVersionMaxLength)
        {
            errors.Add("privacyNoticeVersion", $"privacyNoticeVersion must be at most {Candidate.PrivacyNoticeVersionMaxLength} characters.");
        }

        if (!consented)
        {
            errors.Add("consented", "The candidate must have consented to the privacy notice.");
        }

        errors.ThrowIfAny();
    }
}
