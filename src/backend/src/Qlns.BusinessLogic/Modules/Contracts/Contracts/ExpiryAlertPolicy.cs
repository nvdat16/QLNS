namespace Qlns.BusinessLogic.Modules.Contracts.Contracts;

/// <summary>Badge colour of an expiring contract (OpenAPI ExpiringContract.alertLevel).</summary>
public enum ExpiryAlertLevel
{
    Amber,
    Red
}

public static class ExpiryAlertLevelNames
{
    public static string ToContract(this ExpiryAlertLevel level) => level switch
    {
        ExpiryAlertLevel.Amber => "amber",
        ExpiryAlertLevel.Red => "red",
        _ => throw new ArgumentOutOfRangeException(nameof(level))
    };
}

/// <summary>Latest end date at which a contract of <see cref="Type"/> is still reported by the expiry query.</summary>
public sealed record ExpiryAlertWindow(ContractType Type, DateOnly LatestEndDate);

/// <summary>
/// CON-02 alert thresholds, in days before the end date. Probation contracts alert at 15 (amber) and 7 (red)
/// days so the probation review can be completed; every other dated contract alerts at 45 and 30 days.
/// The API (<c>GET /contracts/expiring</c>) and the daily background scan share this policy.
/// </summary>
public static class ExpiryAlertPolicy
{
    public const int ProbationRedDays = 7;
    public const int ProbationAmberDays = 15;
    public const int StandardRedDays = 30;
    public const int StandardAmberDays = 45;

    public const int DefaultWithinDays = StandardAmberDays;
    public const int MaxWithinDays = 365;

    public static int AmberThresholdDays(ContractType type) =>
        type == ContractType.Probation ? ProbationAmberDays : StandardAmberDays;

    public static int RedThresholdDays(ContractType type) =>
        type == ContractType.Probation ? ProbationRedDays : StandardRedDays;

    /// <summary>
    /// Alert level for a contract with <paramref name="daysRemaining"/> days left, or null when the contract
    /// has not reached the amber threshold of its type (or has already ended).
    /// </summary>
    public static ExpiryAlertLevel? Evaluate(ContractType type, int daysRemaining)
    {
        if (daysRemaining < 0 || daysRemaining > AmberThresholdDays(type))
        {
            return null;
        }

        return daysRemaining <= RedThresholdDays(type) ? ExpiryAlertLevel.Red : ExpiryAlertLevel.Amber;
    }

    /// <summary>
    /// Per-type upper bound of the end dates to report for a scan at <paramref name="asOf"/> looking at most
    /// <paramref name="withinDays"/> ahead: the nearer of the caller's horizon and the type's amber threshold.
    /// Indefinite contracts have no end date and are not listed.
    /// </summary>
    public static IReadOnlyList<ExpiryAlertWindow> Windows(DateOnly asOf, int withinDays)
    {
        if (withinDays < 1 || withinDays > MaxWithinDays)
        {
            throw new ArgumentOutOfRangeException(nameof(withinDays), $"withinDays must be between 1 and {MaxWithinDays}.");
        }

        return Enum.GetValues<ContractType>()
            .Where(type => type.HasFixedTerm())
            .Select(type => new ExpiryAlertWindow(type, asOf.AddDays(Math.Min(withinDays, AmberThresholdDays(type)))))
            .ToList();
    }
}
