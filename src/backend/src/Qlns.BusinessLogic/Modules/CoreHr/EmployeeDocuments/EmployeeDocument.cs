namespace Qlns.BusinessLogic.Modules.CoreHr.EmployeeDocuments;

/// <summary>
/// One stored version of a private employee document (table <c>employee_documents</c>).
/// Editing a document means creating a new version; rows are never overwritten.
/// <see cref="ObjectKey"/> is internal storage addressing and must never leave the backend.
/// </summary>
public sealed class EmployeeDocument
{
    public const int MaxFileNameLength = 255;

    public long Id { get; }
    public long EmployeeId { get; }
    public string DocumentType { get; }
    public int Version { get; }
    public string OriginalFileName { get; }
    public string ObjectKey { get; }
    public string ContentType { get; }
    public long SizeBytes { get; }
    public long UploadedBy { get; }
    public DateTimeOffset UploadedAt { get; }
    public DateOnly? RetentionUntil { get; }
    public DateTimeOffset? DeletedAt { get; }

    public EmployeeDocument(
        long id,
        long employeeId,
        string documentType,
        int version,
        string originalFileName,
        string objectKey,
        string contentType,
        long sizeBytes,
        long uploadedBy,
        DateTimeOffset uploadedAt,
        DateOnly? retentionUntil,
        DateTimeOffset? deletedAt)
    {
        if (id < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Identifier must be zero (transient) or positive.");
        }

        if (employeeId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(employeeId), "Employee identifier must be positive.");
        }

        if (version < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(version), "Version must be at least 1.");
        }

        if (sizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), "Size must be positive.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(documentType);
        ArgumentException.ThrowIfNullOrWhiteSpace(originalFileName);
        ArgumentException.ThrowIfNullOrWhiteSpace(objectKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(contentType);

        Id = id;
        EmployeeId = employeeId;
        DocumentType = documentType;
        Version = version;
        OriginalFileName = originalFileName;
        ObjectKey = objectKey;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        UploadedBy = uploadedBy;
        UploadedAt = uploadedAt;
        RetentionUntil = retentionUntil;
        DeletedAt = deletedAt;
    }

    public bool IsSoftDeleted => DeletedAt.HasValue;

    /// <summary>True when a retention date is set and lies strictly before <paramref name="today"/>.</summary>
    public bool IsRetentionExpired(DateOnly today) => RetentionUntil is { } until && until < today;

    /// <summary>Returns a copy carrying the identifier assigned by the database.</summary>
    public EmployeeDocument WithId(long id) => new(
        id,
        EmployeeId,
        DocumentType,
        Version,
        OriginalFileName,
        ObjectKey,
        ContentType,
        SizeBytes,
        UploadedBy,
        UploadedAt,
        RetentionUntil,
        DeletedAt);
}
