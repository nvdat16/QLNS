namespace Qlns.BusinessLogic.Modules.CoreHr.Offboarding;

/// <summary>offboarding_cases.separation_type (ck_offboarding_separation). Contract values are snake_case.</summary>
public enum SeparationType
{
    Resignation,
    MutualAgreement,
    Dismissal,
    ContractExpiry,
    Retirement
}

public static class SeparationTypeNames
{
    public static string ToContract(this SeparationType type) => type switch
    {
        SeparationType.Resignation => "resignation",
        SeparationType.MutualAgreement => "mutual_agreement",
        SeparationType.Dismissal => "dismissal",
        SeparationType.ContractExpiry => "contract_expiry",
        SeparationType.Retirement => "retirement",
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public static bool TryParseContract(string? value, out SeparationType type)
    {
        type = value switch
        {
            "resignation" => SeparationType.Resignation,
            "mutual_agreement" => SeparationType.MutualAgreement,
            "dismissal" => SeparationType.Dismissal,
            "contract_expiry" => SeparationType.ContractExpiry,
            "retirement" => SeparationType.Retirement,
            _ => default
        };

        return value is "resignation" or "mutual_agreement" or "dismissal" or "contract_expiry" or "retirement";
    }
}
