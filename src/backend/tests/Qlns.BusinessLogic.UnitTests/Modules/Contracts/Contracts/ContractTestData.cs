using System.Text;
using Qlns.BusinessLogic.Modules.Contracts.Contracts;
using Qlns.BusinessLogic.Modules.Contracts.Shared;
using Qlns.BusinessLogic.Modules.CoreHr.EmployeeDocuments;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.UnitTests.Modules.Contracts.Contracts;

/// <summary>Builders, actors and hand-written fakes shared by the Contracts module tests.</summary>
internal static class ContractTestData
{
    public static readonly DateTimeOffset Now = new(2026, 9, 17, 8, 0, 0, TimeSpan.Zero);
    public static readonly DateOnly Today = new(2026, 9, 17);

    public const long EmployeeId = 10;
    public const long DepartmentId = 3;
    public const long OtherDepartmentId = 42;
    public const long ManagerUserId = 55;
    public const long HrUserId = 1200;

    public static Contract Build(
        long id = 42,
        ContractStatus status = ContractStatus.Draft,
        ContractType type = ContractType.FixedTerm,
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        bool isPrimary = true,
        string? documentObjectKey = null,
        DateTimeOffset? signedAt = null,
        long version = 1,
        long employeeId = EmployeeId,
        string contractNumber = "HD-2026-001",
        bool indefinite = false) => new(
        id,
        employeeId,
        contractNumber,
        type,
        startDate ?? new DateOnly(2026, 10, 1),
        indefinite ? null : endDate ?? new DateOnly(2027, 9, 30),
        salary: 25_000_000m,
        currency: "VND",
        noticePeriodDays: 30,
        status,
        isPrimary,
        documentObjectKey,
        signedAt,
        version,
        createdAt: Now.AddDays(-10),
        updatedAt: Now.AddDays(-1));

    public static ContractWrite Write(
        long employeeId = EmployeeId,
        string? contractNumber = "HD-2026-001",
        string? contractType = "fixed_term",
        DateOnly? startDate = null,
        DateOnly? endDate = null,
        decimal salary = 25_000_000m,
        string? currency = "VND",
        int? noticePeriodDays = 30,
        bool isPrimary = true,
        bool noEndDate = false) => new(
        employeeId,
        contractNumber,
        contractType,
        startDate ?? new DateOnly(2026, 10, 1),
        noEndDate ? null : endDate ?? new DateOnly(2027, 9, 30),
        salary,
        currency,
        noticePeriodDays,
        isPrimary);

    public static CoreHrActor Hr(params string[] permissions) => Actor(
        employeeId: 200,
        CoreHrDataScope.Organization,
        permissions.Length == 0 ? [ContractPermissions.Read, ContractPermissions.Write] : permissions);

    public static CoreHrActor Approver() => Hr(ContractPermissions.Read, ContractPermissions.Write, ContractPermissions.Approve);

    public static CoreHrActor Self(long employeeId = EmployeeId) => Actor(employeeId, CoreHrDataScope.Self, [ContractPermissions.Read, ContractPermissions.Write]);

    public static CoreHrActor DepartmentReader(long departmentId) => Actor(employeeId: 300, CoreHrDataScope.Departments(departmentId), [ContractPermissions.Read]);

    public static CoreHrActor Actor(long employeeId, CoreHrDataScope scope, IEnumerable<string> permissions) => new(
        UserId: employeeId + 1000,
        EmployeeId: employeeId,
        DataScope: scope,
        Permissions: permissions.ToHashSet(StringComparer.Ordinal),
        CorrelationId: "test-correlation");

