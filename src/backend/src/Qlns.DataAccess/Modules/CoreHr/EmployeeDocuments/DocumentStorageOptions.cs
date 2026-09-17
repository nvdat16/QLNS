namespace Qlns.DataAccess.Modules.CoreHr.EmployeeDocuments;

/// <summary>
/// Configuration section <c>Documents</c> for the development document adapters.
/// <c>SigningKey</c> is required and must be a long random secret; it is never logged.
/// </summary>
public sealed class DocumentStorageOptions
{
    public const string SectionName = "Documents";
    public const string DefaultStorageFolderName = ".qlns-documents";
    public const string DefaultPublicBaseUrl = "http://localhost:5000";

    /// <summary>Absolute directory under which objects are stored (<c>Documents:StorageRoot</c>).</summary>
    public required string StorageRoot { get; init; }

    /// <summary>Base URL of this API as reachable by clients (<c>Documents:PublicBaseUrl</c>), no trailing slash.</summary>
    public required string PublicBaseUrl { get; init; }

    /// <summary>HMAC secret used to sign download URLs (<c>Documents:SigningKey</c>).</summary>
    public required string SigningKey { get; init; }
}
