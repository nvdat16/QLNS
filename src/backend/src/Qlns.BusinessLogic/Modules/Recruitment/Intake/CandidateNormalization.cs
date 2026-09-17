namespace Qlns.BusinessLogic.Modules.Recruitment.Intake;

/// <summary>
/// Identity normalisation used for duplicate detection (REC-02.2): e-mail is trimmed and lower-cased; phone keeps
/// digits only, with a Vietnamese national prefix <c>0</c> replaced by the country code <c>84</c>.
/// </summary>
public static class CandidateNormalization
{
    public const string DefaultCountryCode = "84";

    public static string NormalizeEmail(string email)
    {
        ArgumentNullException.ThrowIfNull(email);
        return email.Trim().ToLowerInvariant();
    }

    /// <summary>Digits only; leading <c>0</c> becomes <see cref="DefaultCountryCode"/>. Null when no digits remain.</summary>
    public static string? NormalizePhone(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
        {
            return null;
        }

        var digits = new string(phone.Where(char.IsAsciiDigit).ToArray());
        if (digits.Length == 0)
        {
            return null;
        }

        return digits[0] == '0' ? DefaultCountryCode + digits[1..] : digits;
    }
}
