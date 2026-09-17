using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Qlns.BusinessLogic.Modules.Contracts.Contracts;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.DataAccess.Modules.Contracts.Shared;
using Qlns.DataAccess.Modules.CoreHr.Probation;
using Qlns.DataAccess.Modules.CoreHr.Shared;

namespace Qlns.DataAccess.Modules.Contracts.Contracts;

/// <summary>
/// PostgreSQL persistence for <c>contracts</c>. Every read joins <c>employees</c> so the actor's data scope
/// (organization, department of the contract's employee, or the actor's own record) is applied in SQL. Writes are
/// one transaction each: conditional update on the expected version, audit rows, outbox rows; a lost race rolls back
/// and returns false. Audit payloads never contain the salary or the storage object key.
/// </summary>
public sealed class ContractRepository(QlnsDbContext dbContext) : IContractRepository
{
    private const string EntityType = "contract";
    private const string ProbationReviewEntityType = "probation_review";
    private const string ContractNumberConstraint = "contract_number";
    private const string PrimaryActiveConstraint = "ux_contracts_primary_active";
    private const string ApprovedMessageType = "contracts.contract.approved";

    private static readonly string[] InForceStatuses =
    [
        ContractStatus.Executed.ToContract(),
        ContractStatus.Active.ToContract()
    ];

    private DbSet<ContractEntity> Contracts => dbContext.Set<ContractEntity>();
    private DbSet<EmployeeEntity> Employees => dbContext.Set<EmployeeEntity>();
    private DbSet<ProbationReviewEntity> ProbationReviews => dbContext.Set<ProbationReviewEntity>();
    private DbSet<AuditLogEntity> AuditLogs => dbContext.Set<AuditLogEntity>();
    private DbSet<OutboxMessageEntity> Outbox => dbContext.Set<OutboxMessageEntity>();

