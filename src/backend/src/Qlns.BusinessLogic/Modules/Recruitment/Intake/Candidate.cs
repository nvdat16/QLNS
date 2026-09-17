using System.Net.Mail;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Recruitment.Intake;

/// <summary>
/// Candidate master record (REC-02, table candidates). Created or linked only when a recruiter confirms an intake;
/// duplicate detection works on <see cref="NormalizedEmail"/> and <see cref="NormalizedPhone"/>. Privacy notice
/// version and consent instant come from the résumé upload.
/// </summary>
public sealed class Candidate
{
    public const int NameMaxLength = 100;
    public const int EmailMaxLength = 320;
    public const int PhoneMaxLength = 30;
    public const int UrlMaxLength = 2048;
    public const int PrivacyNoticeVersionMaxLength = 50;

    public long Id { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public string Email { get; }
    public string NormalizedEmail { get; }
    public string? Phone { get; private set; }
    public string? NormalizedPhone { get; private set; }
    public string? LinkedinUrl { get; private set; }
    public string? PortfolioUrl { get; private set; }
    public string PrivacyNoticeVersion { get; }
    public DateTimeOffset ConsentedAt { get; }
    public long Version { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Candidate(
        long id,
        string firstName,
        string lastName,
        string email,
        string normalizedEmail,
        string? phone,
        string? normalizedPhone,
        string? linkedinUrl,
        string? portfolioUrl,
        string privacyNoticeVersion,
        DateTimeOffset consentedAt,
        long version,
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        if (id < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Identifier must be zero (transient) or positive.");
        }

        if (version < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(version), "Version must be at least 1.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(email);
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedEmail);
        ArgumentException.ThrowIfNullOrWhiteSpace(privacyNoticeVersion);

        Id = id;
        FirstName = firstName;
        LastName = lastName;
        Email = email;
        NormalizedEmail = normalizedEmail;
        Phone = phone;
        NormalizedPhone = normalizedPhone;
        LinkedinUrl = linkedinUrl;
        PortfolioUrl = portfolioUrl;
        PrivacyNoticeVersion = privacyNoticeVersion;
        ConsentedAt = consentedAt;
        Version = version;
        CreatedAt = createdAt;
        UpdatedAt = updatedAt;
    }

    /// <summary>Validates the input (422, contract field names under <c>candidate.</c>) and returns a transient candidate.</summary>
    public static Candidate Create(
        CandidateInput input,
        string privacyNoticeVersion,
        DateTimeOffset consentedAt,
        DateTimeOffset now)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(privacyNoticeVersion);
        var fields = ValidatedFields.From(input);

        return new Candidate(
            id: 0,
            fields.FirstName,
            fields.LastName,
            fields.Email,
            CandidateNormalization.NormalizeEmail(fields.Email),
            fields.Phone,
            CandidateNormalization.NormalizePhone(fields.Phone),
            fields.LinkedinUrl,
            fields.PortfolioUrl,
            privacyNoticeVersion,
            consentedAt,
            version: 1,
            createdAt: now,
            updatedAt: now);
    }

    /// <summary>
    /// Linking a new application to an existing candidate refreshes the contact channels the recruiter just confirmed
    /// (phone and URLs). Name and e-mail identify the record and are never overwritten by a merge (REC-02.2).
    /// Returns the contract names of the fields whose value changed.
    /// </summary>
    public IReadOnlyList<string> Refresh(CandidateInput input, DateTimeOffset now)
    {
        var fields = ValidatedFields.From(input);
        var changed = new List<string>();

        if (!string.Equals(Phone, fields.Phone, StringComparison.Ordinal))
        {
            Phone = fields.Phone;
            NormalizedPhone = CandidateNormalization.NormalizePhone(fields.Phone);
            changed.Add("phone");
        }

        if (!string.Equals(LinkedinUrl, fields.LinkedinUrl, StringComparison.Ordinal))
        {
            LinkedinUrl = fields.LinkedinUrl;
            changed.Add("linkedinUrl");
        }

        if (!string.Equals(PortfolioUrl, fields.PortfolioUrl, StringComparison.Ordinal))
        {
            PortfolioUrl = fields.PortfolioUrl;
            changed.Add("portfolioUrl");
        }

        Version++;
        UpdatedAt = now;
        return changed;
    }

    private sealed record ValidatedFields(
        string FirstName,
        string LastName,
        string Email,
        string? Phone,
        string? LinkedinUrl,
        string? PortfolioUrl)
    {
        public static ValidatedFields From(CandidateInput input)
        {
            ArgumentNullException.ThrowIfNull(input);
            var errors = new ValidationErrors();

            var firstName = RequireName(errors, "firstName", input.FirstName);
            var lastName = RequireName(errors, "lastName", input.LastName);

            var email = input.Email?.Trim();
            if (string.IsNullOrEmpty(email))
            {
                errors.Add("candidate.email", "email is required.");
            }
            else if (email.Length > EmailMaxLength)
            {
                errors.Add("candidate.email", $"email must be at most {EmailMaxLength} characters.");
            }
            else if (!IsValidEmail(email))
            {
                errors.Add("candidate.email", "email must be a valid e-mail address.");
            }

            var phone = Normalize(input.Phone);
            if (phone is { Length: > PhoneMaxLength })
            {
                errors.Add("candidate.phone", $"phone must be at most {PhoneMaxLength} characters.");
            }
            else if (phone is not null && CandidateNormalization.NormalizePhone(phone) is null)
            {
                errors.Add("candidate.phone", "phone must contain at least one digit.");
            }

            var linkedinUrl = RequireUrl(errors, "linkedinUrl", input.LinkedinUrl);
            var portfolioUrl = RequireUrl(errors, "portfolioUrl", input.PortfolioUrl);

            errors.ThrowIfAny();
            return new ValidatedFields(firstName!, lastName!, email!, phone, linkedinUrl, portfolioUrl);
        }

        /// <summary>Contract path of a CandidateInput property inside ConfirmIntake (<c>candidate.&lt;name&gt;</c>).</summary>
        private static string Field(string name) => "candidate." + name;

        private static string? RequireName(ValidationErrors errors, string name, string? value)
        {
            var trimmed = value?.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                errors.Add(Field(name), $"{name} is required.");
            }
            else if (trimmed.Length > NameMaxLength)
            {
                errors.Add(Field(name), $"{name} must be at most {NameMaxLength} characters.");
            }

            return trimmed;
        }

        private static string? RequireUrl(ValidationErrors errors, string name, string? value)
        {
            var url = Normalize(value);
            if (url is null)
            {
                return null;
            }

            if (url.Length > UrlMaxLength)
            {
                errors.Add(Field(name), $"{name} must be at most {UrlMaxLength} characters.");
            }
            else if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                errors.Add(Field(name), $"{name} must be an absolute http(s) URL.");
            }

            return url;
        }

        private static bool IsValidEmail(string email) =>
            MailAddress.TryCreate(email, out var address) &&
            string.Equals(address.Address, email, StringComparison.Ordinal) &&
            email.IndexOf('@', StringComparison.Ordinal) > 0;

        private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
