using Microsoft.EntityFrameworkCore;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Applications;
using Qlns.BusinessLogic.Modules.Recruitment.Intake;
using Qlns.DataAccess.Modules.CoreHr.Shared;
using Qlns.DataAccess.Modules.Recruitment.Shared;

namespace Qlns.DataAccess.Modules.Recruitment.Applications;

/// <summary>
/// PostgreSQL persistence for applications. Reads join job_postings so the actor's data scope is applied in SQL;
/// writes pair the conditional stage update with its application_stage_events row, audit row and any outbox message
/// in one transaction (sequence diagram §3).
/// </summary>
public sealed class RecruitmentApplicationRepository(QlnsDbContext dbContext) : IRecruitmentApplicationRepository
{
    private const string EntityType = "application";
    private const string LikeEscape = "\\";
    private const string RejectedMessageType = "recruitment.application.rejected";
    private const string InterviewScheduled = "scheduled";
    private const string InterviewCompleted = "completed";

    private DbSet<ApplicationEntity> Applications => dbContext.Set<ApplicationEntity>();
    private DbSet<JobPostingEntity> Postings => dbContext.Set<JobPostingEntity>();
    private DbSet<CandidateEntity> Candidates => dbContext.Set<CandidateEntity>();
    private DbSet<AuditLogEntity> AuditLogs => dbContext.Set<AuditLogEntity>();

    public async Task<RecruitmentApplication?> GetByIdAsync(
        long applicationId,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        var entity = await (
            from application in Applications.AsNoTracking()
            join posting in VisiblePostings(actor) on application.JobPostingId equals posting.Id
            where application.Id == applicationId
            select application)
            .SingleOrDefaultAsync(cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public Task<bool> IsRequisitionVisibleAsync(long requisitionId, CoreHrActor actor, CancellationToken cancellationToken) =>
        VisiblePostings(actor).AnyAsync(x => x.Id == requisitionId, cancellationToken);

    public async Task<PipelineColumn> GetPipelineColumnAsync(
        RecruitmentPipelineQuery query,
        ApplicationStage stage,
        CancellationToken cancellationToken)
    {
        var stageValue = stage.ToContract();
        var cards =
            from application in Applications.AsNoTracking()
            join candidate in Candidates.AsNoTracking() on application.CandidateId equals candidate.Id
            where application.JobPostingId == query.RequisitionId && application.Stage == stageValue
            select new { Application = application, Candidate = candidate };

        if (query.MinimumAiScore is { } minimumAiScore)
        {
            cards = cards.Where(x => x.Application.AiScore != null && x.Application.AiScore >= minimumAiScore);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{EscapeLike(query.Search.Trim())}%";
            cards = cards.Where(x =>
                EF.Functions.ILike(x.Candidate.FirstName, pattern, LikeEscape) ||
                EF.Functions.ILike(x.Candidate.LastName, pattern, LikeEscape) ||
                EF.Functions.ILike(x.Candidate.Email, pattern, LikeEscape));
        }

        var totalItems = await cards.CountAsync(cancellationToken);
        if (totalItems == 0)
        {
            return new PipelineColumn(stage, 0, null, []);
        }

        var averageAiScore = await cards.AverageAsync(x => x.Application.AiScore, cancellationToken);

        var rows = await cards
            .OrderByDescending(x => x.Application.AppliedAt)
            .ThenByDescending(x => x.Application.Id)
            .Skip(query.Page.Skip)
            .Take(query.Page.PageSize)
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(row => new PipelineCard(ToDomain(row.Application), ToSummary(row.Candidate)))
            .ToList();

        return new PipelineColumn(stage, totalItems, averageAiScore, items);
    }

    public async Task<AdvanceEligibility> GetAdvanceEligibilityAsync(
        long applicationId,
        ApplicationStage targetStage,
        CancellationToken cancellationToken)
    {
        var hasScheduledInterview = targetStage != ApplicationStage.TechInterview ||
            await dbContext.Set<InterviewEntity>().AsNoTracking().AnyAsync(
                x => x.ApplicationId == applicationId && x.Status == InterviewScheduled,
                cancellationToken);

        var hasEligibleEvaluation = targetStage != ApplicationStage.OfferLetter ||
            await (
                from evaluation in dbContext.Set<EvaluationEntity>().AsNoTracking()
                join interview in dbContext.Set<InterviewEntity>().AsNoTracking() on evaluation.InterviewId equals interview.Id
                where interview.ApplicationId == applicationId &&
                    interview.Status == InterviewCompleted &&
                    evaluation.UnlockedAt == null &&
                    (evaluation.Recommendation == "hire" || evaluation.Recommendation == "strong_hire")
                select evaluation.Id)
                .AnyAsync(cancellationToken);

        return new AdvanceEligibility(hasScheduledInterview, hasEligibleEvaluation);
    }

    public Task<bool> SaveAdvanceAsync(
        RecruitmentApplication application,
        ApplicationStage previousStage,
        long expectedVersion,
        string? reason,
        CoreHrActor actor,
        CancellationToken cancellationToken) =>
        SaveStageChangeAsync(application, previousStage, expectedVersion, reason, "recruitment.application.advance", actor, cancellationToken);

    public Task<bool> SaveTerminationAsync(
        RecruitmentApplication application,
        ApplicationStage previousStage,
        long expectedVersion,
        string reason,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        var action = application.Stage switch
        {
            ApplicationStage.Rejected => "recruitment.application.reject",
            ApplicationStage.Withdrawn => "recruitment.application.withdraw",
            _ => throw new ArgumentException("The application is not in a terminal stage.", nameof(application))
        };

        return SaveStageChangeAsync(application, previousStage, expectedVersion, reason, action, actor, cancellationToken);
    }

    private async Task<bool> SaveStageChangeAsync(
        RecruitmentApplication application,
        ApplicationStage previousStage,
        long expectedVersion,
        string? reason,
        string auditAction,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var rows = await Applications
            .Where(x => x.Id == application.Id && x.Version == expectedVersion)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.Stage, application.Stage.ToContract())
                    .SetProperty(x => x.Version, application.Version)
                    .SetProperty(x => x.UpdatedAt, application.UpdatedAt),
                cancellationToken);

        if (rows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        dbContext.Set<ApplicationStageEventEntity>().Add(new ApplicationStageEventEntity
        {
            ApplicationId = application.Id,
            FromStage = previousStage.ToContract(),
            ToStage = application.Stage.ToContract(),
            Reason = reason,
            ChangedBy = actor.UserId,
            ChangedAt = application.UpdatedAt,
            ApplicationVersion = application.Version
        });

        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            auditAction,
            EntityType,
            application.Id,
            before: new { stage = previousStage.ToContract(), version = expectedVersion },
            after: new { stage = application.Stage.ToContract(), version = application.Version, reason },
            occurredAt: application.UpdatedAt));

