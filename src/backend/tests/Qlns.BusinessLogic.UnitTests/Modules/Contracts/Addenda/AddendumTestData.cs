using System.Text.Json.Nodes;
using Qlns.BusinessLogic.Modules.Contracts.Addenda;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.UnitTests.Modules.Contracts.Contracts;
using static Qlns.BusinessLogic.UnitTests.Modules.Contracts.Contracts.ContractTestData;

namespace Qlns.BusinessLogic.UnitTests.Modules.Contracts.Addenda;

/// <summary>Builders and the in-memory addendum repository shared by the Addenda tests.</summary>
internal static class AddendumTestData
{
    public const long ContractId = 42;
    public const long AddendumId = 7;

    public static JsonObject Terms(params (string Key, JsonNode? Value)[] pairs)
    {
        var terms = new JsonObject();
        foreach (var (key, value) in pairs)
        {
            terms[key] = value;
        }

        return terms;
    }

    public static ContractAddendum Addendum(
        long id = AddendumId,
        ContractAddendumStatus status = ContractAddendumStatus.Draft,
        JsonObject? afterTerms = null,
        JsonObject? beforeTerms = null,
        DateOnly? effectiveDate = null,
        string? documentObjectKey = null,
        DateTimeOffset? signedAt = null,
        long version = 1,
        long contractId = ContractId,
        string addendumNumber = "PL-2026-001") => new(
        id,
        contractId,
        addendumNumber,
        status,
        effectiveDate ?? new DateOnly(2027, 1, 1),
        beforeTerms ?? Terms(("salary", 25_000_000m)),
        afterTerms ?? Terms(("salary", 30_000_000m)),
        reason: "Annual review",
        documentObjectKey,
        createdBy: HrUserId,
        approvedBy: status is ContractAddendumStatus.Approved or ContractAddendumStatus.Effective or ContractAddendumStatus.Superseded ? 1300 : null,
        approvedAt: status is ContractAddendumStatus.Approved or ContractAddendumStatus.Effective or ContractAddendumStatus.Superseded ? Now.AddDays(-2) : null,
        signedAt,
        version,
        createdAt: Now.AddDays(-5),
        updatedAt: Now.AddDays(-1));

    public static ContractAddendumWrite AddendumWrite(
        string? addendumNumber = "PL-2026-001",
        DateOnly? effectiveDate = null,
        JsonObject? beforeTerms = null,
        JsonObject? afterTerms = null,
        string? reason = "Annual review",
        bool nullBefore = false,
        bool nullAfter = false) => new(
        addendumNumber,
        effectiveDate ?? new DateOnly(2027, 1, 1),
        nullBefore ? null : beforeTerms ?? Terms(("salary", 25_000_000m)),
        nullAfter ? null : afterTerms ?? Terms(("salary", 30_000_000m)),
        reason);

    internal sealed class FakeAddendumRepository : IContractAddendumRepository
    {
        private long _nextId = 500;

        public List<ContractAddendum> Addenda { get; } = [];
        public HashSet<string> TakenNumbers { get; } = new(StringComparer.Ordinal);
        public bool SaveSucceeds { get; set; } = true;
        public Exception? SaveFailure { get; set; }

        public List<(ContractAddendum Addendum, CoreHrActor Actor)> Inserted { get; } = [];
        public List<(long AddendumId, ContractAddendumStatus Previous, ContractAddendumStatus Current, long ExpectedVersion, string? Reason)> Transitions { get; } = [];
        public List<AddendumActivation> Activations { get; } = [];
        public List<(long AddendumId, long ExpectedVersion)> SignedDocuments { get; } = [];

        public ContractAddendum Add(ContractAddendum addendum)
        {
            Addenda.Add(addendum);
            return addendum;
        }

        public Task<IReadOnlyList<ContractAddendum>> ListByContractAsync(long contractId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<ContractAddendum>>(Addenda
                .Where(a => a.ContractId == contractId)
                .OrderBy(a => a.EffectiveDate)
                .ThenBy(a => a.Id)
                .ToList());

        public Task<ContractAddendum?> GetByIdAsync(long addendumId, CancellationToken cancellationToken) =>
            Task.FromResult(Addenda.SingleOrDefault(a => a.Id == addendumId));

        public Task<bool> AddendumNumberExistsAsync(string addendumNumber, CancellationToken cancellationToken) =>
            Task.FromResult(TakenNumbers.Contains(addendumNumber) || Addenda.Any(a => a.AddendumNumber == addendumNumber));

        public Task<ContractAddendum> InsertAsync(ContractAddendum addendum, CoreHrActor actor, CancellationToken cancellationToken)
        {
            var persisted = new ContractAddendum(
                _nextId++,
                addendum.ContractId,
                addendum.AddendumNumber,
                addendum.Status,
                addendum.EffectiveDate,
                addendum.BeforeTerms,
                addendum.AfterTerms,
                addendum.Reason,
                addendum.DocumentObjectKey,
                addendum.CreatedBy,
                addendum.ApprovedBy,
                addendum.ApprovedAt,
                addendum.SignedAt,
                addendum.Version,
                addendum.CreatedAt,
                addendum.UpdatedAt);
            Addenda.Add(persisted);
            Inserted.Add((persisted, actor));
            return Task.FromResult(persisted);
        }

        public Task<bool> SaveTransitionAsync(ContractAddendum addendum, ContractAddendumStatus previousStatus, long expectedVersion, string? reason, CoreHrActor actor, CancellationToken cancellationToken)
        {
            Transitions.Add((addendum.Id, previousStatus, addendum.Status, expectedVersion, reason));
            return Task.FromResult(SaveSucceeds);
        }

        public Task<bool> SaveEffectiveAsync(AddendumActivation activation, CoreHrActor actor, CancellationToken cancellationToken)
        {
            Activations.Add(activation);
            return Task.FromResult(SaveSucceeds);
        }

        public Task<bool> SaveSignedDocumentAsync(ContractAddendum addendum, long expectedVersion, CoreHrActor actor, CancellationToken cancellationToken)
        {
            if (SaveFailure is not null)
            {
                throw SaveFailure;
            }

            SignedDocuments.Add((addendum.Id, expectedVersion));
            return Task.FromResult(SaveSucceeds);
        }
    }
}
