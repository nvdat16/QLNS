namespace Qlns.BusinessLogic.Modules.CoreHr.EmployeeDocuments;

/// <summary>Sensitivity class that drives who may read or upload a document type (EMP-05).</summary>
public enum DocumentSensitivity
{
    /// <summary>Visible to anyone whose data scope covers the employee.</summary>
    Standard,

    /// <summary>Identity papers and contractual documents: the employee themself or HR with the sensitive permission.</summary>
    Personal,

    /// <summary>HR-only records (for example disciplinary records): never visible to the employee.</summary>
    HrOnly
}

/// <summary>A document type of the allowlist together with its sensitivity class.</summary>
public sealed record DocumentTypeInfo(string Code, DocumentSensitivity Sensitivity)
{
    public bool IsStandard => Sensitivity == DocumentSensitivity.Standard;
}

/// <summary>
/// Static catalogue of accepted <c>document_type</c> values. The column is a free varchar in the
/// schema; the code owns the allowlist so that access policy can be derived from the type.
/// </summary>
public static class DocumentType
{
    public const string Degree = "degree";
    public const string Certificate = "certificate";
    public const string Resume = "resume";
    public const string Photo = "photo";
    public const string Other = "other";

    public const string NationalId = "national_id";
    public const string Nda = "nda";
    public const string Contract = "contract";
    public const string TaxForm = "tax_form";
    public const string BankAccount = "bank_account";

    public const string DisciplinaryRecord = "disciplinary_record";

    private static readonly Dictionary<string, DocumentTypeInfo> Catalogue = new[]
    {
        new DocumentTypeInfo(Degree, DocumentSensitivity.Standard),
        new DocumentTypeInfo(Certificate, DocumentSensitivity.Standard),
        new DocumentTypeInfo(Resume, DocumentSensitivity.Standard),
        new DocumentTypeInfo(Photo, DocumentSensitivity.Standard),
        new DocumentTypeInfo(Other, DocumentSensitivity.Standard),
        new DocumentTypeInfo(NationalId, DocumentSensitivity.Personal),
        new DocumentTypeInfo(Nda, DocumentSensitivity.Personal),
        new DocumentTypeInfo(Contract, DocumentSensitivity.Personal),
        new DocumentTypeInfo(TaxForm, DocumentSensitivity.Personal),
        new DocumentTypeInfo(BankAccount, DocumentSensitivity.Personal),
        new DocumentTypeInfo(DisciplinaryRecord, DocumentSensitivity.HrOnly)
    }.ToDictionary(info => info.Code, StringComparer.Ordinal);

    /// <summary>All accepted contract values, in catalogue order.</summary>
    public static IReadOnlyCollection<DocumentTypeInfo> All => Catalogue.Values;

    /// <summary>Resolves a contract value (case-sensitive, trimmed) to its catalogue entry.</summary>
    public static bool TryParse(string? value, out DocumentTypeInfo info)
    {
        info = null!;
        return value is not null && Catalogue.TryGetValue(value.Trim(), out info!);
    }
}
