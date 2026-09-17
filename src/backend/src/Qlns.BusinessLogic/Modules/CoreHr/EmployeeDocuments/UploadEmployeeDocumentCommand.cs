using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.EmployeeDocuments;

/// <summary>Upload request as received by the business layer; the caller owns <see cref="Content"/>.</summary>
public sealed record UploadEmployeeDocumentCommand(
    long EmployeeId,
    string DocumentType,
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream Content,
    DateOnly? RetentionUntil,
    CoreHrActor Actor);
