using Microsoft.EntityFrameworkCore;
using Qlns.BusinessLogic.Modules.CoreHr.EmployeeEvents;
using Qlns.BusinessLogic.Modules.CoreHr.Probation;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.DataAccess.Modules.Contracts.Shared;
using Qlns.DataAccess.Modules.CoreHr.Offboarding;
using Qlns.DataAccess.Modules.CoreHr.Shared;

namespace Qlns.DataAccess.Modules.CoreHr.Probation;

/// <summary>
/// PostgreSQL persistence for probation_reviews. Searches join employees so the actor's data scope is applied in SQL;
/// every write is one transaction pairing the conditional review update with its audit row and, depending on the
/// action, the outbox notification (submit), the approved employee event plus offboarding case (decide) or the
/// cancellation of the linked event (unlock).
/// </summary>
public sealed class ProbationReviewRepository(QlnsDbContext dbContext) : IProbationReviewRepository
{
    private const string ReviewEntityType = "probation_review";
    private const string EventEntityType = "employee_event";
    private const string CaseEntityType = "offboarding_case";
    private const string ProbationContractType = "probation";

    private DbSet<ProbationReviewEntity> Reviews => dbContext.Set<ProbationReviewEntity>();
    private DbSet<EmployeeEntity> Employees => dbContext.Set<EmployeeEntity>();
    private DbSet<ContractEntity> Contracts => dbContext.Set<ContractEntity>();
    private DbSet<EmployeeEventEntity> Events => dbContext.Set<EmployeeEventEntity>();
    private DbSet<OffboardingCaseEntity> Cases => dbContext.Set<OffboardingCaseEntity>();
    private DbSet<AuditLogEntity> AuditLogs => dbContext.Set<AuditLogEntity>();
    private DbSet<OutboxMessageEntity> Outbox => dbContext.Set<OutboxMessageEntity>();

    public Task<long?> GetEmployeeDepartmentAsync(long employeeId, CancellationToken cancellationToken) =>
        Employees.AsNoTracking()
            .Where(x => x.Id == employeeId)
            .Select(x => (long?)x.DepartmentId)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<ProbationReviewEntry?> GetCurrentForEmployeeAsync(long employeeId, CancellationToken cancellationToken)
    {
        var cancelled = ProbationReviewStatus.Cancelled.ToContract();
        var row = await (
            from review in Reviews.AsNoTracking()
            join contract in Contracts.AsNoTracking() on review.ContractId equals contract.Id
            join employee in Employees.AsNoTracking() on review.EmployeeId equals employee.Id
            where review.EmployeeId == employeeId &&
                review.Status != cancelled &&
                contract.ContractType == ProbationContractType
            orderby review.CreatedAt descending, review.Id descending
            select new { Review = review, employee.DepartmentId })
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : new ProbationReviewEntry(ToDomain(row.Review), row.DepartmentId);
    }

    public async Task<ProbationReviewEntry?> GetByIdAsync(long reviewId, CancellationToken cancellationToken)
    {
        var row = await (
            from review in Reviews.AsNoTracking()
            join employee in Employees.AsNoTracking() on review.EmployeeId equals employee.Id
            where review.Id == reviewId
            select new { Review = review, employee.DepartmentId })
            .SingleOrDefaultAsync(cancellationToken);

        return row is null ? null : new ProbationReviewEntry(ToDomain(row.Review), row.DepartmentId);
    }

    public async Task<PagedResult<ProbationReview>> SearchAsync(
        ProbationReviewSearchQuery query,
        CoreHrActor actor,
        DateOnly today,
        CancellationToken cancellationToken)
    {
        var rows =
            from review in Reviews.AsNoTracking()
            join employee in Employees.AsNoTracking() on review.EmployeeId equals employee.Id
            select new { Review = review, employee.DepartmentId };

        if (!actor.DataScope.OrganizationWide)
        {
            // Department scope, the actor's own review, or reviews the actor is assigned to assess.
            var departmentIds = actor.DataScope.DepartmentIds.ToArray();
            var selfId = actor.EmployeeId ?? 0;
            var userId = actor.UserId;
            rows = rows.Where(x =>
                departmentIds.Contains(x.DepartmentId) ||
                x.Review.EmployeeId == selfId ||
                x.Review.ReviewerUserId == userId);
        }

        if (query.Status is { } status)
        {
            var statusValue = status.ToContract();
            rows = rows.Where(x => x.Review.Status == statusValue);
        }

        if (query.Overdue is { } overdue)
        {
            var pending = ProbationReviewStatus.Pending.ToContract();
            var inReview = ProbationReviewStatus.InReview.ToContract();
            rows = overdue
                ? rows.Where(x => x.Review.ReviewDueDate < today && (x.Review.Status == pending || x.Review.Status == inReview))
                : rows.Where(x => x.Review.ReviewDueDate >= today || (x.Review.Status != pending && x.Review.Status != inReview));
        }

        if (query.DepartmentId is { } departmentId)
        {
            rows = rows.Where(x => x.DepartmentId == departmentId);
        }

        var totalItems = await rows.LongCountAsync(cancellationToken);
        if (totalItems == 0)
        {
            return PagedResult<ProbationReview>.Empty(query.Page);
        }

        var entities = await rows
            .OrderBy(x => x.Review.ReviewDueDate)
            .ThenBy(x => x.Review.Id)
            .Skip(query.Page.Skip)
            .Take(query.Page.PageSize)
            .Select(x => x.Review)
            .ToListAsync(cancellationToken);

        return new PagedResult<ProbationReview>(
            entities.Select(ToDomain).ToList(),
            query.Page.Page,
            query.Page.PageSize,
            totalItems);
    }

