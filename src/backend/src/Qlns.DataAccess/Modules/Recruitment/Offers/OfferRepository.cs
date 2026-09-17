using Microsoft.EntityFrameworkCore;
using Npgsql;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Offers;
using Qlns.DataAccess.Modules.Contracts.Shared;
using Qlns.DataAccess.Modules.CoreHr.Shared;
using Qlns.DataAccess.Modules.Recruitment.Shared;

namespace Qlns.DataAccess.Modules.Recruitment.Offers;

/// <summary>
/// PostgreSQL persistence for offers and the candidate-to-employee handoff (sequence diagram §4). Scoped reads join
/// applications → job_postings; every write is one transaction pairing the business change with audit_logs (and
/// outbox_messages). Audit payloads never contain salary amounts or response tokens.
/// </summary>
public sealed class OfferRepository(QlnsDbContext dbContext) : IOfferRepository
{
    private const string OfferEntityType = "offer";
    private const string OfferAggregateType = "offer";
    private const string SentMessage = "recruitment.offer.sent";
    private const string ExtendedMessage = "recruitment.offer.extended";
    private const string AcceptedMessage = "recruitment.offer.accepted";
    private const string PendingTaskStatus = "pending";

    private static readonly string[] OpenStatuses = OfferStatusNames.Open.Select(status => status.ToContract()).ToArray();

    private DbSet<OfferEntity> Offers => dbContext.Set<OfferEntity>();
    private DbSet<ApplicationEntity> Applications => dbContext.Set<ApplicationEntity>();
    private DbSet<ApplicationStageEventEntity> StageEvents => dbContext.Set<ApplicationStageEventEntity>();
    private DbSet<CandidateEntity> Candidates => dbContext.Set<CandidateEntity>();
    private DbSet<JobPostingEntity> JobPostings => dbContext.Set<JobPostingEntity>();
    private DbSet<EmployeeEntity> Employees => dbContext.Set<EmployeeEntity>();
    private DbSet<ContractEntity> Contracts => dbContext.Set<ContractEntity>();
    private DbSet<OnboardingTaskEntity> OnboardingTasks => dbContext.Set<OnboardingTaskEntity>();
    private DbSet<AuditLogEntity> AuditLogs => dbContext.Set<AuditLogEntity>();
    private DbSet<OutboxMessageEntity> Outbox => dbContext.Set<OutboxMessageEntity>();

