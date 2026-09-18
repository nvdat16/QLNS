using System.Globalization;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Identity.Shared;

/// <summary>
/// Normalization and shape check of users.email. The column is UNIQUE, so two accounts that differ only
/// in letter case must not both exist: every read and write goes through <see cref="Normalize"/>.
/// The check is deliberately structural, not RFC-complete — deliverability is not our concern here.
/// </summary>
public static class EmailAddress
{
    public const int MaxLength = 320;

    /// <summary>Lower-cases and trims; returns an empty string for null or blank input.</summary>
    public static string Normalize(string? email) =>
        string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim().ToLower(CultureInfo.InvariantCulture);

    public static bool IsWellFormed(string normalizedEmail)
    {
        if (normalizedEmail.Length is 0 or > MaxLength)
        {
            return false;
        }

        var at = normalizedEmail.IndexOf('@', StringComparison.Ordinal);
        if (at <= 0 || at != normalizedEmail.LastIndexOf('@') || at == normalizedEmail.Length - 1)
        {
            return false;
        }

        var domain = normalizedEmail[(at + 1)..];
        return !normalizedEmail.Any(char.IsWhiteSpace) &&
            domain.Contains('.', StringComparison.Ordinal) &&
            domain[0] != '.' && domain[^1] != '.';
    }

    /// <summary>Normalizes and throws <see cref="CoreHrValidationException"/> (HTTP 422) when malformed.</summary>
    public static string Require(string? email, string field)
    {
        var normalized = Normalize(email);
        if (!IsWellFormed(normalized))
        {
            throw CoreHrValidationException.For(field, "A valid e-mail address is required.");
        }

        return normalized;
    }
}
