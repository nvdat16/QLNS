using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Contracts.Addenda;

/// <summary>
/// The terms an addendum may change (CON-03) and their value rules. <c>positionId</c>, <c>departmentId</c> and
/// <c>salary</c> are master data: making the addendum effective raises an employee event for them.
/// </summary>
public static class AddendumTermRules
{
    public const string Salary = "salary";
    public const string Currency = "currency";
    public const string PositionId = "positionId";
    public const string DepartmentId = "departmentId";
    public const string Allowance = "allowance";
    public const string WorkLocation = "workLocation";
    public const string EndDate = "endDate";
    public const string NoticePeriodDays = "noticePeriodDays";
    public const string Other = "other";

    public static readonly IReadOnlySet<string> AllowedKeys = new HashSet<string>(StringComparer.Ordinal)
    {
        Salary, Currency, PositionId, DepartmentId, Allowance, WorkLocation, EndDate, NoticePeriodDays, Other
    };

    /// <summary>Keys mirrored into employee_events.before_data / after_data (EmployeeEvent.AllowedFields).</summary>
    public static readonly IReadOnlySet<string> MasterDataKeys = new HashSet<string>(StringComparer.Ordinal)
    {
        DepartmentId, PositionId, Salary
    };

    /// <summary>Validates <c>afterTerms</c>: non-empty, allow-listed keys, typed values for the keys the system interprets.</summary>
    public static void ValidateAfterTerms(JsonObject afterTerms, ValidationErrors errors)
    {
        ArgumentNullException.ThrowIfNull(afterTerms);
        ArgumentNullException.ThrowIfNull(errors);

        if (afterTerms.Count == 0)
        {
            errors.Add("afterTerms", "afterTerms must contain at least one changed term.");
            return;
        }

        foreach (var (key, value) in afterTerms)
        {
            if (!AllowedKeys.Contains(key))
            {
                errors.Add($"afterTerms.{key}", "Unknown term. Allowed: " + string.Join(", ", AllowedKeys) + ".");
                continue;
            }

            switch (key)
            {
                case PositionId or DepartmentId when !TryGetPositiveLong(value, out _):
                    errors.Add($"afterTerms.{key}", "Must be a positive integer identifier.");
                    break;
                case Salary when !(TryGetDecimal(value, out var salary) && salary >= 0):
                    errors.Add("afterTerms.salary", "Must be a number greater than or equal to zero.");
                    break;
                case NoticePeriodDays when !(TryGetInt(value, out var days) && days >= 0):
                    errors.Add("afterTerms.noticePeriodDays", "Must be an integer greater than or equal to zero.");
                    break;
                case Currency when !(TryGetString(value, out var currency) && currency.Length == 3 && currency.All(char.IsAsciiLetterUpper)):
                    errors.Add("afterTerms.currency", "Must be a three-letter ISO 4217 code in upper case.");
                    break;
                case EndDate when !(TryGetString(value, out var endDate) && DateOnly.TryParseExact(endDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _)):
                    errors.Add("afterTerms.endDate", "Must be a date in yyyy-MM-dd format.");
                    break;
            }
        }
    }

    /// <summary>Copy of <paramref name="terms"/> restricted to <see cref="MasterDataKeys"/>.</summary>
    public static JsonObject MasterDataSubset(JsonObject terms)
    {
        ArgumentNullException.ThrowIfNull(terms);

        var subset = new JsonObject();
        foreach (var (key, value) in terms)
        {
            if (MasterDataKeys.Contains(key))
            {
                subset[key] = value?.DeepClone();
            }
        }

        return subset;
    }

    private static bool TryGetPositiveLong(JsonNode? node, out long value)
    {
        value = 0;
        return node is JsonValue &&
            node.GetValueKind() == JsonValueKind.Number &&
            long.TryParse(node.ToJsonString(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value) &&
            value > 0;
    }

    private static bool TryGetInt(JsonNode? node, out int value)
    {
        value = 0;
        return node is JsonValue &&
            node.GetValueKind() == JsonValueKind.Number &&
            int.TryParse(node.ToJsonString(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryGetDecimal(JsonNode? node, out decimal value)
    {
        value = 0;
        return node is JsonValue &&
            node.GetValueKind() == JsonValueKind.Number &&
            decimal.TryParse(node.ToJsonString(), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
    }

    private static bool TryGetString(JsonNode? node, out string value)
    {
        value = string.Empty;
        if (node is JsonValue && node.GetValueKind() == JsonValueKind.String && node.AsValue().TryGetValue<string>(out var parsed))
        {
            value = parsed;
            return true;
        }

        return false;
    }
}
