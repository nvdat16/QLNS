namespace Qlns.BusinessLogic.Modules.CoreHr.EmployeeDocuments;

/// <summary>
/// Private object storage port. Objects are addressed by an opaque key that never leaves the backend;
/// clients only ever receive short-lived signed URLs.
/// </summary>
public interface IDocumentStorage
{
    Task StoreAsync(string objectKey, Stream content, string contentType, CancellationToken cancellationToken);

    Task<Uri> CreateSignedDownloadUrlAsync(
        string objectKey,
        string downloadFileName,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken);

    Task DeleteAsync(string objectKey, CancellationToken cancellationToken);
}
