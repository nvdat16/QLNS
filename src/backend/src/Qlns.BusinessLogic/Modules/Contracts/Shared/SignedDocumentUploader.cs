using Qlns.BusinessLogic.Modules.CoreHr.EmployeeDocuments;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Contracts.Shared;

/// <summary>
/// Scan-then-store pipeline for signed PDFs, shared by contracts and addenda. Validates the upload (422),
/// refuses infected files (422) and an unreachable scanner (409 with the caller's code), rewinds the stream
/// and writes the object under the caller's key. Deleting the object again when the database write fails is
/// left to the caller through <see cref="TryDeleteAsync"/>.
/// </summary>
public sealed class SignedDocumentUploader(IDocumentStorage storage, IMalwareScanner scanner)
{
    public async Task<string> ScanAndStoreAsync(
        string objectKey,
        SignedDocumentUpload upload,
        string scanUnavailableCode,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);
        ArgumentNullException.ThrowIfNull(upload);

        SignedDocumentRules.Validate(upload.FileName, upload.ContentType, upload.SizeBytes);

        switch (await scanner.ScanAsync(upload.Content, upload.FileName, cancellationToken))
        {
            case MalwareScanResult.Infected:
                throw CoreHrValidationException.For("file", "The file failed the malware scan.");
            case MalwareScanResult.Unavailable:
                throw new CoreHrBusinessRuleException(
                    scanUnavailableCode,
                    "The malware scanner is unavailable; the document was not stored. Retry later.");
            case MalwareScanResult.Clean:
                break;
            default:
                throw new InvalidOperationException("Unknown malware scan result.");
        }

        if (upload.Content.CanSeek)
        {
            upload.Content.Position = 0;
        }

        await storage.StoreAsync(objectKey, upload.Content, SignedDocumentRules.PdfContentType, cancellationToken);
        return objectKey;
    }

    /// <summary>Best-effort removal of an object whose database row was never written.</summary>
    public async Task TryDeleteAsync(string objectKey)
    {
        try
        {
            await storage.DeleteAsync(objectKey, CancellationToken.None);
        }
        catch (Exception)
        {
            // The caller rethrows the original failure; storage housekeeping reconciles orphans later.
        }
    }
}
