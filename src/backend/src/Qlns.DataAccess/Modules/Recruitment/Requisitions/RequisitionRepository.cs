using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Requisitions;
using Qlns.DataAccess.Modules.CoreHr.Shared;
using Qlns.DataAccess.Modules.Recruitment.Shared;

namespace Qlns.DataAccess.Modules.Recruitment.Requisitions;

/// <summary>
/// PostgreSQL persistence for job_postings. Every read applies the actor's data scope on the posting's department in
/// SQL; postings outside scope are simply absent. Every write pairs the conditional business change with its audit row
/// (and, for publish, the outbox message) in one transaction. Audit payloads never contain salary amounts.
/// </summary>
public sealed class RequisitionRepository(QlnsDbContext dbContext) : IRequisitionRepository
{
    private const string EntityType = "job_posting";
    private const string LikeEscape = "\\";
    private const string PublishedMessageType = "recruitment.requisition.published";
    private const string OfferSentStatus = "sent";

    private DbSet<JobPostingEntity> Postings => dbContext.Set<JobPostingEntity>();
    private DbSet<AuditLogEntity> AuditLogs => dbContext.Set<AuditLogEntity>();

    public async Task<PagedResult<Requisition>> SearchAsync(
        RequisitionSearchQuery query,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        var postings = Visible(actor);

        if (query.DepartmentId is { } departmentId)
        {
            postings = postings.Where(x => x.DepartmentId == departmentId);
        }

        if (query.Status is { } status)
        {
            var statusValue = status.ToContract();
            postings = postings.Where(x => x.Status == statusValue);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{EscapeLike(query.Search.Trim())}%";
            postings = postings.Where(x =>
                EF.Functions.ILike(x.Title, pattern, LikeEscape) ||
                EF.Functions.ILike(x.JobCode, pattern, LikeEscape));
        }

        var totalItems = await postings.LongCountAsync(cancellationToken);
        if (totalItems == 0)
        {
            return PagedResult<Requisition>.Empty(query.Page);
        }

        var entities = await ApplySort(postings, query.Sort)
            .Skip(query.Page.Skip)
            .Take(query.Page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Requisition>(
            entities.Select(ToDomain).ToList(),
            query.Page.Page,
            query.Page.PageSize,
            totalItems);
    }

    public async Task<Requisition?> GetByIdAsync(long requisitionId, CoreHrActor actor, CancellationToken cancellationToken)
    {
        var entity = await Visible(actor).SingleOrDefaultAsync(x => x.Id == requisitionId, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public Task<bool> DepartmentExistsAsync(long departmentId, CancellationToken cancellationToken) =>
        dbContext.Set<DepartmentEntity>().AsNoTracking().AnyAsync(x => x.Id == departmentId, cancellationToken);

    public Task<bool> PositionExistsAsync(long positionId, CancellationToken cancellationToken) =>
        dbContext.Set<PositionEntity>().AsNoTracking().AnyAsync(x => x.Id == positionId, cancellationToken);

    public Task<bool> HasOpenOffersAsync(long requisitionId, CancellationToken cancellationToken) =>
        (from offer in dbContext.Set<OfferEntity>().AsNoTracking()
         join application in dbContext.Set<ApplicationEntity>().AsNoTracking()
             on offer.ApplicationId equals application.Id
         where application.JobPostingId == requisitionId && offer.Status == OfferSentStatus
         select offer.Id)
        .AnyAsync(cancellationToken);

    public async Task<Requisition> InsertAsync(Requisition draft, CoreHrActor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(actor);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var entity = new JobPostingEntity
        {
            // Provisional unique code: the final REQ-{yyyy}-{id:D5} needs the generated id.
            JobCode = string.Create(CultureInfo.InvariantCulture, $"pending-{Guid.NewGuid():N}"),
            Title = draft.Title,
            DepartmentId = draft.DepartmentId,
            PositionId = draft.PositionId,
            Description = draft.Description,
            Requirements = draft.Requirements,
            Location = draft.Location,
            EmploymentType = draft.EmploymentType.ToContract(),
            SalaryMin = draft.SalaryMin,
            SalaryMax = draft.SalaryMax,
            TargetHeadcount = draft.TargetHeadcount,
            Status = draft.Status.ToContract(),
            ClosingDate = draft.ClosingDate,
            PublishedAt = draft.PublishedAt,
            CreatedBy = draft.CreatedBy,
            CreatedAt = draft.CreatedAt,
            UpdatedAt = draft.UpdatedAt,
            Version = draft.Version
        };

        Postings.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        entity.JobCode = Requisition.BuildJobCode(draft.CreatedAt.Year, entity.Id);
        await dbContext.SaveChangesAsync(cancellationToken);

        var persisted = ToDomain(entity);
        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            "recruitment.requisition.create",
            EntityType,
            persisted.Id,
            before: null,
            after: DeclaredSnapshot(persisted),
            occurredAt: persisted.CreatedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return persisted;
    }

    public async Task<bool> ReplaceAsync(
        Requisition requisition,
        long expectedVersion,
        IReadOnlyList<string> changedFields,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var rows = await Postings
            .Where(x => x.Id == requisition.Id && x.Version == expectedVersion)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Title, requisition.Title)
                    .SetProperty(x => x.DepartmentId, requisition.DepartmentId)
                    .SetProperty(x => x.PositionId, requisition.PositionId)
                    .SetProperty(x => x.Description, requisition.Description)
                    .SetProperty(x => x.Requirements, requisition.Requirements)
                    .SetProperty(x => x.Location, requisition.Location)
                    .SetProperty(x => x.EmploymentType, requisition.EmploymentType.ToContract())
                    .SetProperty(x => x.SalaryMin, requisition.SalaryMin)
                    .SetProperty(x => x.SalaryMax, requisition.SalaryMax)
                    .SetProperty(x => x.TargetHeadcount, requisition.TargetHeadcount)
                    .SetProperty(x => x.ClosingDate, requisition.ClosingDate)
                    .SetProperty(x => x.Version, requisition.Version)
                    .SetProperty(x => x.UpdatedAt, requisition.UpdatedAt),
                cancellationToken);

        if (rows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            "recruitment.requisition.update",
            EntityType,
            requisition.Id,
            before: new { status = requisition.Status.ToContract(), version = expectedVersion },
            after: new { status = requisition.Status.ToContract(), version = requisition.Version, changedFields },
            occurredAt: requisition.UpdatedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SaveTransitionAsync(
        Requisition requisition,
        RequisitionStatus previousStatus,
        RequisitionAction action,
        long expectedVersion,
        string? reason,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var rows = await Postings
            .Where(x => x.Id == requisition.Id && x.Version == expectedVersion)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Status, requisition.Status.ToContract())
                    .SetProperty(x => x.PublishedAt, requisition.PublishedAt)
                    .SetProperty(x => x.Version, requisition.Version)
                    .SetProperty(x => x.UpdatedAt, requisition.UpdatedAt),
                cancellationToken);

        if (rows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            $"recruitment.requisition.{action.ToContract()}",
            EntityType,
            requisition.Id,
            before: new { status = previousStatus.ToContract(), version = expectedVersion },
            after: new { status = requisition.Status.ToContract(), version = requisition.Version, reason },
            occurredAt: requisition.UpdatedAt));

        if (action == RequisitionAction.Publish)
        {
            // Single built-in careers channel: the worker publishes there; no channel list is carried.
            dbContext.Set<OutboxMessageEntity>().Add(CoreHrOutbox.Message(
                PublishedMessageType,
                EntityType,
                requisition.Id,
                new
                {
                    requisitionId = requisition.Id,
                    jobCode = requisition.JobCode,
                    title = requisition.Title,
                    departmentId = requisition.DepartmentId
                },
                requisition.UpdatedAt));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    /// <summary>Postings the actor may see: organization-wide scope, or the posting's department in the actor's set.</summary>
    private IQueryable<JobPostingEntity> Visible(CoreHrActor actor)
    {
        var postings = Postings.AsNoTracking();
        if (actor.DataScope.OrganizationWide)
        {
            return postings;
        }

        var departmentIds = actor.DataScope.DepartmentIds;
        return postings.Where(x => departmentIds.Contains(x.DepartmentId));
    }

    private static IOrderedQueryable<JobPostingEntity> ApplySort(IQueryable<JobPostingEntity> postings, string sort) => sort switch
    {
        RequisitionSort.CreatedAt => postings.OrderBy(x => x.CreatedAt).ThenBy(x => x.Id),
        RequisitionSort.CreatedAtDescending => postings.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id),
        RequisitionSort.ClosingDate => postings.OrderBy(x => x.ClosingDate == null).ThenBy(x => x.ClosingDate).ThenBy(x => x.Id),
        RequisitionSort.ClosingDateDescending => postings.OrderBy(x => x.ClosingDate == null).ThenByDescending(x => x.ClosingDate).ThenByDescending(x => x.Id),
        _ => throw new ArgumentOutOfRangeException(nameof(sort), $"Unsupported sort '{sort}'.")
    };

    private static string EscapeLike(string term) => term
        .Replace(LikeEscape, LikeEscape + LikeEscape, StringComparison.Ordinal)
        .Replace("%", LikeEscape + "%", StringComparison.Ordinal)
        .Replace("_", LikeEscape + "_", StringComparison.Ordinal);

    /// <summary>Audit snapshot of the declared fields; salary amounts are deliberately excluded.</summary>
    private static object DeclaredSnapshot(Requisition requisition) => new
    {
        jobCode = requisition.JobCode,
        title = requisition.Title,
        departmentId = requisition.DepartmentId,
        positionId = requisition.PositionId,
        employmentType = requisition.EmploymentType.ToContract(),
        targetHeadcount = requisition.TargetHeadcount,
        closingDate = requisition.ClosingDate,
        status = requisition.Status.ToContract(),
        version = requisition.Version
    };

    /// <summary>Shared with the intake repository, which reads the same table through the same scope rule.</summary>
    internal static Requisition ToDomain(JobPostingEntity entity)
    {
        if (!RequisitionStatusNames.TryParseContract(entity.Status, out var status))
        {
            throw new InvalidOperationException($"job_postings {entity.Id} has unknown status '{entity.Status}'.");
        }

        if (!EmploymentTypeNames.TryParseContract(entity.EmploymentType, out var employmentType))
        {
            throw new InvalidOperationException($"job_postings {entity.Id} has unknown employment_type '{entity.EmploymentType}'.");
        }

        return new Requisition(
            entity.Id,
            entity.JobCode,
            entity.Title,
            entity.DepartmentId,
            entity.PositionId,
            entity.Description,
            entity.Requirements,
            entity.Location,
            employmentType,
            entity.SalaryMin,
            entity.SalaryMax,
            entity.TargetHeadcount,
            status,
            entity.ClosingDate,
            entity.PublishedAt,
            entity.CreatedBy,
            entity.Version,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}
