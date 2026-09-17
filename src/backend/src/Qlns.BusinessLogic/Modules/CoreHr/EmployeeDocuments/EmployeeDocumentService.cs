using System.Globalization;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.EmployeeDocuments;

/// <summary>
/// Use cases of EMP-05.1: list authorized document metadata, scan-and-store a new version and
/// issue short-lived signed download URLs. Data scope is enforced server-side: employees outside the
/// actor's scope are reported as not found; documents the actor may not read are omitted from lists
/// and a direct download request for one is refused and audited.
/// </summary>
public sealed class EmployeeDocumentService(
    IEmployeeDocumentRepository repository,
    IDocumentStorage storage,
    IMalwareScanner scanner,
    TimeProvider timeProvider)
{
    public static readonly TimeSpan SignedUrlLifetime = TimeSpan.FromMinutes(15);

    public const string UploadForbiddenCode = "corehr.document.upload_forbidden";
    public const string AccessForbiddenCode = "corehr.document.access_forbidden";
    public const string ScanUnavailableCode = "corehr.document.scan_unavailable";

    public async Task<IReadOnlyList<EmployeeDocument>> ListAsync(
        long employeeId,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        var departmentId = await RequireVisibleEmployeeAsync(employeeId, actor, cancellationToken);
        var documents = await repository.ListByEmployeeAsync(employeeId, cancellationToken);

        return documents
            .Where(document => !document.IsSoftDeleted)
            .Where(document => DocumentAccessPolicy.CanRead(actor, document, departmentId))
            .OrderBy(document => document.DocumentType, StringComparer.Ordinal)
            .ThenByDescending(document => document.Version)
            .ToList();
    }

    public async Task<EmployeeDocument> UploadAsync(
        UploadEmployeeDocumentCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);

        var departmentId = await RequireVisibleEmployeeAsync(command.EmployeeId, command.Actor, cancellationToken);

        if (!DocumentType.TryParse(command.DocumentType, out var type))
        {
            throw CoreHrValidationException.For("documentType", "Unknown document type.");
        }

        var now = timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        DocumentUploadRules.Validate(command.FileName, command.ContentType, command.SizeBytes, command.RetentionUntil, today);

        if (!DocumentAccessPolicy.CanUpload(command.Actor, type, command.EmployeeId, departmentId))
        {
            throw new CoreHrForbiddenException(
                UploadForbiddenCode,
                "You are not allowed to upload this document type for the employee.");
        }

        switch (await scanner.ScanAsync(command.Content, command.FileName, cancellationToken))
        {
            case MalwareScanResult.Infected:
                throw CoreHrValidationException.For("file", "The file failed the malware scan.");
            case MalwareScanResult.Unavailable:
                throw new CoreHrBusinessRuleException(
                    ScanUnavailableCode,
                    "The malware scanner is unavailable; the document was not stored. Retry later.");
            case MalwareScanResult.Clean:
                break;
            default:
                throw new InvalidOperationException("Unknown malware scan result.");
        }

        if (command.Content.CanSeek)
        {
            command.Content.Position = 0;
        }

        var version = await repository.GetLatestVersionAsync(command.EmployeeId, type.Code, cancellationToken) + 1;
        var objectKey = BuildObjectKey(command.EmployeeId, type.Code, version);
        var contentType = DocumentUploadRules.NormalizeContentType(command.ContentType)!;

        var document = new EmployeeDocument(
            id: 0,
            command.EmployeeId,
            type.Code,
            version,
            command.FileName.Trim(),
            objectKey,
            contentType,
            command.SizeBytes,
            command.Actor.UserId,
            now,
            command.RetentionUntil,
            deletedAt: null);

        await storage.StoreAsync(objectKey, command.Content, contentType, cancellationToken);

        try
        {
            return await repository.InsertAsync(document, command.Actor, cancellationToken);
        }
        catch
        {
            await TryDeleteStoredObjectAsync(objectKey);
            throw;
        }
    }

    public async Task<SignedDownload> CreateDownloadUrlAsync(
        long documentId,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        var found = await repository.GetByIdAsync(documentId, cancellationToken);
        if (found is not { } entry ||
            entry.Document.IsSoftDeleted ||
            !actor.CanAccessEmployee(entry.Document.EmployeeId, entry.EmployeeDepartmentId))
        {
            throw new CoreHrNotFoundException("Employee document", documentId);
        }

        var now = timeProvider.GetUtcNow();
        if (!DocumentAccessPolicy.CanRead(actor, entry.Document, entry.EmployeeDepartmentId))
        {
            await repository.RecordAccessDeniedAsync(entry.Document, actor, now, cancellationToken);
            throw new CoreHrForbiddenException(
                AccessForbiddenCode,
                "You are not allowed to access this document.");
        }

        var expiresAt = now + SignedUrlLifetime;
        var url = await storage.CreateSignedDownloadUrlAsync(
            entry.Document.ObjectKey,
            entry.Document.OriginalFileName,
            expiresAt,
            cancellationToken);

        await repository.RecordAccessGrantedAsync(entry.Document, actor, now, expiresAt, cancellationToken);
        return new SignedDownload(url, expiresAt);
    }

    private async Task<long> RequireVisibleEmployeeAsync(long employeeId, CoreHrActor actor, CancellationToken cancellationToken)
    {
        var departmentId = await repository.GetEmployeeDepartmentAsync(employeeId, cancellationToken);
        if (departmentId is not { } visibleDepartment || !actor.CanAccessEmployee(employeeId, visibleDepartment))
        {
            throw new CoreHrNotFoundException("Employee", employeeId);
        }

        return visibleDepartment;
    }

    private static string BuildObjectKey(long employeeId, string documentType, int version) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"employees/{employeeId}/{documentType}/v{version}/{Guid.NewGuid():N}");

    private async Task TryDeleteStoredObjectAsync(string objectKey)
    {
        try
        {
            await storage.DeleteAsync(objectKey, CancellationToken.None);
        }
        catch (Exception)
        {
            // Best effort only: the caller rethrows the original insert failure and storage housekeeping
            // reconciles any orphaned object later.
        }
    }
}
