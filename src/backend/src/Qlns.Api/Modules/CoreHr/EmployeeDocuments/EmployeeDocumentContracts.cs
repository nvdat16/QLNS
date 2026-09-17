using System.ComponentModel.DataAnnotations;
using Qlns.BusinessLogic.Modules.CoreHr.EmployeeDocuments;

namespace Qlns.Api.Modules.CoreHr.EmployeeDocuments;

/// <summary>OpenAPI <c>EmployeeDocumentUpload</c> (multipart/form-data).</summary>
public sealed record EmployeeDocumentUploadRequest(
    IFormFile? File,
    [Required, MaxLength(80)] string DocumentType,
    DateOnly? RetentionUntil);

/// <summary>OpenAPI <c>EmployeeDocument</c>. Deliberately excludes the internal object key.</summary>
public sealed record EmployeeDocumentResponse(
    long Id,
    long EmployeeId,
    string DocumentType,
    int Version,
    string OriginalFileName,
    string ContentType,
    long SizeBytes,
    DateOnly? RetentionUntil,
    DateTimeOffset UploadedAt)
{
    public static EmployeeDocumentResponse From(EmployeeDocument document) => new(
        document.Id,
        document.EmployeeId,
        document.DocumentType,
        document.Version,
        document.OriginalFileName,
        document.ContentType,
        document.SizeBytes,
        document.RetentionUntil,
        document.UploadedAt);
}

/// <summary>OpenAPI <c>SignedDownload</c>.</summary>
public sealed record SignedDownloadResponse(Uri Url, DateTimeOffset ExpiresAt);
