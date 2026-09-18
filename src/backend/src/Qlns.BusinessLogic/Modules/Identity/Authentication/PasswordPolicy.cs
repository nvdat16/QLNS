using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Identity.Authentication;

/// <summary>
/// Password rules enforced whenever a secret is set: at sign-up by an administrator, on self-service
/// change and on administrative reset. Length is the primary control (NIST SP 800-63B); the class
/// requirement is kept deliberately mild and the rejection of the e-mail local part blocks the most
/// common guess. Verification never applies these rules — an existing weak password still signs in.
/// </summary>
public static class PasswordPolicy
{
    public const int MinLength = 10;

    /// <summary>Upper bound so that a very long input cannot turn PBKDF2 into a denial-of-service vector.</summary>
    public const int MaxLength = 128;

    public const int MinCharacterClasses = 3;

    public static void Validate(string? password, string field, string? email = null)
    {
        var errors = new ValidationErrors();

        if (string.IsNullOrEmpty(password))
        {
            errors.Add(field, "Password is required.");
            errors.ThrowIfAny();
            return;
        }

        if (password.Length < MinLength)
        {
            errors.Add(field, $"Password must be at least {MinLength} characters long.");
        }

        if (password.Length > MaxLength)
        {
            errors.Add(field, $"Password must be at most {MaxLength} characters long.");
        }

        if (password.Trim().Length != password.Length)
        {
            errors.Add(field, "Password must not start or end with whitespace.");
        }

        if (CharacterClasses(password) < MinCharacterClasses)
        {
            errors.Add(field, $"Password must combine at least {MinCharacterClasses} of: lower-case, upper-case, digit, symbol.");
        }

        var localPart = LocalPart(email);
        if (localPart is { Length: >= 3 } && password.Contains(localPart, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(field, "Password must not contain the e-mail account name.");
        }

        errors.ThrowIfAny();
    }

    private static int CharacterClasses(string password)
    {
        var lower = false;
        var upper = false;
        var digit = false;
        var symbol = false;

        foreach (var character in password)
        {
            if (char.IsLower(character))
            {
                lower = true;
            }
            else if (char.IsUpper(character))
            {
                upper = true;
            }
            else if (char.IsDigit(character))
            {
                digit = true;
            }
            else
            {
                symbol = true;
            }
        }

        return (lower ? 1 : 0) + (upper ? 1 : 0) + (digit ? 1 : 0) + (symbol ? 1 : 0);
    }

    private static string? LocalPart(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        var at = email.IndexOf('@', StringComparison.Ordinal);
        return at > 0 ? email[..at] : email;
    }
}