    public async Task<PagedResult<Offer>> SearchAsync(OfferSearchQuery query, CoreHrActor actor, CancellationToken cancellationToken)
    {
        var offers = Visible(actor);

        if (query.ApplicationId is { } applicationId)
        {
            offers = offers.Where(o => o.ApplicationId == applicationId);
        }

        if (query.Status is { } status)
        {
            var statusValue = status.ToContract();
            offers = offers.Where(o => o.Status == statusValue);
        }

        var totalItems = await offers.LongCountAsync(cancellationToken);
        if (totalItems == 0)
        {
            return PagedResult<Offer>.Empty(query.Page);
        }

        var entities = await offers
            .OrderByDescending(o => o.CreatedAt)
            .ThenByDescending(o => o.Id)
            .Skip(query.Page.Skip)
            .Take(query.Page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Offer>(entities.Select(ToDomain).ToList(), query.Page.Page, query.Page.PageSize, totalItems);
    }

    public async Task<Offer?> GetByIdAsync(long offerId, CoreHrActor actor, CancellationToken cancellationToken)
    {
        var entity = await Visible(actor).SingleOrDefaultAsync(o => o.Id == offerId, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public async Task<Offer?> GetForCandidateAsync(long offerId, CancellationToken cancellationToken)
    {
        var entity = await Offers.AsNoTracking().SingleOrDefaultAsync(o => o.Id == offerId, cancellationToken);
        return entity is null ? null : ToDomain(entity);
    }

    public Task<OfferApplication?> GetApplicationAsync(long applicationId, CoreHrActor actor, CancellationToken cancellationToken)
    {
        var organizationWide = actor.DataScope.OrganizationWide;
        var departmentIds = actor.DataScope.DepartmentIds;

        return (
            from application in Applications.AsNoTracking()
            join job in JobPostings.AsNoTracking() on application.JobPostingId equals job.Id
            where application.Id == applicationId &&
                (organizationWide || departmentIds.Contains(job.DepartmentId))
            select new OfferApplication(application.Id, application.CandidateId, application.JobPostingId, job.DepartmentId, application.Stage))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public Task<bool> HasOpenOfferAsync(long applicationId, CancellationToken cancellationToken) =>
        Offers.AsNoTracking().AnyAsync(o => o.ApplicationId == applicationId && OpenStatuses.Contains(o.Status), cancellationToken);

    public async Task<Offer?> InsertAsync(Offer offer, CoreHrActor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(offer);
        ArgumentNullException.ThrowIfNull(actor);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var entity = new OfferEntity
        {
            ApplicationId = offer.ApplicationId,
            BaseSalary = offer.BaseSalary,
            BonusAmount = offer.BonusAmount,
            AllowanceAmount = offer.AllowanceAmount,
            Currency = offer.Currency,
            EmploymentType = offer.EmploymentType,
            StartDate = offer.StartDate,
            ExpirationDate = offer.ExpirationDate,
            Status = offer.Status.ToContract(),
            TemplateVersion = offer.TemplateVersion,
            DocumentObjectKey = null,
            CreatedAt = offer.CreatedAt,
            UpdatedAt = offer.UpdatedAt,
            Version = offer.Version
        };

        Offers.Add(entity);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            dbContext.Entry(entity).State = EntityState.Detached;
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            "recruitment.offer.create",
            OfferEntityType,
            entity.Id,
            before: null,
            after: new
            {
                applicationId = offer.ApplicationId,
                status = offer.Status.ToContract(),
                employmentType = offer.EmploymentType,
                currency = offer.Currency,
                startDate = offer.StartDate,
                expirationDate = offer.ExpirationDate,
                templateVersion = offer.TemplateVersion,
                version = offer.Version
            },
            offer.CreatedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ToDomain(entity);
    }

    public async Task<bool> SaveTransitionAsync(
        Offer offer,
        OfferTransition transition,
        OfferSnapshot before,
        string? reason,
        string? responseToken,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(offer);
        ArgumentNullException.ThrowIfNull(before);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var rows = await UpdateOfferRowAsync(offer, before.Version, cancellationToken);
        if (rows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            transition.ToAuditAction(),
            OfferEntityType,
            offer.Id,
            before: AuditSnapshot(before),
            after: AuditSnapshot(offer.Snapshot(), reason),
            occurredAt: offer.UpdatedAt));

        if (transition is OfferTransition.Send or OfferTransition.Extend)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(responseToken);
            var candidate = await CandidateOfAsync(offer.ApplicationId, cancellationToken);

            Outbox.Add(CoreHrOutbox.Message(
                transition == OfferTransition.Send ? SentMessage : ExtendedMessage,
                OfferAggregateType,
                offer.Id,
                new
                {
                    offerId = offer.Id,
                    applicationId = offer.ApplicationId,
                    candidateId = candidate.Id,
                    candidateEmail = candidate.Email,
                    startDate = offer.StartDate,
                    expirationDate = offer.ExpirationDate,
                    templateVersion = offer.TemplateVersion,
                    responseToken
                },
                offer.UpdatedAt));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<Offer>> ListExpiredSentAsync(DateOnly today, CancellationToken cancellationToken)
    {
        var sent = OfferStatus.Sent.ToContract();
        var rows = await Offers.AsNoTracking()
            .Where(o => o.Status == sent && o.ExpirationDate < today)
            .OrderBy(o => o.ExpirationDate)
            .ThenBy(o => o.Id)
            .ToListAsync(cancellationToken);

        return rows.Select(ToDomain).ToList();
    }

    public Task<OfferHandoffContext?> GetHandoffContextAsync(long applicationId, CancellationToken cancellationToken) =>
        (
            from application in Applications.AsNoTracking()
            join candidate in Candidates.AsNoTracking() on application.CandidateId equals candidate.Id
            join job in JobPostings.AsNoTracking() on application.JobPostingId equals job.Id
            where application.Id == applicationId
            select new OfferHandoffContext(
                new HandoffApplication(application.Id, application.CandidateId, application.JobPostingId, application.Stage, application.Version),
                new HandoffCandidate(candidate.FirstName, candidate.LastName, candidate.Email, candidate.Phone),
                new HandoffJobPosting(job.DepartmentId, job.PositionId)))
        .SingleOrDefaultAsync(cancellationToken);

    public async Task<OfferHandoffResult?> FindHandoffAsync(long applicationId, CancellationToken cancellationToken)
    {
        var employeeId = await Employees.AsNoTracking()
            .Where(e => e.SourceApplicationId == applicationId)
            .Select(e => (long?)e.Id)
            .SingleOrDefaultAsync(cancellationToken);

        if (employeeId is not { } id)
        {
            return null;
        }

        return await LoadHandoffAsync(id, cancellationToken);
    }

    public async Task<OfferHandoffResult?> SaveAcceptanceAsync(
        Offer offer,
        OfferSnapshot before,
        OfferHandoffPlan plan,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(offer);
        ArgumentNullException.ThrowIfNull(before);
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(actor);

        var now = offer.UpdatedAt;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var offerRows = await UpdateOfferRowAsync(offer, before.Version, cancellationToken);
        if (offerRows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        var hire = plan.Application;
        var applicationRows = await Applications
            .Where(a => a.Id == hire.ApplicationId && a.Version == hire.ExpectedVersion)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(a => a.Stage, hire.ToStage)
                    .SetProperty(a => a.Version, hire.NewVersion)
                    .SetProperty(a => a.UpdatedAt, now),
                cancellationToken);

        if (applicationRows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        // The candidate has no users row; the automatic stage change is attributed to the HR Manager who approved the offer.
        var changedBy = actor.UserId > 0
            ? actor.UserId
            : offer.ApprovedBy ?? throw new InvalidOperationException($"Offer {offer.Id} is sent but has no approver.");

        StageEvents.Add(new ApplicationStageEventEntity
        {
            ApplicationId = hire.ApplicationId,
            FromStage = hire.FromStage,
            ToStage = hire.ToStage,
            Reason = "Offer accepted by candidate",
            ChangedBy = changedBy,
            ChangedAt = now,
            ApplicationVersion = hire.NewVersion
        });

        var sequenceValue = await dbContext.Database
            .SqlQuery<long>($"SELECT nextval('employee_code_seq') AS \"Value\"")
            .SingleAsync(cancellationToken);
        var employeeCode = OfferHandoff.EmployeeCode(sequenceValue);

        var employee = new EmployeeEntity
        {
            EmployeeCode = employeeCode,
            SourceApplicationId = plan.Employee.SourceApplicationId,
            UserId = null,
            FirstName = plan.Employee.FirstName,
            LastName = plan.Employee.LastName,
            WorkEmail = null,
            PersonalEmail = plan.Employee.PersonalEmail,
            Phone = plan.Employee.Phone,
            ManagerId = null,
            DepartmentId = plan.Employee.DepartmentId,
            PositionId = plan.Employee.PositionId,
            HireDate = plan.Employee.HireDate,
            Status = plan.Employee.Status,
            CreatedAt = now,
            UpdatedAt = now,
            Version = 1
        };

        Employees.Add(employee);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception))
        {
            // Another accept already linked an employee to this application (ux on employees.source_application_id).
            dbContext.ChangeTracker.Clear();
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        var contract = new ContractEntity
        {
            EmployeeId = employee.Id,
            ContractNumber = OfferHandoff.ProbationContractNumber(employeeCode),
            ContractType = plan.Contract.ContractType,
            StartDate = plan.Contract.StartDate,
            EndDate = plan.Contract.EndDate,
            Salary = plan.Contract.Salary,
            Currency = plan.Contract.Currency,
            NoticePeriodDays = plan.Contract.NoticePeriodDays,
            Status = plan.Contract.Status,
            IsPrimary = plan.Contract.IsPrimary,
            DocumentObjectKey = null,
            SignedAt = null,
            CreatedAt = now,
            UpdatedAt = now,
            Version = 1
        };

        var tasks = plan.Tasks.Select(task => new OnboardingTaskEntity
        {
            EmployeeId = employee.Id,
            TemplateKey = task.TemplateKey,
            TaskName = task.TaskName,
            Description = task.Description,
            AssignedToUserId = null,
            DueAt = task.DueAt,
            Status = PendingTaskStatus,
            CompletedAt = null,
            CreatedAt = now,
            UpdatedAt = now,
            Version = 1
        }).ToList();

        Contracts.Add(contract);
        OnboardingTasks.AddRange(tasks);
        await dbContext.SaveChangesAsync(cancellationToken);

        var taskIds = tasks.Select(task => task.Id).ToList();

        AuditLogs.Add(CoreHrAudit.Entry(actor, "recruitment.offer.accept", OfferEntityType, offer.Id,
            before: AuditSnapshot(before),
            after: AuditSnapshot(offer.Snapshot()),
            occurredAt: now));

        AuditLogs.Add(CoreHrAudit.Entry(actor, "recruitment.application.advance", "application", hire.ApplicationId,
            before: new { stage = hire.FromStage, version = hire.ExpectedVersion },
            after: new { stage = hire.ToStage, version = hire.NewVersion, offerId = offer.Id },
            occurredAt: now));

        AuditLogs.Add(CoreHrAudit.Entry(actor, "recruitment.offer.handoff", "employee", employee.Id,
            before: null,
            after: new
            {
                employeeCode,
                sourceApplicationId = plan.Employee.SourceApplicationId,
                departmentId = plan.Employee.DepartmentId,
                positionId = plan.Employee.PositionId,
                hireDate = plan.Employee.HireDate,
                status = plan.Employee.Status,
                version = 1
            },
            occurredAt: now));

        AuditLogs.Add(CoreHrAudit.Entry(actor, "recruitment.offer.handoff", "contract", contract.Id,
            before: null,
            after: new
            {
                employeeId = employee.Id,
                contractNumber = contract.ContractNumber,
                contractType = contract.ContractType,
                startDate = contract.StartDate,
                endDate = contract.EndDate,
                status = contract.Status,
                version = 1
            },
            occurredAt: now));

        AuditLogs.Add(CoreHrAudit.Entry(actor, "recruitment.offer.handoff", "onboarding_task", employee.Id,
            before: null,
            after: new { employeeId = employee.Id, taskIds, templateKeys = plan.Tasks.Select(task => task.TemplateKey).ToList() },
            occurredAt: now));

        Outbox.Add(CoreHrOutbox.Message(
            AcceptedMessage,
            OfferAggregateType,
            offer.Id,
            new
            {
                offerId = offer.Id,
                applicationId = hire.ApplicationId,
                candidateId = hire.CandidateId,
                employeeId = employee.Id,
                employeeCode,
                contractId = contract.Id,
                onboardingTaskIds = taskIds,
                startDate = plan.Employee.HireDate,
                departmentId = plan.Employee.DepartmentId,
                positionId = plan.Employee.PositionId
            },
            now));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new OfferHandoffResult(employee.Id, contract.Id, taskIds);
    }

    private IQueryable<OfferEntity> Visible(CoreHrActor actor)
    {
        var offers = Offers.AsNoTracking();
        if (actor.DataScope.OrganizationWide)
        {
            return offers;
        }

        var departmentIds = actor.DataScope.DepartmentIds;

        return
            from offer in offers
            join application in Applications.AsNoTracking() on offer.ApplicationId equals application.Id
            join job in JobPostings.AsNoTracking() on application.JobPostingId equals job.Id
            where departmentIds.Contains(job.DepartmentId)
            select offer;
    }

    private Task<int> UpdateOfferRowAsync(Offer offer, long expectedVersion, CancellationToken cancellationToken) =>
        Offers
            .Where(o => o.Id == offer.Id && o.Version == expectedVersion)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(o => o.Status, offer.Status.ToContract())
                    .SetProperty(o => o.ExpirationDate, offer.ExpirationDate)
                    .SetProperty(o => o.ApprovedBy, offer.ApprovedBy)
                    .SetProperty(o => o.ApprovedAt, offer.ApprovedAt)
                    .SetProperty(o => o.SentAt, offer.SentAt)
                    .SetProperty(o => o.RespondedAt, offer.RespondedAt)
                    .SetProperty(o => o.Version, offer.Version)
                    .SetProperty(o => o.UpdatedAt, offer.UpdatedAt),
                cancellationToken);

    private async Task<OfferHandoffResult> LoadHandoffAsync(long employeeId, CancellationToken cancellationToken)
    {
        var contractId = await Contracts.AsNoTracking()
            .Where(c => c.EmployeeId == employeeId && c.ContractType == OfferHandoff.ProbationContractType)
            .OrderBy(c => c.CreatedAt)
            .ThenBy(c => c.Id)
            .Select(c => (long?)c.Id)
            .FirstOrDefaultAsync(cancellationToken);

        var taskIds = await OnboardingTasks.AsNoTracking()
            .Where(t => t.EmployeeId == employeeId)
            .OrderBy(t => t.Id)
            .Select(t => t.Id)
            .ToListAsync(cancellationToken);

        return new OfferHandoffResult(employeeId, contractId, taskIds);
    }

    private Task<CandidateEntity> CandidateOfAsync(long applicationId, CancellationToken cancellationToken) =>
        (
            from application in Applications.AsNoTracking()
            join candidate in Candidates.AsNoTracking() on application.CandidateId equals candidate.Id
            where application.Id == applicationId
            select candidate)
        .SingleAsync(cancellationToken);

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    /// <summary>Audit payload without amounts: status, dates and version only.</summary>
    private static object AuditSnapshot(OfferSnapshot snapshot, string? reason = null) => new
    {
        status = snapshot.Status.ToContract(),
        expirationDate = snapshot.ExpirationDate,
        version = snapshot.Version,
        reason
    };

    private static Offer ToDomain(OfferEntity entity)
    {
        if (!OfferStatusNames.TryParseContract(entity.Status, out var status))
        {
            throw new InvalidOperationException($"offers {entity.Id} has unknown status '{entity.Status}'.");
        }

        return new Offer(
            entity.Id,
            entity.ApplicationId,
            entity.BaseSalary,
            entity.BonusAmount,
            entity.AllowanceAmount,
            entity.Currency.Trim(),
            entity.EmploymentType,
            entity.StartDate,
            entity.ExpirationDate,
            status,
            entity.TemplateVersion,
            entity.DocumentObjectKey,
            entity.ApprovedBy,
            entity.ApprovedAt,
            entity.SentAt,
            entity.RespondedAt,
            entity.Version,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}