    public async Task<EmployeeEventStatus?> GetEmployeeEventStatusAsync(long employeeEventId, CancellationToken cancellationToken)
    {
        var value = await Events.AsNoTracking()
            .Where(x => x.Id == employeeEventId)
            .Select(x => x.Status)
            .SingleOrDefaultAsync(cancellationToken);

        if (value is null)
        {
            return null;
        }

        if (!EmployeeEventStatusNames.TryParseContract(value, out var status))
        {
            throw new InvalidOperationException($"employee_events {employeeEventId} has unknown status '{value}'.");
        }

        return status;
    }

    public async Task<bool> SaveAssessmentAsync(
        ProbationReview review,
        ProbationReviewStatus previousStatus,
        long expectedVersion,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var outcome = review.Outcome?.ToContract();
        var rows = await Reviews
            .Where(r => r.Id == review.Id && r.Version == expectedVersion)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(r => r.Status, review.Status.ToContract())
                    .SetProperty(r => r.Outcome, outcome)
                    .SetProperty(r => r.OverallScore, review.OverallScore)
                    .SetProperty(r => r.Strengths, review.Strengths)
                    .SetProperty(r => r.Improvements, review.Improvements)
                    .SetProperty(r => r.Version, review.Version)
                    .SetProperty(r => r.UpdatedAt, review.UpdatedAt),
                cancellationToken);

