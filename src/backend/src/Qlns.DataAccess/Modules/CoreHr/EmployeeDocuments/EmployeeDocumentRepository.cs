using Microsoft.EntityFrameworkCore;
using Qlns.BusinessLogic.Modules.CoreHr.EmployeeDocuments;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.DataAccess.Modules.CoreHr.Shared;
using Qlns.DataAccess.Modules.Recruitment.Applications;

namespace Qlns.DataAccess.Modules.CoreHr.EmployeeDocuments;

/// <summary>
/// PostgreSQL persistence for employee documents. Writes the document row and its audit row in one
/// transaction. Audit payloads carry metadata only: never the object key, never file content.
/// </summary>
public sealed class EmployeeDocumentRepository(QlnsDbContext dbContext) : IEmployeeDocumentRepository
{
    private const string EntityType = "employee_document";
    private const string UploadAction = "corehr.document.upload";
    private const string DownloadUrlAction = "corehr.document.download_url";

    private DbSet<EmployeeDocumentEntity> Documents => dbContext.Set<EmployeeDocumentEntity>();
    private DbSet<EmployeeEntity> Employees => dbContext.Set<EmployeeEntity>();
    private DbSet<AuditLogEntity> AuditLogs => dbContext.Set<AuditLogEntity>();

    public Task<long?> GetEmployeeDepartmentAsync(long employeeId, CancellationToken cancellationToken) =>
        Employees.AsNoTracking()
            .Where(x => x.Id == employeeId)
            .Select(x => (long?)x.DepartmentId)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<EmployeeDocument>> ListByEmployeeAsync(
        long employeeId,
        CancellationToken cancellationToken)
    {
        var entities = await Documents.AsNoTracking()
            .Where(x => x.EmployeeId == employeeId && x.DeletedAt == null)
            .OrderBy(x => x.DocumentType)
            .ThenByDescending(x => x.Version)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain).ToList();
    }

    public async Task<(EmployeeDocument Document, long EmployeeDepartmentId)?> GetByIdAsync(
        long documentId,
        CancellationToken cancellationToken)
    {
        var row = await (
            from document in Documents.AsNoTracking()
            join employee in Employees.AsNoTracking() on document.EmployeeId equals employee.Id
            where document.Id == documentId
            select new { Document = document, employee.DepartmentId })
            .SingleOrDefaultAsync(cancellationToken);

        return row is null ? null : (ToDomain(row.Document), row.DepartmentId);
    }

    public async Task<int> GetLatestVersionAsync(
        long employeeId,
        string documentType,
        CancellationToken cancellationToken)
    {
        // Soft-deleted rows keep their version: ux_employee_document_version covers them too.
        var latest = await Documents.AsNoTracking()
            .Where(x => x.EmployeeId == employeeId && x.DocumentType == documentType)
            .MaxAsync(x => (int?)x.Version, cancellationToken);

        return latest ?? 0;
    }

    public async Task<EmployeeDocument> InsertAsync(
        EmployeeDocument document,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(actor);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var entity = new EmployeeDocumentEntity
        {
            EmployeeId = document.EmployeeId,
            DocumentType = document.DocumentType,
            Version = document.Version,
            OriginalFileName = document.OriginalFileName,
            ObjectKey = document.ObjectKey,
            ContentType = document.ContentType,
            SizeBytes = document.SizeBytes,
            UploadedBy = document.UploadedBy,
            UploadedAt = document.UploadedAt,
            RetentionUntil = document.RetentionUntil,
            DeletedAt = null
        };

        Documents.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            UploadAction,
            EntityType,
            entity.Id,
            before: null,
            after: new
            {
                employeeId = document.EmployeeId,
                documentType = document.DocumentType,
                version = document.Version,
                originalFileName = document.OriginalFileName,
                contentType = document.ContentType,
                sizeBytes = document.SizeBytes,
                retentionUntil = document.RetentionUntil
            },
            document.UploadedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ToDomain(entity);
    }

    public async Task RecordAccessGrantedAsync(
        EmployeeDocument document,
        CoreHrActor actor,
        DateTimeOffset occurredAt,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            DownloadUrlAction,
            EntityType,
            document.Id,
            before: null,
            after: new
            {
                employeeId = document.EmployeeId,
                documentType = document.DocumentType,
                version = document.Version,
                expiresAt
            },
            occurredAt));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordAccessDeniedAsync(
        EmployeeDocument document,
        CoreHrActor actor,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(document);

        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            DownloadUrlAction,
            EntityType,
            document.Id,
            before: null,
            after: new
            {
                employeeId = document.EmployeeId,
                documentType = document.DocumentType,
                version = document.Version
            },
            occurredAt,
            result: "rejected"));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static EmployeeDocument ToDomain(EmployeeDocumentEntity entity) => new(
        entity.Id,
        entity.EmployeeId,
        entity.DocumentType,
        entity.Version,
        entity.OriginalFileName,
        entity.ObjectKey,
        entity.ContentType,
        entity.SizeBytes,
        entity.UploadedBy,
        entity.UploadedAt,
        entity.RetentionUntil,
        entity.DeletedAt);
}