    public static SignedDocumentUpload Pdf(string fileName = "signed.pdf", string contentType = "application/pdf", string content = "%PDF-1.7")
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        return new SignedDocumentUpload(fileName, contentType, stream.Length, stream);
    }

    public static SignedDocumentUploader Uploader(FakeStorage? storage = null, FakeScanner? scanner = null) =>
        new(storage ?? new FakeStorage(), scanner ?? new FakeScanner(MalwareScanResult.Clean));

    internal sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }

    internal sealed class FakeStorage : IDocumentStorage
    {
        public List<(string ObjectKey, string ContentType, long PositionAtStore, string Content)> Stored { get; } = [];
        public List<string> Deleted { get; } = [];
        public List<(Uri Url, string ObjectKey, string FileName, DateTimeOffset ExpiresAt)> SignedUrls { get; } = [];

        public async Task StoreAsync(string objectKey, Stream content, string contentType, CancellationToken cancellationToken)
        {
            var position = content.Position;
            using var reader = new StreamReader(content, Encoding.UTF8, leaveOpen: true);
            var text = await reader.ReadToEndAsync(cancellationToken);
            Stored.Add((objectKey, contentType, position, text));
        }

        public Task<Uri> CreateSignedDownloadUrlAsync(string objectKey, string downloadFileName, DateTimeOffset expiresAt, CancellationToken cancellationToken)
        {
            var url = new Uri($"https://storage.test/{objectKey}?expires={expiresAt.ToUnixTimeSeconds()}");
            SignedUrls.Add((url, objectKey, downloadFileName, expiresAt));
            return Task.FromResult(url);
        }

        public Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
        {
            Deleted.Add(objectKey);
            return Task.CompletedTask;
        }
    }

    internal sealed class FakeScanner(MalwareScanResult result) : IMalwareScanner
    {
        public int Calls { get; private set; }

        public async Task<MalwareScanResult> ScanAsync(Stream content, string fileName, CancellationToken cancellationToken)
        {
            Calls++;
            // Consume the stream like a real scanner would, so the uploader must rewind before storing.
            await content.CopyToAsync(Stream.Null, cancellationToken);
            return result;
        }
    }

    /// <summary>
    /// In-memory <see cref="IContractRepository"/>: applies the actor's scope through the employee table, mimics the
    /// expiry window filter of the SQL repository and records every write so tests can assert orchestration.
    /// </summary>
    internal sealed class FakeContractRepository : IContractRepository
    {
        private long _nextId = 100;

        public Dictionary<long, ContractEmployee> Employees { get; } = new()
        {
            [EmployeeId] = new ContractEmployee(EmployeeId, DepartmentId, ManagerUserId)
        };

        public List<Contract> Contracts { get; } = [];
        public HashSet<string> TakenNumbers { get; } = new(StringComparer.Ordinal);
        public bool SaveSucceeds { get; set; } = true;
        public Exception? InsertFailure { get; set; }
        public Exception? SaveFailure { get; set; }

        public List<(Contract Contract, CoreHrActor Actor)> Inserted { get; } = [];
        public List<(long ContractId, ContractStatus Previous, ContractStatus Current, long ExpectedVersion, string? Reason)> Transitions { get; } = [];
        public List<(long ContractId, long ExpectedVersion, IReadOnlyList<string> ChangedFields)> Replacements { get; } = [];
        public List<ContractActivation> Activations { get; } = [];
        public List<(long ContractId, ContractStatus Previous, long ExpectedVersion)> SignedDocuments { get; } = [];
        public List<(long ContractId, DateTimeOffset OccurredAt, DateTimeOffset ExpiresAt)> Downloads { get; } = [];
        public ContractSearchQuery? LastSearchQuery { get; private set; }
        public (DateOnly AsOf, IReadOnlyList<ExpiryAlertWindow> Windows, PageRequest Page)? LastExpiringSearch { get; private set; }
        public int FindOtherPrimaryCalls { get; private set; }

        public Contract Add(Contract contract)
        {
            Contracts.Add(contract);
            return contract;
        }

        public Task<ContractEmployee?> GetEmployeeAsync(long employeeId, CancellationToken cancellationToken) =>
            Task.FromResult(Employees.TryGetValue(employeeId, out var employee) ? employee : null);

        public Task<PagedResult<Contract>> SearchAsync(ContractSearchQuery query, CoreHrActor actor, CancellationToken cancellationToken)
        {
            LastSearchQuery = query;
            var items = Contracts.Where(c => IsVisible(c, actor))
                .Where(c => query.EmployeeId is null || c.EmployeeId == query.EmployeeId)
                .Where(c => query.Type is null || c.ContractType == query.Type)
                .Where(c => query.Status is null || c.Status == query.Status)
                .ToList();
            return Task.FromResult(new PagedResult<Contract>(items, query.Page.Page, query.Page.PageSize, items.Count));
        }

        public Task<Contract?> GetByIdAsync(long contractId, CoreHrActor actor, CancellationToken cancellationToken) =>
            Task.FromResult(Contracts.SingleOrDefault(c => c.Id == contractId && IsVisible(c, actor)));

        public Task<bool> ContractNumberExistsAsync(string contractNumber, long? excludeContractId, CancellationToken cancellationToken) =>
            Task.FromResult(TakenNumbers.Contains(contractNumber) ||
                Contracts.Any(c => c.ContractNumber == contractNumber && c.Id != excludeContractId));

        public Task<Contract?> FindOtherPrimaryInForceAsync(long employeeId, long excludeContractId, CancellationToken cancellationToken)
        {
            FindOtherPrimaryCalls++;
            return Task.FromResult(Contracts
                .Where(c => c.EmployeeId == employeeId && c.IsPrimary && c.Id != excludeContractId && c.IsInForce)
                .OrderBy(c => c.Id)
                .FirstOrDefault());
        }

        public Task<PagedResult<Contract>> SearchExpiringAsync(
            DateOnly asOf,
            IReadOnlyList<ExpiryAlertWindow> windows,
            CoreHrActor actor,
            PageRequest page,
            CancellationToken cancellationToken)
        {
            LastExpiringSearch = (asOf, windows, page);
            var items = Contracts
                .Where(c => IsVisible(c, actor) && c.IsInForce && c.EndDate is { } end && end >= asOf &&
                    windows.Any(w => w.Type == c.ContractType && end <= w.LatestEndDate))
                .OrderBy(c => c.EndDate)
                .ThenBy(c => c.Id)
                .ToList();
            var pageItems = items.Skip(page.Skip).Take(page.PageSize).ToList();
            return Task.FromResult(new PagedResult<Contract>(pageItems, page.Page, page.PageSize, items.Count));
        }

        public Task<IReadOnlyList<Contract>> ListDueForExpiryAsync(DateOnly today, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Contract>>(Contracts
                .Where(c => c.Status == ContractStatus.Active && c.EndDate is { } end && end < today)
                .OrderBy(c => c.EndDate)
                .ThenBy(c => c.Id)
                .ToList());

        public Task<Contract> InsertAsync(Contract contract, CoreHrActor actor, CancellationToken cancellationToken)
        {
            if (InsertFailure is not null)
            {
                throw InsertFailure;
            }

            var persisted = new Contract(
                _nextId++,
                contract.EmployeeId,
                contract.ContractNumber,
                contract.ContractType,
                contract.StartDate,
                contract.EndDate,
                contract.Salary,
                contract.Currency,
                contract.NoticePeriodDays,
                contract.Status,
                contract.IsPrimary,
                contract.DocumentObjectKey,
                contract.SignedAt,
                contract.Version,
                contract.CreatedAt,
                contract.UpdatedAt);
            Contracts.Add(persisted);
            Inserted.Add((persisted, actor));
            return Task.FromResult(persisted);
        }

        public Task<bool> SaveReplacementAsync(Contract contract, long expectedVersion, IReadOnlyList<string> changedFields, CoreHrActor actor, CancellationToken cancellationToken)
        {
            Replacements.Add((contract.Id, expectedVersion, changedFields));
            return Task.FromResult(SaveSucceeds);
        }

        public Task<bool> SaveTransitionAsync(Contract contract, ContractStatus previousStatus, long expectedVersion, string? reason, CoreHrActor actor, CancellationToken cancellationToken)
        {
            Transitions.Add((contract.Id, previousStatus, contract.Status, expectedVersion, reason));
            return Task.FromResult(SaveSucceeds);
        }

        public Task<bool> SaveActivationAsync(ContractActivation activation, CoreHrActor actor, CancellationToken cancellationToken)
        {
            Activations.Add(activation);
            return Task.FromResult(SaveSucceeds);
        }

        public Task<bool> SaveSignedDocumentAsync(Contract contract, ContractStatus previousStatus, long expectedVersion, CoreHrActor actor, CancellationToken cancellationToken)
        {
            if (SaveFailure is not null)
            {
                throw SaveFailure;
            }

            SignedDocuments.Add((contract.Id, previousStatus, expectedVersion));
            return Task.FromResult(SaveSucceeds);
        }

        public Task RecordDownloadAsync(Contract contract, CoreHrActor actor, DateTimeOffset occurredAt, DateTimeOffset expiresAt, CancellationToken cancellationToken)
        {
            Downloads.Add((contract.Id, occurredAt, expiresAt));
            return Task.CompletedTask;
        }

        private bool IsVisible(Contract contract, CoreHrActor actor) =>
            Employees.TryGetValue(contract.EmployeeId, out var employee) &&
            actor.CanAccessEmployee(contract.EmployeeId, employee.DepartmentId);
    }
}
