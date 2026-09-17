namespace Qlns.BusinessLogic.Modules.CoreHr.EmployeeDocuments;

/// <summary>Short-lived, authorization-checked download link (OpenAPI <c>SignedDownload</c>).</summary>
public sealed record SignedDownload(Uri Url, DateTimeOffset ExpiresAt);
