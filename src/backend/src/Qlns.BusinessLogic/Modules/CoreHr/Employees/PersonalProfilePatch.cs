namespace Qlns.BusinessLogic.Modules.CoreHr.Employees;

/// <summary>
/// The fields an employee (or HR with profile.manage) may change through
/// <c>PATCH /employees/{id}/profile</c>. Every other employee field is a work field and is
/// read-only on this path; see <see cref="Employee"/>.
/// </summary>
public sealed record PersonalProfilePatch(
    Optional<string?> PersonalEmail,
    Optional<string?> Phone,
    Optional<string?> TemporaryAddress,
    Optional<IReadOnlyDictionary<string, string>?> EmergencyContact)
{
    public static PersonalProfilePatch Empty { get; } = new(
        Optional<string?>.Unset,
        Optional<string?>.Unset,
        Optional<string?>.Unset,
        Optional<IReadOnlyDictionary<string, string>?>.Unset);

    public bool IsEmpty =>
        !PersonalEmail.IsSet && !Phone.IsSet && !TemporaryAddress.IsSet && !EmergencyContact.IsSet;
}
