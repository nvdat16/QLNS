using Microsoft.EntityFrameworkCore;
using Npgsql;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Evaluations;
using Qlns.BusinessLogic.Modules.Recruitment.Interviews;
using Qlns.DataAccess.Modules.CoreHr.Shared;
using Qlns.DataAccess.Modules.Recruitment.Shared;

namespace Qlns.DataAccess.Modules.Recruitment.Evaluations;

/// <summary>
/// PostgreSQL persistence for evaluations. Rows are insert-only; <c>ux_evaluation_interviewer_version</c> turns a
/// concurrent duplicate submission or unlock into a null result instead of a second row. Interview visibility uses the
/// same rule as the Interviews feature. Audit payloads carry identifiers, versions and the recommendation — never scores.
/// </summary>
public sealed class EvaluationRepository(QlnsDbContext dbContext) : IEvaluationRepository
{
    private const string EntityType = "evaluation";

    private DbSet<EvaluationEntity> Evaluations => dbContext.Set<EvaluationEntity>();
    private DbSet<InterviewEntity> Interviews => dbContext.Set<InterviewEntity>();
    private DbSet<InterviewPanelistEntity> Panelists => dbContext.Set<InterviewPanelistEntity>();
    private DbSet<ApplicationEntity> Applications => dbContext.Set<ApplicationEntity>();
    private DbSet<JobPostingEntity> JobPostings => dbContext.Set<JobPostingEntity>();
    private DbSet<AuditLogEntity> AuditLogs => dbContext.Set<AuditLogEntity>();

    public async Task<EvaluationInterview?> GetInterviewAsync(long interviewId, CoreHrActor actor, CancellationToken cancellationToken)
    {
        var interview = await VisibleInterviews(actor)
            .Where(i => i.Id == interviewId)
            .Select(i => new { i.Id, i.Status, i.InterviewerUserId })
            .SingleOrDefaultAsync(cancellationToken);

        if (interview is null)
        {
            return null;
        }

        if (!InterviewStatusNames.TryParseContract(interview.Status, out var status))
        {
            throw new InvalidOperationException($"interviews {interview.Id} has unknown status '{interview.Status}'.");
        }

        var panel = await Panelists.AsNoTracking()
            .Where(p => p.InterviewId == interviewId)
            .OrderBy(p => p.UserId)
            .Select(p => p.UserId)
            .ToListAsync(cancellationToken);

        if (!panel.Contains(interview.InterviewerUserId))
        {
            panel.Insert(0, interview.InterviewerUserId);
        }

        return new EvaluationInterview(interview.Id, status, panel);
    }

    public async Task<IReadOnlyList<Evaluation>> ListByInterviewAsync(long interviewId, CancellationToken cancellationToken)
    {
        var rows = await Evaluations.AsNoTracking()
            .Where(e => e.InterviewId == interviewId)
            .OrderBy(e => e.EvaluatorUserId)
            .ThenBy(e => e.Version)
            .ToListAsync(cancellationToken);

        return rows.Select(ToDomain).ToList();
    }

