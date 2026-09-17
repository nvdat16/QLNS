using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.EmployeeDocuments;

/// <summary>
/// Document-level access policy (EMP-05): data scope first, then sensitivity class of the document type.
/// Endpoint permissions (<see cref="CoreHrPermissions.DocumentRead"/>, <see cref="CoreHrPermissions.DocumentUpload"/>)
/// are enforced by the presentation layer; this policy decides per document.
/// </summary>
public static class DocumentAccessPolicy
{
    /// <summary>
    /// True when <paramref name="actor"/> may see the metadata of and download <paramref name="document"/>.
    /// Document types outside the catalogue fail closed and require the sensitive permission.
    /// </summary>
    public static bool CanRead(CoreHrActor actor, EmployeeDocument document, long employeeDepartmentId)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(document);

        if (!actor.CanAccessEmployee(document.EmployeeId, employeeDepartmentId))
        {
            return false;
        }

        var readSensitive = actor.HasPermission(CoreHrPermissions.DocumentReadSensitive);
        if (!DocumentType.TryParse(document.DocumentType, out var type))
        {
            return readSensitive;
        }

        return type.Sensitivity switch
        {
            DocumentSensitivity.Standard => true,
            DocumentSensitivity.Personal => actor.IsSelf(document.EmployeeId) || readSensitive,
            DocumentSensitivity.HrOnly => readSensitive,
            _ => false
        };
    }

    /// <summary>
    /// True when <paramref name="actor"/> may upload a document of <paramref name="type"/> for the employee.
    /// Standard documents may be uploaded by anyone in scope (including the employee themself);
    /// personal identity papers and HR-only records require the sensitive permission.
    /// </summary>
    public static bool CanUpload(CoreHrActor actor, DocumentTypeInfo type, long employeeId, long employeeDepartmentId)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(type);

        return actor.CanAccessEmployee(employeeId, employeeDepartmentId) &&
            (type.IsStandard || actor.HasPermission(CoreHrPermissions.DocumentReadSensitive));
    }
}
