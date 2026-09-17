using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Contracts.Contracts;

/// <summary>Read model of one row of <c>GET /contracts/expiring</c>.</summary>
public sealed record ExpiringContract(Contract Contract, int DaysRemaining, ExpiryAlertLevel AlertLevel);

/// <summary>Page of expiring contracts together with the reference date the days were counted from.</summary>
public sealed record ExpiringContractsView(PagedResult<ExpiringContract> Contracts, DateOnly AsOf);

/// <summary>Outcome of one background expiry run: contracts moved to expired and ids that lost a version race.</summary>
public sealed record ExpireDueContractsResult(int Expired, IReadOnlyList<long> Conflicted);