    public Task<ContractEmployee?> GetEmployeeAsync(long employeeId, CancellationToken cancellationToken) =>
        Employees.AsNoTracking()
            .Where(e => e.Id == employeeId)
            .Select(e => new ContractEmployee(
                e.Id,
                e.DepartmentId,
                Employees.Where(m => m.Id == e.ManagerId).Select(m => m.UserId).FirstOrDefault()))
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<PagedResult<Contract>> SearchAsync(ContractSearchQuery query, CoreHrActor actor, CancellationToken cancellationToken)
    {
        var contracts = Visible(actor);

        if (query.EmployeeId is { } employeeId)
        {
            contracts = contracts.Where(c => c.EmployeeId == employeeId);
        }

        if (query.Type is { } type)
        {
            var typeValue = type.ToContract();
            contracts = contracts.Where(c => c.ContractType == typeValue);
        }

        if (query.Status is { } status)
        {
            var statusValue = status.ToContract();
            contracts = contracts.Where(c => c.Status == statusValue);
        }

        var totalItems = await contracts.LongCountAsync(cancellationToken);
        if (totalItems == 0)
        {
            return PagedResult<Contract>.Empty(query.Page);
        }

        var rows = await contracts
            .OrderByDescending(c => c.StartDate)
            .ThenByDescending(c => c.Id)
            .Skip(query.Page.Skip)
            .Take(query.Page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Contract>(rows.Select(ToDomain).ToList(), query.Page.Page, query.Page.PageSize, totalItems);
    }

    public async Task<Contract?> GetByIdAsync(long contractId, CoreHrActor actor, CancellationToken cancellationToken)
    {
        var entity = await Visible(actor).SingleOrDefaultAsync(c => c.Id == contractId, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public Task<bool> ContractNumberExistsAsync(string contractNumber, long? excludeContractId, CancellationToken cancellationToken) =>
        Contracts.AsNoTracking()
            .AnyAsync(c => c.ContractNumber == contractNumber && (excludeContractId == null || c.Id != excludeContractId), cancellationToken);

    public async Task<Contract?> FindOtherPrimaryInForceAsync(long employeeId, long excludeContractId, CancellationToken cancellationToken)
    {
        var entity = await Contracts.AsNoTracking()
            .Where(c => c.EmployeeId == employeeId && c.IsPrimary && c.Id != excludeContractId && InForceStatuses.Contains(c.Status))
            .OrderBy(c => c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<PagedResult<Contract>> SearchExpiringAsync(
        DateOnly asOf,
        IReadOnlyList<ExpiryAlertWindow> windows,
        CoreHrActor actor,
        PageRequest page,
        CancellationToken cancellationToken)
    {
        var contracts = Visible(actor)
            .Where(c => c.EndDate != null && c.EndDate >= asOf && InForceStatuses.Contains(c.Status))
            .Where(InAlertWindow(windows));

        var totalItems = await contracts.LongCountAsync(cancellationToken);
        if (totalItems == 0)
        {
            return PagedResult<Contract>.Empty(page);
        }

        var rows = await contracts
            .OrderBy(c => c.EndDate)
            .ThenBy(c => c.Id)
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Contract>(rows.Select(ToDomain).ToList(), page.Page, page.PageSize, totalItems);
    }

    public async Task<IReadOnlyList<Contract>> ListDueForExpiryAsync(DateOnly today, CancellationToken cancellationToken)
    {
        var active = ContractStatus.Active.ToContract();
        var rows = await Contracts.AsNoTracking()
            .Where(c => c.Status == active && c.EndDate != null && c.EndDate < today)
            .OrderBy(c => c.EndDate)
            .ThenBy(c => c.Id)
            .ToListAsync(cancellationToken);

        return rows.Select(ToDomain).ToList();
    }

    public async Task<Contract> InsertAsync(Contract contract, CoreHrActor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contract);
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var entity = new ContractEntity
        {
            EmployeeId = contract.EmployeeId,
            ContractNumber = contract.ContractNumber,
            ContractType = contract.ContractType.ToContract(),
            StartDate = contract.StartDate,
            EndDate = contract.EndDate,
            Salary = contract.Salary,
            Currency = contract.Currency,
            NoticePeriodDays = contract.NoticePeriodDays,
            Status = contract.Status.ToContract(),
            IsPrimary = contract.IsPrimary,
            DocumentObjectKey = null,
            SignedAt = null,
            CreatedAt = contract.CreatedAt,
            UpdatedAt = contract.UpdatedAt,
            Version = contract.Version
        };

        Contracts.Add(entity);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (UniqueViolation.IsOn(exception, ContractNumberConstraint))
        {
            throw ContractService.NumberTaken(contract.ContractNumber);
        }

        var persisted = ToDomain(entity);
        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            "contracts.contract.create",
            EntityType,
            persisted.Id,
            before: null,
            after: Snapshot(persisted),
            occurredAt: persisted.CreatedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return persisted;
    }

    public async Task<bool> SaveReplacementAsync(
        Contract contract,
        long expectedVersion,
        IReadOnlyList<string> changedFields,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        int rows;
        try
        {
            rows = await Contracts
                .Where(c => c.Id == contract.Id && c.Version == expectedVersion)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(c => c.ContractNumber, contract.ContractNumber)
                        .SetProperty(c => c.ContractType, contract.ContractType.ToContract())
                        .SetProperty(c => c.StartDate, contract.StartDate)
                        .SetProperty(c => c.EndDate, contract.EndDate)
                        .SetProperty(c => c.Salary, contract.Salary)
                        .SetProperty(c => c.Currency, contract.Currency)
                        .SetProperty(c => c.NoticePeriodDays, contract.NoticePeriodDays)
                        .SetProperty(c => c.IsPrimary, contract.IsPrimary)
                        .SetProperty(c => c.Version, contract.Version)
                        .SetProperty(c => c.UpdatedAt, contract.UpdatedAt),
                    cancellationToken);
        }
        catch (PostgresException exception) when (UniqueViolation.IsOn(exception, ContractNumberConstraint))
        {
            throw ContractService.NumberTaken(contract.ContractNumber);
        }

        if (rows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            "contracts.contract.update",
            EntityType,
            contract.Id,
            before: new { version = expectedVersion },
            after: new { version = contract.Version, changedFields },
            occurredAt: contract.UpdatedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SaveTransitionAsync(
        Contract contract,
        ContractStatus previousStatus,
        long expectedVersion,
        string? reason,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        if (!await UpdateStatusAsync(contract, expectedVersion, cancellationToken))
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        AddTransitionAudit(contract, previousStatus, expectedVersion, reason, actor);

        if (contract.Status == ContractStatus.Approved)
        {
            Outbox.Add(CoreHrOutbox.Message(
                ApprovedMessageType,
                EntityType,
                contract.Id,
                new
                {
                    contractId = contract.Id,
                    employeeId = contract.EmployeeId,
                    contractNumber = contract.ContractNumber,
                    contractType = contract.ContractType.ToContract(),
                    startDate = contract.StartDate,
                    endDate = contract.EndDate,
                    approvedByUserId = actor.UserId,
                    correlationId = actor.CorrelationId
                },
                contract.UpdatedAt));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SaveActivationAsync(ContractActivation activation, CoreHrActor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(activation);
        var contract = activation.Contract;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // The predecessor leaves the in-force slot first so ux_contracts_primary_active accepts the successor.
        if (activation.Superseded is { } superseded)
        {
            if (!await UpdateStatusAsync(superseded.Contract, superseded.ExpectedVersion, cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            AddTransitionAudit(
                superseded.Contract,
                superseded.PreviousStatus,
                superseded.ExpectedVersion,
                superseded.OverrideReason,
                actor,
                new { supersededByContractId = contract.Id, overridden = superseded.OverrideReason is not null });
        }

        bool activated;
        try
        {
            activated = await Contracts
                .Where(c => c.Id == contract.Id && c.Version == activation.ExpectedVersion)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(c => c.Status, contract.Status.ToContract())
                        .SetProperty(c => c.SignedAt, contract.SignedAt)
                        .SetProperty(c => c.Version, contract.Version)
                        .SetProperty(c => c.UpdatedAt, contract.UpdatedAt),
                    cancellationToken) == 1;
        }
        catch (PostgresException exception) when (UniqueViolation.IsOn(exception, PrimaryActiveConstraint))
        {
            throw new CoreHrBusinessRuleException(
                ContractService.PrimaryOverlapCode,
                $"Employee {contract.EmployeeId} already has a primary contract in force; reload and retry.");
        }

        if (!activated)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        AddTransitionAudit(contract, activation.PreviousStatus, activation.ExpectedVersion, reason: null, actor, new
        {
            supersededContractId = activation.Superseded?.Contract.Id,
            supersededContractStatus = activation.Superseded?.Contract.Status.ToContract(),
            signedAt = contract.SignedAt
        });

        if (activation.ProbationReview is { } review &&
            !await ProbationReviews.AsNoTracking().AnyAsync(r => r.ContractId == review.ContractId, cancellationToken))
        {
            var entity = new ProbationReviewEntity
            {
                EmployeeId = review.EmployeeId,
                ContractId = review.ContractId,
                ReviewDueDate = review.ReviewDueDate,
                ReviewerUserId = review.ReviewerUserId,
                Status = ProbationReviewDraft.PendingStatus,
                CreatedAt = contract.UpdatedAt,
                UpdatedAt = contract.UpdatedAt,
                Version = 1
            };

            ProbationReviews.Add(entity);
            await dbContext.SaveChangesAsync(cancellationToken);

            AuditLogs.Add(CoreHrAudit.Entry(
                actor,
                "corehr.probation.create",
                ProbationReviewEntityType,
                entity.Id,
                before: null,
                after: new
                {
                    employeeId = review.EmployeeId,
                    contractId = review.ContractId,
                    reviewDueDate = review.ReviewDueDate,
                    reviewerUserId = review.ReviewerUserId,
                    status = ProbationReviewDraft.PendingStatus,
                    version = 1
                },
                occurredAt: contract.UpdatedAt));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SaveSignedDocumentAsync(
        Contract contract,
        ContractStatus previousStatus,
        long expectedVersion,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        int rows;
        try
        {
            rows = await Contracts
                .Where(c => c.Id == contract.Id && c.Version == expectedVersion)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(c => c.Status, contract.Status.ToContract())
                        .SetProperty(c => c.DocumentObjectKey, contract.DocumentObjectKey)
                        .SetProperty(c => c.SignedAt, contract.SignedAt)
                        .SetProperty(c => c.Version, contract.Version)
                        .SetProperty(c => c.UpdatedAt, contract.UpdatedAt),
                    cancellationToken);
        }
        catch (PostgresException exception) when (UniqueViolation.IsOn(exception, PrimaryActiveConstraint))
        {
            throw new CoreHrBusinessRuleException(
                ContractService.PrimaryOverlapCode,
                $"Employee {contract.EmployeeId} already has a primary contract in force; the contract cannot become executed yet.");
        }

        if (rows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            "contracts.contract.signed_document",
            EntityType,
            contract.Id,
            before: new { status = previousStatus.ToContract(), version = expectedVersion },
            after: new { status = contract.Status.ToContract(), version = contract.Version, signedAt = contract.SignedAt, signedDocumentAvailable = true },
            occurredAt: contract.UpdatedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task RecordDownloadAsync(Contract contract, CoreHrActor actor, DateTimeOffset occurredAt, DateTimeOffset expiresAt, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(contract);

        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            "contracts.contract.download",
            EntityType,
            contract.Id,
            before: null,
            after: new { employeeId = contract.EmployeeId, status = contract.Status.ToContract(), expiresAt },
            occurredAt));

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Contracts the actor may see: organization-wide scope, the contract's employee belongs to one of the actor's
    /// departments, or the contract belongs to the actor's own employee record.
    /// </summary>
    private IQueryable<ContractEntity> Visible(CoreHrActor actor)
    {
        var contracts = Contracts.AsNoTracking();
        if (actor.DataScope.OrganizationWide)
        {
            return contracts;
        }

        var departmentIds = actor.DataScope.DepartmentIds;
        var employeeId = actor.EmployeeId;

        return
            from contract in contracts
            join employee in Employees.AsNoTracking() on contract.EmployeeId equals employee.Id
            where departmentIds.Contains(employee.DepartmentId) ||
                (employeeId != null && contract.EmployeeId == employeeId)
            select contract;
    }

    private async Task<bool> UpdateStatusAsync(Contract contract, long expectedVersion, CancellationToken cancellationToken)
    {
        var rows = await Contracts
            .Where(c => c.Id == contract.Id && c.Version == expectedVersion)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(c => c.Status, contract.Status.ToContract())
                    .SetProperty(c => c.Version, contract.Version)
                    .SetProperty(c => c.UpdatedAt, contract.UpdatedAt),
                cancellationToken);

        return rows == 1;
    }

    private void AddTransitionAudit(
        Contract contract,
        ContractStatus previousStatus,
        long expectedVersion,
        string? reason,
        CoreHrActor actor,
        object? context = null)
    {
        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            TransitionAction(contract.Status),
            EntityType,
            contract.Id,
            before: new { status = previousStatus.ToContract(), version = expectedVersion },
            after: new { status = contract.Status.ToContract(), version = contract.Version, reason, context },
            occurredAt: contract.UpdatedAt));
    }

    private static string TransitionAction(ContractStatus newStatus) => newStatus switch
    {
        ContractStatus.Approved => "contracts.contract.approve",
        ContractStatus.Active => "contracts.contract.activate",
        ContractStatus.Terminated => "contracts.contract.terminate",
        ContractStatus.Cancelled => "contracts.contract.cancel",
        ContractStatus.Expired => "contracts.contract.expire",
        _ => throw new ArgumentOutOfRangeException(nameof(newStatus), $"{newStatus.ToContract()} is not a workflow transition target.")
    };

    /// <summary>Audit payload without the salary: identifiers, type, dates, status and version only.</summary>
    private static object Snapshot(Contract contract) => new
    {
        employeeId = contract.EmployeeId,
        contractNumber = contract.ContractNumber,
        contractType = contract.ContractType.ToContract(),
        startDate = contract.StartDate,
        endDate = contract.EndDate,
        currency = contract.Currency,
        isPrimary = contract.IsPrimary,
        status = contract.Status.ToContract(),
        version = contract.Version
    };

    /// <summary>OR of one <c>contract_type = ? AND end_date &lt;= ?</c> clause per alert window, as a translatable predicate.</summary>
    private static Expression<Func<ContractEntity, bool>> InAlertWindow(IReadOnlyList<ExpiryAlertWindow> windows)
    {
        var parameter = Expression.Parameter(typeof(ContractEntity), "c");
        Expression? body = null;

        foreach (var window in windows)
        {
            var typeValue = window.Type.ToContract();
            var latest = window.LatestEndDate;
            Expression<Func<ContractEntity, bool>> clause = c => c.ContractType == typeValue && c.EndDate <= latest;
            var rebound = new ParameterReplacer(clause.Parameters[0], parameter).Visit(clause.Body);
            body = body is null ? rebound : Expression.OrElse(body, rebound);
        }

        return Expression.Lambda<Func<ContractEntity, bool>>(body ?? Expression.Constant(false), parameter);
    }

    private static Contract ToDomain(ContractEntity entity)
    {
        if (!ContractTypeNames.TryParseContract(entity.ContractType, out var type))
        {
            throw new InvalidOperationException($"contracts {entity.Id} has unknown contract_type '{entity.ContractType}'.");
        }

        if (!ContractStatusNames.TryParseContract(entity.Status, out var status))
        {
            throw new InvalidOperationException($"contracts {entity.Id} has unknown status '{entity.Status}'.");
        }

        return new Contract(
            entity.Id,
            entity.EmployeeId,
            entity.ContractNumber,
            type,
            entity.StartDate,
            entity.EndDate,
            entity.Salary,
            entity.Currency,
            entity.NoticePeriodDays,
            status,
            entity.IsPrimary,
            entity.DocumentObjectKey,
            entity.SignedAt,
            entity.Version,
            entity.CreatedAt,
            entity.UpdatedAt);
    }

    private sealed class ParameterReplacer(ParameterExpression source, ParameterExpression target) : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node) => node == source ? target : base.VisitParameter(node);
    }
}
