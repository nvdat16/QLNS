using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.Employees;

/// <summary>
/// Employee master record (employees table). Personal contact fields can be patched here.
/// Work fields (Status, DepartmentId, PositionId, ManagerId, WorkEmail, EmployeeCode, HireDate)
/// deliberately have no mutators in this feature: they change only through approved Employee Events
/// (Modules/CoreHr/EmployeeEvents), per class_diagrams.md §1 and EMP-04.
/// </summary>
public sealed class Employee
{
    public const string PersonalEmailField = "personalEmail";
    public const string PhoneField = "phone";
    public const string TemporaryAddressField = "temporaryAddress";
    public const string EmergencyContactField = "emergencyContact";

    private const int MaxEmailLength = 320;
    private const int MaxPhoneLength = 30;
    private const int MaxTemporaryAddressLength = 1000;
    private const int MaxEmergencyContactEntries = 10;
    private const int MaxEmergencyContactKeyLength = 100;
    private const int MaxEmergencyContactValueLength = 255;

    public long Id { get; }
    public string EmployeeCode { get; }
    public long? SourceApplicationId { get; }
    public long? UserId { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public string? WorkEmail { get; }
    public string? PersonalEmail { get; private set; }
    public string? Phone { get; private set; }
    public DateOnly? DateOfBirth { get; }
    public string? Gender { get; }
    public string? OfficeLocation { get; }
    public string? PermanentAddress { get; }
    public string? TemporaryAddress { get; private set; }
    public IReadOnlyDictionary<string, string>? EmergencyContact { get; private set; }
    public long? ManagerId { get; }
    public long DepartmentId { get; }
    public long PositionId { get; }
    public DateOnly HireDate { get; }
    public EmployeeStatus Status { get; }
    public long Version { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Employee(
        long id,
        string employeeCode,
        long? sourceApplicationId,
        long? userId,
        string firstName,
        string lastName,
        string? workEmail,
        string? personalEmail,
        string? phone,
        DateOnly? dateOfBirth,
        string? gender,
        string? officeLocation,
        string? permanentAddress,
        string? temporaryAddress,
        IReadOnlyDictionary<string, string>? emergencyContact,
        long? managerId,
        long departmentId,
        long positionId,
        DateOnly hireDate,
        EmployeeStatus status,
        long version,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        if (id <= 0 || departmentId <= 0 || positionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Persistent identifiers must be positive.");
        }

        if (sourceApplicationId is <= 0 || userId is <= 0 || managerId is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(managerId), "Optional identifiers must be positive when present.");
        }

        if (version <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(version));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(employeeCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);

        Id = id;
        EmployeeCode = employeeCode;
        SourceApplicationId = sourceApplicationId;
        UserId = userId;
        FirstName = firstName;
        LastName = lastName;
        WorkEmail = workEmail;
        PersonalEmail = personalEmail;
        Phone = phone;
        DateOfBirth = dateOfBirth;
        Gender = gender;
        OfficeLocation = officeLocation;
        PermanentAddress = permanentAddress;
        TemporaryAddress = temporaryAddress;
        EmergencyContact = emergencyContact;
        ManagerId = managerId;
        DepartmentId = departmentId;
        PositionId = positionId;
        HireDate = hireDate;
        Status = status;
        Version = version;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    public string FullName() => $"{FirstName} {LastName}";

    /// <summary>
    /// Validates and applies a merge patch of personal fields. Returns the contract names of the
    /// fields whose value actually changed (for audit). When nothing changed the record is left
    /// untouched (no version bump) and the list is empty.
    /// </summary>
    public IReadOnlyList<string> ApplyPersonalProfilePatch(PersonalProfilePatch patch, DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(patch);

        if (patch.IsEmpty)
        {
            throw CoreHrValidationException.For("patch", "At least one field is required.");
        }

        var errors = new ValidationErrors();
        var personalEmail = patch.PersonalEmail.IsSet ? NormalizeEmail(patch.PersonalEmail.Value, errors) : PersonalEmail;
        var phone = patch.Phone.IsSet ? NormalizePhone(patch.Phone.Value, errors) : Phone;
        var temporaryAddress = patch.TemporaryAddress.IsSet
            ? NormalizeTemporaryAddress(patch.TemporaryAddress.Value, errors)
            : TemporaryAddress;
        var emergencyContact = patch.EmergencyContact.IsSet
            ? NormalizeEmergencyContact(patch.EmergencyContact.Value, errors)
            : EmergencyContact;
        errors.ThrowIfAny();

        var changed = new List<string>(4);
        if (patch.PersonalEmail.IsSet && !string.Equals(personalEmail, PersonalEmail, StringComparison.Ordinal))
        {
            PersonalEmail = personalEmail;
            changed.Add(PersonalEmailField);
        }

        if (patch.Phone.IsSet && !string.Equals(phone, Phone, StringComparison.Ordinal))
        {
            Phone = phone;
            changed.Add(PhoneField);
        }

        if (patch.TemporaryAddress.IsSet && !string.Equals(temporaryAddress, TemporaryAddress, StringComparison.Ordinal))
        {
            TemporaryAddress = temporaryAddress;
            changed.Add(TemporaryAddressField);
        }

        if (patch.EmergencyContact.IsSet && !DictionaryEquals(emergencyContact, EmergencyContact))
        {
            EmergencyContact = emergencyContact;
            changed.Add(EmergencyContactField);
        }

        if (changed.Count > 0)
        {
            Version++;
            UpdatedAt = now;
        }

        return changed;
    }

    private static string? NormalizeEmail(string? value, ValidationErrors errors)
    {
        if (value is null)
        {
            return null;
        }

        var email = value.Trim();
        if (email.Length == 0)
        {
            errors.Add(PersonalEmailField, "personalEmail must not be blank; send null to clear it.");
            return null;
        }

        if (email.Length > MaxEmailLength)
        {
            errors.Add(PersonalEmailField, $"personalEmail must be at most {MaxEmailLength} characters.");
            return null;
        }

        if (!LooksLikeEmail(email))
        {
            errors.Add(PersonalEmailField, "personalEmail must be a valid email address.");
            return null;
        }

        return email;
    }

    private static bool LooksLikeEmail(string email)
    {
        var at = email.IndexOf('@', StringComparison.Ordinal);
        if (at <= 0 || at != email.LastIndexOf('@') || at == email.Length - 1)
        {
            return false;
        }

        var domain = email[(at + 1)..];
        var dot = domain.IndexOf('.', StringComparison.Ordinal);
        return !email.Any(char.IsWhiteSpace) &&
            dot > 0 && dot < domain.Length - 1 &&
            !domain.StartsWith('.') && !domain.EndsWith('.') && !domain.Contains("..", StringComparison.Ordinal);
    }

    private static string? NormalizePhone(string? value, ValidationErrors errors)
    {
        if (value is null)
        {
            return null;
        }

        var phone = value.Trim();
        if (phone.Length == 0)
        {
            errors.Add(PhoneField, "phone must not be blank; send null to clear it.");
            return null;
        }

        if (phone.Length > MaxPhoneLength)
        {
            errors.Add(PhoneField, $"phone must be at most {MaxPhoneLength} characters.");
            return null;
        }

        if (!phone.All(c => char.IsAsciiDigit(c) || c is ' ' or '+' or '-' or '(' or ')') || !phone.Any(char.IsAsciiDigit))
        {
            errors.Add(PhoneField, "phone may contain only digits, spaces, '+', '-', '(' and ')' and must include a digit.");
            return null;
        }

        return phone;
    }

    private static string? NormalizeTemporaryAddress(string? value, ValidationErrors errors)
    {
        if (value is null)
        {
            return null;
        }

        var address = value.Trim();
        if (address.Length == 0)
        {
            errors.Add(TemporaryAddressField, "temporaryAddress must not be blank; send null to clear it.");
            return null;
        }

        if (address.Length > MaxTemporaryAddressLength)
        {
            errors.Add(TemporaryAddressField, $"temporaryAddress must be at most {MaxTemporaryAddressLength} characters.");
            return null;
        }

        return address;
    }

    private static IReadOnlyDictionary<string, string>? NormalizeEmergencyContact(
        IReadOnlyDictionary<string, string>? value,
        ValidationErrors errors)
    {
        if (value is null)
        {
            return null;
        }

        if (value.Count == 0)
        {
            errors.Add(EmergencyContactField, "emergencyContact must have at least one entry; send null to clear it.");
            return null;
        }

        if (value.Count > MaxEmergencyContactEntries)
        {
            errors.Add(EmergencyContactField, $"emergencyContact may have at most {MaxEmergencyContactEntries} entries.");
            return null;
        }

        var normalized = new Dictionary<string, string>(value.Count, StringComparer.Ordinal);
        foreach (var (rawKey, rawValue) in value)
        {
            var key = rawKey?.Trim() ?? string.Empty;
            var entry = rawValue?.Trim() ?? string.Empty;
            if (key.Length == 0 || key.Length > MaxEmergencyContactKeyLength)
            {
                errors.Add(EmergencyContactField, $"emergencyContact keys must be 1 to {MaxEmergencyContactKeyLength} characters.");
                continue;
            }

            if (entry.Length == 0 || entry.Length > MaxEmergencyContactValueLength)
            {
                errors.Add($"{EmergencyContactField}.{key}", $"emergencyContact values must be 1 to {MaxEmergencyContactValueLength} characters.");
                continue;
            }

            normalized[key] = entry;
        }

        return normalized;
    }

    private static bool DictionaryEquals(
        IReadOnlyDictionary<string, string>? left,
        IReadOnlyDictionary<string, string>? right)
    {
        if (ReferenceEquals(left, right))
        {
            return true;
        }

        if (left is null || right is null || left.Count != right.Count)
        {
            return false;
        }

        return left.All(pair => right.TryGetValue(pair.Key, out var other) &&
            string.Equals(pair.Value, other, StringComparison.Ordinal));
    }
}
