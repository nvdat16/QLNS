using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.EmployeeDocuments;

/// <summary>
/// Upload constraints of EMP-05: allowed media types, size ceiling, safe file name and retention date.
/// The presentation layer answers 413/415 early from the same constants; the service re-validates (422).
/// </summary>
public static class DocumentUploadRules
{
    public const long MaxSizeBytes = 10 * 1024 * 1024;

    public static readonly IReadOnlySet<string> AllowedContentTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "application/pdf",
        "image/jpeg",
        "image/png"
    };

    /// <summary>Lower-cased media type without parameters, or null when blank.</summary>
    public static string? NormalizeContentType(string? contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
        {
            return null;
        }

        var separator = contentType.IndexOf(';', StringComparison.Ordinal);
        var mediaType = separator >= 0 ? contentType[..separator] : contentType;
        return mediaType.Trim().ToLowerInvariant();
    }

    public static bool IsAllowedContentType(string? contentType) =>
        NormalizeContentType(contentType) is { } normalized && AllowedContentTypes.Contains(normalized);

    public static bool IsSafeFileName(string? fileName) =>
        !string.IsNullOrWhiteSpace(fileName) &&
        fileName.Length <= EmployeeDocument.MaxFileNameLength &&
        fileName.IndexOfAny(['/', '\\']) < 0 &&
        fileName.Trim() is not ("." or "..");

    /// <summary>Throws <see cref="CoreHrValidationException"/> with contract field names when any rule fails.</summary>
    public static void Validate(
        string? fileName,
        string? contentType,
        long sizeBytes,
        DateOnly? retentionUntil,
        DateOnly today)
    {
        var errors = new ValidationErrors();

        if (string.IsNullOrWhiteSpace(fileName))
        {
            errors.Add("file", "A file name is required.");
        }
        else if (fileName.Length > EmployeeDocument.MaxFileNameLength)
        {
            errors.Add("file", $"File name must not exceed {EmployeeDocument.MaxFileNameLength} characters.");
        }
        else if (!IsSafeFileName(fileName))
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

        if (retentionUntil is { } until && until < today)
        {
            errors.Add("retentionUntil", "retentionUntil must not be in the past.");
        }

        errors.ThrowIfAny();
    }
}
