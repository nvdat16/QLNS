using System.Text;
using Qlns.BusinessLogic.Modules.CoreHr.EmployeeDocuments;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Xunit;

namespace Qlns.BusinessLogic.UnitTests.Modules.CoreHr.EmployeeDocuments;

public sealed class EmployeeDocumentServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 17, 8, 30, 0, TimeSpan.Zero);

    private const long EmployeeId = 10;
    private const long DepartmentId = 3;
    private const long OtherDepartmentId = 42;

    [Fact]
    public async Task ListAsync_FiltersDocumentsTheActorMayNotReadAndOrdersByTypeThenVersionDesc()
    {
        var repository = new FakeRepository();
        repository.AddDocument(1, DocumentType.Degree, version: 1);
        repository.AddDocument(2, DocumentType.Degree, version: 2);
        repository.AddDocument(3, DocumentType.Contract, version: 1);
        repository.AddDocument(4, DocumentType.DisciplinaryRecord, version: 1);
        var service = CreateService(repository);

        var forSelf = await service.ListAsync(EmployeeId, Self(), CancellationToken.None);
        var forColleague = await service.ListAsync(EmployeeId, Colleague(), CancellationToken.None);
        var forHr = await service.ListAsync(EmployeeId, HrSensitive(), CancellationToken.None);

        Assert.Equal([3, 2, 1], forSelf.Select(d => d.Id));
        Assert.Equal([2, 1], forColleague.Select(d => d.Id));
        Assert.Equal([3, 2, 1, 4], forHr.Select(d => d.Id));
    }

    [Fact]
    public async Task ListAsync_EmployeeOutOfScope_ThrowsNotFound()
    {
        var repository = new FakeRepository();
        var outsider = Actor(employeeId: 77, CoreHrDataScope.Departments(OtherDepartmentId), CoreHrPermissions.DocumentRead);
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            service.ListAsync(EmployeeId, outsider, CancellationToken.None));

        Assert.Equal("Employee", exception.Resource);
        await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            service.ListAsync(999, HrSensitive(), CancellationToken.None));
    }

    [Fact]
    public async Task UploadAsync_HappyPath_StoresOnceInsertsOnceWithNextVersionAndRewoundStream()
    {
        var repository = new FakeRepository();
        repository.AddDocument(1, DocumentType.Degree, version: 1);
        repository.AddDocument(2, DocumentType.Degree, version: 2);
        var storage = new FakeStorage();
        var scanner = new FakeScanner(MalwareScanResult.Clean);
        var service = CreateService(repository, storage, scanner);

        var result = await service.UploadAsync(Command(HrSensitive(), DocumentType.Degree, "Degree Scan.PDF", "Application/PDF"), CancellationToken.None);

        Assert.Equal(3, result.Version);
        Assert.Equal(100, result.Id);
        Assert.Equal(EmployeeId, result.EmployeeId);
        Assert.Equal(DocumentType.Degree, result.DocumentType);
        Assert.Equal("application/pdf", result.ContentType);
        Assert.Equal(Now, result.UploadedAt);
        Assert.Equal(HrSensitive().UserId, result.UploadedBy);
        Assert.StartsWith("employees/10/degree/v3/", result.ObjectKey, StringComparison.Ordinal);

        Assert.Equal(1, scanner.Calls);
        Assert.Single(storage.Stored);
        Assert.Equal(result.ObjectKey, storage.Stored[0].ObjectKey);
        Assert.Equal(0, storage.Stored[0].PositionAtStore);
        Assert.Equal("hello", storage.Stored[0].Content);
        Assert.Single(repository.Inserted);
        Assert.Equal(HrSensitive().UserId, repository.Inserted[0].Actor.UserId);
        Assert.Empty(storage.Deleted);
    }

    [Fact]
    public async Task UploadAsync_FirstVersion_StartsAtOne()
    {
        var repository = new FakeRepository();
        var service = CreateService(repository);

        var result = await service.UploadAsync(Command(Self(), DocumentType.Resume), CancellationToken.None);

        Assert.Equal(1, result.Version);
    }

    [Fact]
    public async Task UploadAsync_Infected_Throws422AndStoresNothing()
    {
        var repository = new FakeRepository();
        var storage = new FakeStorage();
        var service = CreateService(repository, storage, new FakeScanner(MalwareScanResult.Infected));

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() =>
            service.UploadAsync(Command(HrSensitive(), DocumentType.Degree), CancellationToken.None));

        Assert.Equal(["The file failed the malware scan."], exception.Errors["file"]);
        Assert.Empty(storage.Stored);
        Assert.Empty(repository.Inserted);
    }

    [Fact]
    public async Task UploadAsync_ScannerUnavailable_Throws409ScanUnavailable()
    {
        var repository = new FakeRepository();
        var storage = new FakeStorage();
        var service = CreateService(repository, storage, new FakeScanner(MalwareScanResult.Unavailable));

        var exception = await Assert.ThrowsAsync<CoreHrBusinessRuleException>(() =>
            service.UploadAsync(Command(HrSensitive(), DocumentType.Degree), CancellationToken.None));

        Assert.Equal("corehr.document.scan_unavailable", exception.Code);
        Assert.Empty(storage.Stored);
        Assert.Empty(repository.Inserted);
    }

    [Fact]
    public async Task UploadAsync_DisciplinaryRecordByNonHr_Throws403WithoutScanning()
    {
        var repository = new FakeRepository();
        var storage = new FakeStorage();
        var scanner = new FakeScanner(MalwareScanResult.Clean);
        var service = CreateService(repository, storage, scanner);

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() =>
            service.UploadAsync(Command(Self(), DocumentType.DisciplinaryRecord), CancellationToken.None));

        Assert.Equal("corehr.document.upload_forbidden", exception.Code);
        Assert.Equal(0, scanner.Calls);
        Assert.Empty(storage.Stored);
    }

    [Fact]
    public async Task UploadAsync_UnknownType_Throws422DocumentType()
    {
        var service = CreateService(new FakeRepository());

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() =>
            service.UploadAsync(Command(HrSensitive(), "passport"), CancellationToken.None));

        Assert.Contains("documentType", exception.Errors.Keys);
    }

    [Fact]
    public async Task UploadAsync_InvalidFile_Throws422EvenThoughControllerFiltersFirst()
    {
        var service = CreateService(new FakeRepository());

        var exception = await Assert.ThrowsAsync<CoreHrValidationException>(() =>
            service.UploadAsync(Command(HrSensitive(), DocumentType.Degree, contentType: "application/zip"), CancellationToken.None));

        Assert.Contains("file", exception.Errors.Keys);
    }

    [Fact]
    public async Task UploadAsync_EmployeeOutOfScope_ThrowsNotFound()
    {
        var outsider = Actor(employeeId: 77, CoreHrDataScope.Departments(OtherDepartmentId),
            CoreHrPermissions.DocumentUpload, CoreHrPermissions.DocumentReadSensitive);
        var service = CreateService(new FakeRepository());

        await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            service.UploadAsync(Command(outsider, DocumentType.Degree), CancellationToken.None));
    }

    [Fact]
    public async Task UploadAsync_InsertFails_DeletesStoredObjectAndRethrows()
    {
        var repository = new FakeRepository { InsertFailure = new InvalidOperationException("unique violation") };
        var storage = new FakeStorage();
        var service = CreateService(repository, storage);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UploadAsync(Command(HrSensitive(), DocumentType.Degree), CancellationToken.None));

        Assert.Equal("unique violation", exception.Message);
        Assert.Single(storage.Stored);
        Assert.Equal([storage.Stored[0].ObjectKey], storage.Deleted);
    }

    [Fact]
    public async Task CreateDownloadUrlAsync_PermittedDocument_ExpiresInFifteenMinutesAndAuditsGrant()
    {
        var repository = new FakeRepository();
        repository.AddDocument(3, DocumentType.Contract, version: 1);
        var storage = new FakeStorage();
        var service = CreateService(repository, storage);

        var download = await service.CreateDownloadUrlAsync(3, Self(), CancellationToken.None);

        Assert.Equal(Now.AddMinutes(15), download.ExpiresAt);
        Assert.Equal(EmployeeDocumentService.SignedUrlLifetime, download.ExpiresAt - Now);
        Assert.Single(storage.SignedUrls);
        Assert.Equal(download.Url, storage.SignedUrls[0].Url);
        Assert.Equal("scan.pdf", storage.SignedUrls[0].FileName);
        Assert.Equal(download.ExpiresAt, storage.SignedUrls[0].ExpiresAt);
        Assert.Single(repository.Granted);
        Assert.Equal((3L, Now, download.ExpiresAt), (repository.Granted[0].Document.Id, repository.Granted[0].OccurredAt, repository.Granted[0].ExpiresAt));
        Assert.Empty(repository.Denied);
    }

    [Fact]
    public async Task CreateDownloadUrlAsync_ForbiddenDocument_Throws403AuditsDenialAndCreatesNoUrl()
    {
        var repository = new FakeRepository();
        repository.AddDocument(4, DocumentType.DisciplinaryRecord, version: 1);
        var storage = new FakeStorage();
        var service = CreateService(repository, storage);

        var exception = await Assert.ThrowsAsync<CoreHrForbiddenException>(() =>
            service.CreateDownloadUrlAsync(4, Self(), CancellationToken.None));

        Assert.Equal("corehr.document.access_forbidden", exception.Code);
        Assert.Empty(storage.SignedUrls);
        Assert.Empty(repository.Granted);
        Assert.Single(repository.Denied);
        Assert.Equal(4, repository.Denied[0].Document.Id);
        Assert.Equal(Now, repository.Denied[0].OccurredAt);
    }

    [Fact]
    public async Task CreateDownloadUrlAsync_UnknownDocument_Throws404()
    {
        var service = CreateService(new FakeRepository());

        var exception = await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            service.CreateDownloadUrlAsync(12345, HrSensitive(), CancellationToken.None));

        Assert.Equal("Employee document", exception.Resource);
        Assert.Equal(12345, exception.Id);
    }

    [Fact]
    public async Task CreateDownloadUrlAsync_SoftDeletedDocument_Throws404()
    {
        var repository = new FakeRepository();
        repository.AddDocument(5, DocumentType.Degree, version: 1, deletedAt: Now.AddDays(-1));
        var storage = new FakeStorage();
        var service = CreateService(repository, storage);

        await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            service.CreateDownloadUrlAsync(5, HrSensitive(), CancellationToken.None));

        Assert.Empty(storage.SignedUrls);
        Assert.Empty(repository.Granted);
        Assert.Empty(repository.Denied);
    }

    [Fact]
    public async Task CreateDownloadUrlAsync_EmployeeOutOfScope_Throws404NotForbidden()
    {
        var repository = new FakeRepository();
        repository.AddDocument(6, DocumentType.Degree, version: 1);
        var outsider = Actor(employeeId: 77, CoreHrDataScope.Departments(OtherDepartmentId),
            CoreHrPermissions.DocumentRead, CoreHrPermissions.DocumentReadSensitive);
        var service = CreateService(repository);

        await Assert.ThrowsAsync<CoreHrNotFoundException>(() =>
            service.CreateDownloadUrlAsync(6, outsider, CancellationToken.None));

        Assert.Empty(repository.Denied);
    }

    private static EmployeeDocumentService CreateService(
        FakeRepository repository,
        FakeStorage? storage = null,
        FakeScanner? scanner = null) =>
        new(repository, storage ?? new FakeStorage(), scanner ?? new FakeScanner(MalwareScanResult.Clean), new FixedTimeProvider(Now));

    private static UploadEmployeeDocumentCommand Command(
        CoreHrActor actor,
        string documentType,
        string fileName = "scan.pdf",
        string contentType = "application/pdf")
    {
        var content = new MemoryStream(Encoding.UTF8.GetBytes("hello"));
        return new UploadEmployeeDocumentCommand(
            EmployeeId,
            documentType,
            fileName,
            contentType,
            content.Length,
            content,
            RetentionUntil: null,
            actor);
    }

    private static CoreHrActor Self() =>
        Actor(EmployeeId, CoreHrDataScope.Self, CoreHrPermissions.DocumentRead, CoreHrPermissions.DocumentUpload);

    private static CoreHrActor Colleague() =>
        Actor(employeeId: 11, CoreHrDataScope.Departments(DepartmentId), CoreHrPermissions.DocumentRead);

    private static CoreHrActor HrSensitive() =>
        Actor(employeeId: 200, CoreHrDataScope.Organization,
            CoreHrPermissions.DocumentRead, CoreHrPermissions.DocumentReadSensitive, CoreHrPermissions.DocumentUpload);

    private static CoreHrActor Actor(long employeeId, CoreHrDataScope scope, params string[] permissions) => new(
        UserId: employeeId + 1000,
        EmployeeId: employeeId,
        DataScope: scope,
        Permissions: permissions.ToHashSet(StringComparer.Ordinal),
        CorrelationId: "test-correlation");

    private sealed class FakeRepository : IEmployeeDocumentRepository
    {
        private readonly Dictionary<long, long> _employeeDepartments = new() { [EmployeeId] = DepartmentId };
        private readonly List<EmployeeDocument> _documents = [];
        private long _nextId = 100;

        public Exception? InsertFailure { get; init; }
        public List<(EmployeeDocument Document, CoreHrActor Actor)> Inserted { get; } = [];
        public List<(EmployeeDocument Document, CoreHrActor Actor, DateTimeOffset OccurredAt, DateTimeOffset ExpiresAt)> Granted { get; } = [];
        public List<(EmployeeDocument Document, CoreHrActor Actor, DateTimeOffset OccurredAt)> Denied { get; } = [];

        public void AddDocument(long id, string type, int version, DateTimeOffset? deletedAt = null) =>
            _documents.Add(new EmployeeDocument(
                id,
                EmployeeId,
                type,
                version,
                originalFileName: "scan.pdf",
                objectKey: $"employees/{EmployeeId}/{type}/v{version}/{id:D8}",
                contentType: "application/pdf",
                sizeBytes: 5,
                uploadedBy: 1,
                uploadedAt: Now.AddDays(-1),
                retentionUntil: null,
                deletedAt));

        public Task<long?> GetEmployeeDepartmentAsync(long employeeId, CancellationToken cancellationToken) =>
            Task.FromResult(_employeeDepartments.TryGetValue(employeeId, out var department) ? department : (long?)null);

        public Task<IReadOnlyList<EmployeeDocument>> ListByEmployeeAsync(long employeeId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<EmployeeDocument>>(
                _documents.Where(d => d.EmployeeId == employeeId && !d.IsSoftDeleted).ToList());

        public Task<(EmployeeDocument Document, long EmployeeDepartmentId)?> GetByIdAsync(long documentId, CancellationToken cancellationToken)
        {
            var document = _documents.SingleOrDefault(d => d.Id == documentId);
            return Task.FromResult<(EmployeeDocument, long)?>(
                document is null ? null : (document, _employeeDepartments[document.EmployeeId]));
        }

        public Task<int> GetLatestVersionAsync(long employeeId, string documentType, CancellationToken cancellationToken) =>
            Task.FromResult(_documents
                .Where(d => d.EmployeeId == employeeId && d.DocumentType == documentType)
                .Select(d => d.Version)
                .DefaultIfEmpty(0)
                .Max());

        public Task<EmployeeDocument> InsertAsync(EmployeeDocument document, CoreHrActor actor, CancellationToken cancellationToken)
        {
            if (InsertFailure is not null)
            {
                throw InsertFailure;
            }

            var persisted = document.WithId(_nextId++);
            _documents.Add(persisted);
            Inserted.Add((persisted, actor));
            return Task.FromResult(persisted);
        }

        public Task RecordAccessGrantedAsync(EmployeeDocument document, CoreHrActor actor, DateTimeOffset occurredAt, DateTimeOffset expiresAt, CancellationToken cancellationToken)
        {
            Granted.Add((document, actor, occurredAt, expiresAt));
            return Task.CompletedTask;
        }

        public Task RecordAccessDeniedAsync(EmployeeDocument document, CoreHrActor actor, DateTimeOffset occurredAt, CancellationToken cancellationToken)
        {
            Denied.Add((document, actor, occurredAt));
            return Task.CompletedTask;
        }
    }

    private sealed class FakeStorage : IDocumentStorage
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

    private sealed class FakeScanner(MalwareScanResult result) : IMalwareScanner
    {
        public int Calls { get; private set; }

        public async Task<MalwareScanResult> ScanAsync(Stream content, string fileName, CancellationToken cancellationToken)
        {
            Calls++;
            // Consume the stream like a real scanner would, so the service must rewind before storing.
            await content.CopyToAsync(Stream.Null, cancellationToken);
            return result;
        }
    }

    private sealed class FixedTimeProvider(DateTimeOffset value) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => value;
    }
}
