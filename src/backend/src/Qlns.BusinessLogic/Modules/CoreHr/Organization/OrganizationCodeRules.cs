using System.Text.RegularExpressions;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.Organization;

/// <summary>Field rules shared by departments and positions (code, name, free-text lengths).</summary>
internal static partial class OrganizationCodeRules
{
    public const int CodeMaxLength = 50;
    public const int NameMaxLength = 255;
    public const int DescriptionMaxLength = 5000;

    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9_-]*$")]
    private static partial Regex CodePattern();

    /// <summary>Trims and validates <paramref name="code"/>; returns the trimmed value (empty when invalid).</summary>
    public static string ValidateCode(ValidationErrors errors, string? code)
    {
        var trimmed = code?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            errors.Add("code", "code is required.");
        }
        else if (trimmed.Length > CodeMaxLength)
        {
            errors.Add("code", $"code must be at most {CodeMaxLength} characters.");
        }
        else if (!CodePattern().IsMatch(trimmed))
        {
            errors.Add("code", "code must start with a letter or digit and contain only letters, digits, '_' or '-'.");
        }

        return trimmed;
    }

    public static string ValidateName(ValidationErrors errors, string? name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            errors.Add("name", "name is required.");
        }
        else if (trimmed.Length > NameMaxLength)
        {
            errors.Add("name", $"name must be at most {NameMaxLength} characters.");
        }

        return trimmed;
    }

    /// <summary>Normalizes an optional text: whitespace-only becomes null; enforces the maximum length.</summary>
    public static string? ValidateOptional(ValidationErrors errors, string field, string? value, int maxLength)
    {
        var trimmed = value?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return null;
        }

        if (trimmed.Length > maxLength)
        {
            errors.Add(field, $"{field} must be at most {maxLength} characters.");
        }

        return trimmed;
    }
}
