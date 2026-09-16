using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Qlns.BusinessLogic.Modules.Recruitment.Applications;

namespace Qlns.DataAccess.Modules.Recruitment.Applications;

public sealed class RecruitmentApplicationRepository(QlnsDbContext dbContext)
    : IRecruitmentApplicationRepository
{
    public async Task<RecruitmentApplication?> GetByIdAsync(
        long applicationId,
        RecruitmentDataScope dataScope,
        CancellationToken cancellationToken)
    {
        var query =
            from application in dbContext.Applications.AsNoTracking()
            join job in dbContext.JobPostings.AsNoTracking()
                on application.JobPostingId equals job.Id
            where application.Id == applicationId &&
                (dataScope.OrganizationWide || dataScope.DepartmentIds.Contains(job.DepartmentId))
            select application;

        var entity = await query
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == applicationId, cancellationToken);

        if (entity is null || !ApplicationStageNames.TryParseContract(entity.Stage, out var stage))
        {
            return null;
        }

        return new RecruitmentApplication(
            entity.Id,
            entity.CandidateId,
            entity.JobPostingId,
            stage,
            entity.Version,
            entity.UpdatedAt);
    }

    public async Task<AdvanceEligibility> GetAdvanceEligibilityAsync(
        long applicationId,
        ApplicationStage targetStage,
        CancellationToken cancellationToken)
    {
        var hasScheduledInterview = targetStage != ApplicationStage.TechInterview ||
            await dbContext.Interviews.AnyAsync(
                x => x.ApplicationId == applicationId && x.Status == "scheduled",
                cancellationToken);

        var hasEligibleEvaluation = targetStage != ApplicationStage.OfferLetter ||
            await (
                from evaluation in dbContext.Evaluations
                join interview in dbContext.Interviews on evaluation.InterviewId equals interview.Id
                where interview.ApplicationId == applicationId &&
                    interview.Status == "completed" &&
                    (evaluation.Recommendation == "hire" || evaluation.Recommendation == "strong_hire")
                select evaluation.Id)
                .AnyAsync(cancellationToken);

        return new AdvanceEligibility(hasScheduledInterview, hasEligibleEvaluation);
    }

    public async Task<bool> SaveAdvanceAsync(
        RecruitmentApplication application,
        ApplicationStage previousStage,
        long expectedVersion,
        long actorUserId,
        string correlationId,
        string? reason,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var rows = await dbContext.Applications
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

        dbContext.ApplicationStageEvents.Add(new ApplicationStageEventEntity
        {
            ApplicationId = application.Id,
            FromStage = previousStage.ToContract(),
            ToStage = application.Stage.ToContract(),
            Reason = reason,
            ChangedBy = actorUserId,
            ChangedAt = application.UpdatedAt,
            ApplicationVersion = application.Version
        });

        dbContext.AuditLogs.Add(new AuditLogEntity
        {
            ActorUserId = actorUserId,
            Action = "recruitment.application.advance",
            EntityType = "application",
            EntityId = application.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            BeforeData = JsonSerializer.Serialize(new { stage = previousStage.ToContract(), version = expectedVersion }),
            AfterData = JsonSerializer.Serialize(new { stage = application.Stage.ToContract(), version = application.Version }),
            Result = "succeeded",
            CorrelationId = correlationId,
            OccurredAt = application.UpdatedAt
        });

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }
}