    public async Task<Evaluation?> GetByIdAsync(long evaluationId, CoreHrActor actor, CancellationToken cancellationToken)
    {
        var entity = await (
            from evaluation in Evaluations.AsNoTracking()
            join interview in VisibleInterviews(actor) on evaluation.InterviewId equals interview.Id
            where evaluation.Id == evaluationId
            select evaluation)
            .SingleOrDefaultAsync(cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<Evaluation?> GetLatestAsync(long interviewId, long evaluatorUserId, CancellationToken cancellationToken)
    {
        var entity = await Evaluations.AsNoTracking()
            .Where(e => e.InterviewId == interviewId && e.EvaluatorUserId == evaluatorUserId)
            .OrderByDescending(e => e.Version)
            .FirstOrDefaultAsync(cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public Task<Evaluation?> InsertSubmissionAsync(Evaluation evaluation, CoreHrActor actor, CancellationToken cancellationToken) =>
        InsertVersionAsync(
            evaluation,
            actor,
            "recruitment.evaluation.submit",
            before: null,
            after: new
            {
                interviewId = evaluation.InterviewId,
                evaluatorUserId = evaluation.EvaluatorUserId,
                recommendation = evaluation.Recommendation.ToContract(),
                version = evaluation.Version
            },
            evaluation.SubmittedAt,
            cancellationToken);

    public Task<Evaluation?> InsertUnlockedVersionAsync(Evaluation evaluation, int previousVersion, CoreHrActor actor, CancellationToken cancellationToken) =>
        InsertVersionAsync(
            evaluation,
            actor,
            "recruitment.evaluation.unlock",
            before: new { version = previousVersion, unlocked = false },
            after: new
            {
                interviewId = evaluation.InterviewId,
                evaluatorUserId = evaluation.EvaluatorUserId,
                version = evaluation.Version,
                unlocked = true,
                unlockedBy = evaluation.UnlockedBy,
                reason = evaluation.UnlockReason
            },
            evaluation.UnlockedAt ?? evaluation.SubmittedAt,
            cancellationToken);

    private async Task<Evaluation?> InsertVersionAsync(
        Evaluation evaluation,
        CoreHrActor actor,
        string action,
        object? before,
        object after,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(evaluation);
        ArgumentNullException.ThrowIfNull(actor);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var entity = new EvaluationEntity
        {
            InterviewId = evaluation.InterviewId,
            EvaluatorUserId = evaluation.EvaluatorUserId,
            TechnicalScore = evaluation.Scores.Technical,
            CommunicationScore = evaluation.Scores.Communication,
            ProblemSolvingScore = evaluation.Scores.ProblemSolving,
            TeamworkScore = evaluation.Scores.Teamwork,
            OverallScore = evaluation.OverallScore,
            Recommendation = evaluation.Recommendation.ToContract(),
            Feedback = evaluation.Feedback,
            SubmittedAt = evaluation.SubmittedAt,
            UnlockedAt = evaluation.UnlockedAt,
            UnlockedBy = evaluation.UnlockedBy,
            UnlockReason = evaluation.UnlockReason,
            Version = evaluation.Version
        };

        Evaluations.Add(entity);
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

        AuditLogs.Add(CoreHrAudit.Entry(actor, action, EntityType, entity.Id, before, after, occurredAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ToDomain(entity);
    }

    /// <summary>Same visibility rule as <c>InterviewRepository</c>: department scope of the job posting, or the actor is a panelist.</summary>
    private IQueryable<InterviewEntity> VisibleInterviews(CoreHrActor actor)
    {
        var interviews = Interviews.AsNoTracking();
        if (actor.DataScope.OrganizationWide)
        {
            return interviews;
        }

        var departmentIds = actor.DataScope.DepartmentIds;
        var userId = actor.UserId;

        return
            from interview in interviews
            join application in Applications.AsNoTracking() on interview.ApplicationId equals application.Id
            join job in JobPostings.AsNoTracking() on application.JobPostingId equals job.Id
            where departmentIds.Contains(job.DepartmentId) ||
                Panelists.Any(p => p.InterviewId == interview.Id && p.UserId == userId)
            select interview;
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private static Evaluation ToDomain(EvaluationEntity entity)
    {
        if (!RecommendationNames.TryParseContract(entity.Recommendation, out var recommendation))
        {
            throw new InvalidOperationException($"evaluations {entity.Id} has unknown recommendation '{entity.Recommendation}'.");
        }

        return new Evaluation(
            entity.Id,
            entity.InterviewId,
            entity.EvaluatorUserId,
            new EvaluationScores(entity.TechnicalScore, entity.CommunicationScore, entity.ProblemSolvingScore, entity.TeamworkScore),
            entity.OverallScore,
            recommendation,
            entity.Feedback,
            entity.SubmittedAt,
            entity.UnlockedAt,
            entity.UnlockedBy,
            entity.UnlockReason,
            entity.Version);
    }
}