        if (application.Stage == ApplicationStage.Rejected)
        {
            // Thank-you e-mail is sent by the worker after commit; the reason stays internal.
            dbContext.Set<OutboxMessageEntity>().Add(CoreHrOutbox.Message(
                RejectedMessageType,
                EntityType,
                application.Id,
                new { applicationId = application.Id, candidateId = application.CandidateId },
                application.UpdatedAt));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    /// <summary>Postings the actor may see: organization-wide scope, or the posting's department in the actor's set.</summary>
    private IQueryable<JobPostingEntity> VisiblePostings(CoreHrActor actor)
    {
        var postings = Postings.AsNoTracking();
        if (actor.DataScope.OrganizationWide)
        {
            return postings;
        }

        var departmentIds = actor.DataScope.DepartmentIds;
        return postings.Where(x => departmentIds.Contains(x.DepartmentId));
    }

    private static string EscapeLike(string term) => term
        .Replace(LikeEscape, LikeEscape + LikeEscape, StringComparison.Ordinal)
        .Replace("%", LikeEscape + "%", StringComparison.Ordinal)
        .Replace("_", LikeEscape + "_", StringComparison.Ordinal);

    private static CandidateSummary ToSummary(CandidateEntity entity) => new(
        entity.Id,
        entity.FirstName,
        entity.LastName,
        entity.Email,
        entity.Phone,
        entity.LinkedinUrl,
        entity.PortfolioUrl);

    private static RecruitmentApplication ToDomain(ApplicationEntity entity)
    {
        if (!ApplicationStageNames.TryParseContract(entity.Stage, out var stage))
        {
            throw new InvalidOperationException($"applications {entity.Id} has unknown stage '{entity.Stage}'.");
        }

        return new RecruitmentApplication(
            entity.Id,
            entity.CandidateId,
            entity.JobPostingId,
            entity.ResumeId,
            stage,
            entity.AiScore,
            entity.Source,
            entity.AppliedAt,
            entity.Version,
            entity.UpdatedAt);
    }
}