        if (rows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            "corehr.probation.submit",
            ReviewEntityType,
            review.Id,
            before: new { status = previousStatus.ToContract(), version = expectedVersion },
            after: new
            {
                status = review.Status.ToContract(),
                version = review.Version,
                recommendedOutcome = outcome,
                overallScore = review.OverallScore,
                fields = new[] { "overallScore", "strengths", "improvements", "recommendedOutcome" }
            },
            occurredAt: review.UpdatedAt));

        // Notify HR Manager that an assessment is ready to decide (EMP-06.1 scenario 1).
        Outbox.Add(CoreHrOutbox.Message(
            "corehr.probation.review_submitted",
            ReviewEntityType,
            review.Id,
            new
            {
                reviewId = review.Id,
                employeeId = review.EmployeeId,
                contractId = review.ContractId,
                recommendedOutcome = outcome,
                reviewerUserId = actor.UserId
            },
            review.UpdatedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SaveDecisionAsync(
        ProbationReview review,
        ProbationDecisionPlan plan,
        long expectedVersion,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var now = review.UpdatedAt;
        var decided = ProbationReviewStatus.Decided.ToContract();

        // 1. The approved event first, so its generated id can be linked by the conditional update below.
        var eventEntity = OffboardingMapping.ToApprovedEventEntity(plan.EmployeeEvent, actor.UserId, now);
        Events.Add(eventEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        // 2. Conditional update: version match plus the idempotency guard — one review yields at most one event.
        var rows = await Reviews
            .Where(r => r.Id == review.Id && r.Version == expectedVersion && r.Status != decided && r.EmployeeEventId == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(r => r.Status, decided)
                    .SetProperty(r => r.Outcome, review.Outcome!.Value.ToContract())
                    .SetProperty(r => r.EffectiveDate, review.EffectiveDate)
                    .SetProperty(r => r.DecidedBy, review.DecidedBy)
                    .SetProperty(r => r.DecidedAt, review.DecidedAt)
                    .SetProperty(r => r.EmployeeEventId, eventEntity.Id)
                    .SetProperty(r => r.Version, review.Version)
                    .SetProperty(r => r.UpdatedAt, now),
                cancellationToken);

        if (rows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        review.LinkEmployeeEvent(eventEntity.Id);

        // 3. A terminated outcome opens the offboarding case unless the employee already has an open one.
        long? offboardingCaseId = null;
        if (plan.OffboardingCase is { } offboardingCase &&
            !await Cases.AnyAsync(c => c.EmployeeId == offboardingCase.EmployeeId && OffboardingMapping.OpenCaseStatuses.Contains(c.Status), cancellationToken))
        {
            var caseEntity = OffboardingMapping.ToEntity(offboardingCase);
            Cases.Add(caseEntity);
            await dbContext.SaveChangesAsync(cancellationToken);
            offboardingCaseId = caseEntity.Id;

            AuditLogs.Add(CoreHrAudit.Entry(
                actor,
                "corehr.offboarding.case.create",
                CaseEntityType,
                caseEntity.Id,
                before: null,
                after: new
                {
                    status = caseEntity.Status,
                    version = caseEntity.Version,
                    employeeId = caseEntity.EmployeeId,
                    separationType = caseEntity.SeparationType,
                    lastWorkingDate = caseEntity.LastWorkingDate,
                    source = new { entityType = ReviewEntityType, entityId = review.Id }
                },
                occurredAt: now));
        }

        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            "corehr.event.create",
            EventEntityType,
            eventEntity.Id,
            before: null,
            after: OffboardingMapping.ApprovedEventAuditSnapshot(plan.EmployeeEvent, ReviewEntityType, review.Id),
            occurredAt: now));

        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            "corehr.probation.decide",
            ReviewEntityType,
            review.Id,
            before: new { status = ProbationReviewStatus.InReview.ToContract(), version = expectedVersion },
            after: new
            {
                status = decided,
                version = review.Version,
                outcome = review.Outcome!.Value.ToContract(),
                effectiveDate = review.EffectiveDate,
                employeeEventId = eventEntity.Id,
                offboardingCaseId,
                reason = plan.EmployeeEvent.Reason
            },
            occurredAt: now));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SaveCancellationAsync(
        ProbationReview review,
        ProbationReviewStatus previousStatus,
        long expectedVersion,
        string reason,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var rows = await Reviews
            .Where(r => r.Id == review.Id && r.Version == expectedVersion)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(r => r.Status, review.Status.ToContract())
                    .SetProperty(r => r.Version, review.Version)
                    .SetProperty(r => r.UpdatedAt, review.UpdatedAt),
                cancellationToken);

        if (rows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            "corehr.probation.cancel",
            ReviewEntityType,
            review.Id,
            before: new { status = previousStatus.ToContract(), version = expectedVersion },
            after: new { status = review.Status.ToContract(), version = review.Version, reason },
            occurredAt: review.UpdatedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SaveUnlockAsync(
        ProbationReview review,
        long employeeEventId,
        long expectedVersion,
        string reason,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var now = review.UpdatedAt;
        var rows = await Reviews
            .Where(r => r.Id == review.Id && r.Version == expectedVersion)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(r => r.Status, review.Status.ToContract())
                    .SetProperty(r => r.EmployeeEventId, (long?)null)
                    .SetProperty(r => r.DecidedBy, (long?)null)
                    .SetProperty(r => r.DecidedAt, (DateTimeOffset?)null)
                    .SetProperty(r => r.EffectiveDate, (DateOnly?)null)
                    .SetProperty(r => r.Version, review.Version)
                    .SetProperty(r => r.UpdatedAt, now),
                cancellationToken);

        if (rows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        // The linked event is cancelled in the same transaction, guarded against a concurrent worker run
        // that applied it in the meantime (then the unlock loses the race and the caller reports 409).
        var applied = EmployeeEventStatus.Applied.ToContract();
        var cancelled = EmployeeEventStatus.Cancelled.ToContract();
        var previousEventStatus = await Events.AsNoTracking()
            .Where(e => e.Id == employeeEventId)
            .Select(e => e.Status)
            .SingleOrDefaultAsync(cancellationToken);

        var eventRows = await Events
            .Where(e => e.Id == employeeEventId && e.Status != applied && e.Status != cancelled)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(e => e.Status, cancelled)
                    .SetProperty(e => e.Version, e => e.Version + 1)
                    .SetProperty(e => e.UpdatedAt, now),
                cancellationToken);

        if (eventRows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            "corehr.event.cancel",
            EventEntityType,
            employeeEventId,
            before: new { status = previousEventStatus },
            after: new { status = cancelled, reason, source = new { entityType = ReviewEntityType, entityId = review.Id } },
            occurredAt: now));

        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            "corehr.probation.unlock",
            ReviewEntityType,
            review.Id,
            before: new { status = ProbationReviewStatus.Decided.ToContract(), version = expectedVersion, employeeEventId },
            after: new
            {
                status = review.Status.ToContract(),
                version = review.Version,
                recommendedOutcome = review.Outcome?.ToContract(),
                reason
            },
            occurredAt: now));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    private static ProbationReview ToDomain(ProbationReviewEntity entity)
    {
        if (!ProbationReviewStatusNames.TryParseContract(entity.Status, out var status))
        {
            throw new InvalidOperationException($"probation_reviews {entity.Id} has unknown status '{entity.Status}'.");
        }

        ProbationOutcome? outcome = null;
        if (entity.Outcome is not null)
        {
            if (!ProbationOutcomeNames.TryParseContract(entity.Outcome, out var parsed))
            {
                throw new InvalidOperationException($"probation_reviews {entity.Id} has unknown outcome '{entity.Outcome}'.");
            }

            outcome = parsed;
        }

        return new ProbationReview(
            entity.Id,
            entity.EmployeeId,
            entity.ContractId,
            entity.ReviewDueDate,
            entity.ReviewerUserId,
            status,
            outcome,
            entity.OverallScore,
            entity.Strengths,
            entity.Improvements,
            entity.EffectiveDate,
            entity.DecidedBy,
            entity.DecidedAt,
            entity.EmployeeEventId,
            entity.Version,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}
