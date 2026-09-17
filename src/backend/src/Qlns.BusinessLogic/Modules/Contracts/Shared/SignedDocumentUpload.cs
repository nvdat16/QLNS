namespace Qlns.BusinessLogic.Modules.Contracts.Shared;

/// <summary>A signed PDF as received by the business layer; the caller owns <see cref="Content"/>.</summary>
public sealed record SignedDocumentUpload(
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream Content);
