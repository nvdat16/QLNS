namespace Qlns.BusinessLogic.Modules.Contracts.Contracts;

/// <summary>contracts.contract_type (OpenAPI ContractType). Vietnamese Labour Code contract kinds, snake_case in the contract.</summary>
public enum ContractType
{
    Probation,
    FixedTerm,
    Indefinite,
    Internship,
    ServiceContract
}

public static class ContractTypeNames
{
    /// <summary>Every type except <see cref="ContractType.Indefinite"/> carries a mandatory end date.</summary>
    public static bool HasFixedTerm(this ContractType type) => type != ContractType.Indefinite;

    public static string ToContract(this ContractType type) => type switch
    {
        ContractType.Probation => "probation",
        ContractType.FixedTerm => "fixed_term",
        ContractType.Indefinite => "indefinite",
        ContractType.Internship => "internship",
        ContractType.ServiceContract => "service_contract",
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public static bool TryParseContract(string? value, out ContractType type)
    {
        type = value switch
        {
            "probation" => ContractType.Probation,
            "fixed_term" => ContractType.FixedTerm,
            "indefinite" => ContractType.Indefinite,
            "internship" => ContractType.Internship,
            "service_contract" => ContractType.ServiceContract,
            _ => default
        };

        return value is "probation" or "fixed_term" or "indefinite" or "internship" or "service_contract";
    }
}
