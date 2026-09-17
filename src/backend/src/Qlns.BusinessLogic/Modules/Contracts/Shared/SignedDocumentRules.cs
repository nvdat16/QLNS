using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Contracts.Shared;

/// <summary>
/// Upload constraints shared by signed contracts and signed addenda (CON-01.2, CON-03.1): PDF only,
/// at most 10 MiB, safe file name. The presentation layer answers 413/415 early from the same constants;
/// the services re-validate and answer 422.
/// </summary>
public static class SignedDocumentRules
{
    public const long MaxSizeBytes = 10 * 1024 * 1024;
    public const int MaxFileNameLength = 255;
    public const string PdfContentType = "application/pdf";

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

    public static bool IsPdf(string? contentType) =>
        string.Equals(NormalizeContentType(contentType), PdfContentType, StringComparison.Ordinal);

    public static bool IsSafeFileName(string? fileName) =>
        !string.IsNullOrWhiteSpace(fileName) &&
        fileName.Length <= MaxFileNameLength &&
        fileName.IndexOfAny(['/', '\\']) < 0 &&
        fileName.Trim() is not ("." or "..");

    /// <summary>Throws <see cref="CoreHrValidationException"/> keyed on <c>file</c> when any rule fails.</summary>
    public static void Validate(string? fileName, string? contentType, long sizeBytes)
    {
        var errors = new ValidationErrors();

        if (!IsSafeFileName(fileName))
        {
            errors.Add("file", $"A file name of at most {MaxFileNameLength} characters without path separators is required.");
        }

        if (!IsPdf(contentType))
        {
            errors.Add("file", $"Signed documents must be {PdfContentType}.");
        }

        if (sizeBytes <= 0)
        {
            errors.Add("file", "The file is empty.");
        }
        else if (sizeBytes > MaxSizeBytes)
        {
            errors.Add("file", $"The file exceeds the maximum size of {MaxSizeBytes} bytes.");
        }

        errors.ThrowIfAny();
    }
}
