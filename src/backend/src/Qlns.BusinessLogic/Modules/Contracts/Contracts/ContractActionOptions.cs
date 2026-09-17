namespace Qlns.BusinessLogic.Modules.Contracts.Contracts;

/// <summary>
/// Optional body of the transition endpoint (OpenAPI ContractAction). <see cref="Reason"/> is mandatory for
/// terminate, cancel and the primary-overlap override; <see cref="SignedAt"/> is signature evidence for
/// activate; <see cref="AllowPrimaryOverlap"/> lets an approver end the current primary contract early.
/// </summary>
public sealed record ContractActionOptions(
    string? Reason,
    DateTimeOffset? SignedAt,
    bool AllowPrimaryOverlap)
{
    public static ContractActionOptions None { get; } = new(null, null, false);
}
