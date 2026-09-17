namespace Qlns.BusinessLogic.Modules.CoreHr.Offboarding;

/// <summary>
/// offboarding_cases.final_settlement_status (ck_offboarding_settlement). Payroll owns the transitions of this
/// value (out of scope); offboarding only reads it to decide whether a case may be completed.
/// </summary>
public enum FinalSettlementStatus
{
    Pending,
    Calculated,
    Paid,
    Waived
}

public static class FinalSettlementStatusNames
{
    public static string ToContract(this FinalSettlementStatus status) => status switch
    {
        FinalSettlementStatus.Pending => "pending",
        FinalSettlementStatus.Calculated => "calculated",
        FinalSettlementStatus.Paid => "paid",
        FinalSettlementStatus.Waived => "waived",
        _ => throw new ArgumentOutOfRangeException(nameof(status))
    };

    public static bool TryParseContract(string? value, out FinalSettlementStatus status)
    {
        status = value switch
        {
            "pending" => FinalSettlementStatus.Pending,
            "calculated" => FinalSettlementStatus.Calculated,
            "paid" => FinalSettlementStatus.Paid,
            "waived" => FinalSettlementStatus.Waived,
            _ => default
        };

        return value is "pending" or "calculated" or "paid" or "waived";
    }

    /// <summary>True when the final settlement no longer blocks closing the case.</summary>
    public static bool IsSettled(this FinalSettlementStatus status) =>
        status is FinalSettlementStatus.Paid or FinalSettlementStatus.Waived;
}
