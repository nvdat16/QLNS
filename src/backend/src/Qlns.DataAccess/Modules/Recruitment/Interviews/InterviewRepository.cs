using Microsoft.EntityFrameworkCore;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Interviews;
using Qlns.DataAccess.Modules.CoreHr.Shared;
using Qlns.DataAccess.Modules.Recruitment.Shared;

namespace Qlns.DataAccess.Modules.Recruitment.Interviews;

/// <summary>
/// PostgreSQL persistence for interviews + interview_panelists. Every scoped read joins applications → job_postings so
/// the actor's department scope (or panel membership) is applied in SQL; rows outside scope are simply absent.
/// Writes pair the business change with audit_logs and outbox_messages rows in one transaction.
/// </summary>
public sealed class InterviewRepository(QlnsDbContext dbContext) : IInterviewRepository
{
    private const string EntityType = "interview";
    private const string AggregateType = "interview";
    private const string ScheduledMessage = "recruitment.interview.scheduled";
    private const string RescheduledMessage = "recruitment.interview.rescheduled";
    private const string CancelledMessage = "recruitment.interview.cancelled";

    private DbSet<InterviewEntity> Interviews => dbContext.Set<InterviewEntity>();
    private DbSet<InterviewPanelistEntity> Panelists => dbContext.Set<InterviewPanelistEntity>();
    private DbSet<ApplicationEntity> Applications => dbContext.Set<ApplicationEntity>();
    private DbSet<JobPostingEntity> JobPostings => dbContext.Set<JobPostingEntity>();
    private DbSet<UserEntity> Users => dbContext.Set<UserEntity>();
    private DbSet<AuditLogEntity> AuditLogs => dbContext.Set<AuditLogEntity>();
    private DbSet<OutboxMessageEntity> Outbox => dbContext.Set<OutboxMessageEntity>();

    public async Task<PagedResult<Interview>> SearchAsync(
        InterviewSearchQuery query,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        var interviews = Visible(actor);

        if (query.ApplicationId is { } applicationId)
        {
            interviews = interviews.Where(i => i.ApplicationId == applicationId);
        }

        if (query.InterviewerUserId is { } interviewerUserId)
        {
            interviews = interviews.Where(i => Panelists.Any(p => p.InterviewId == i.Id && p.UserId == interviewerUserId));
        }

        if (query.From is { } from)
        {
            interviews = interviews.Where(i => i.EndsAt >= from);
        }

        if (query.To is { } to)
        {
            interviews = interviews.Where(i => i.StartsAt <= to);
        }

        if (query.Status is { } status)
        {
            var statusValue = status.ToContract();
            interviews = interviews.Where(i => i.Status == statusValue);
        }

        var totalItems = await interviews.LongCountAsync(cancellationToken);
        if (totalItems == 0)
        {
            return PagedResult<Interview>.Empty(query.Page);
        }

        var entities = await interviews
            .OrderBy(i => i.StartsAt)
            .ThenBy(i => i.Id)
            .Skip(query.Page.Skip)
            .Take(query.Page.PageSize)
            .ToListAsync(cancellationToken);

        var panels = await LoadPanelsAsync(entities.Select(e => e.Id).ToList(), cancellationToken);
        var items = entities.Select(entity => ToDomain(entity, panels)).ToList();

        return new PagedResult<Interview>(items, query.Page.Page, query.Page.PageSize, totalItems);
    }

    public async Task<Interview?> GetByIdAsync(long interviewId, CoreHrActor actor, CancellationToken cancellationToken)
    {
        var entity = await Visible(actor).SingleOrDefaultAsync(i => i.Id == interviewId, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        var panels = await LoadPanelsAsync([entity.Id], cancellationToken);
        return ToDomain(entity, panels);
    }

    public Task<InterviewApplication?> GetApplicationAsync(long applicationId, CoreHrActor actor, CancellationToken cancellationToken)
    {
        var organizationWide = actor.DataScope.OrganizationWide;
        var departmentIds = actor.DataScope.DepartmentIds;

        return (
            from application in Applications.AsNoTracking()
            join job in JobPostings.AsNoTracking() on application.JobPostingId equals job.Id
            where application.Id == applicationId &&
                (organizationWide || departmentIds.Contains(job.DepartmentId))
            select new InterviewApplication(application.Id, application.CandidateId, application.JobPostingId, job.DepartmentId, application.Stage))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<IReadOnlySet<long>> FindActiveUserIdsAsync(IReadOnlyCollection<long> userIds, CancellationToken cancellationToken)
    {
        if (userIds.Count == 0)
        {
            return new HashSet<long>();
        }

        var ids = userIds.ToList();
        var found = await Users.AsNoTracking()
            .Where(u => ids.Contains(u.Id) && u.Status == "active")
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);

        return found.ToHashSet();
    }

    public async Task<IReadOnlyList<ScheduledInterviewSummary>> ListOverlappingScheduledAsync(
        InterviewSlot slot,
        IReadOnlyCollection<long> panelUserIds,
        string? location,
        long? excludeInterviewId,
        CancellationToken cancellationToken)
    {
        var scheduled = InterviewStatus.Scheduled.ToContract();
        var panel = panelUserIds.ToList();
        var normalizedLocation = string.IsNullOrWhiteSpace(location) ? null : location.Trim().ToLowerInvariant();

        var entities = await Interviews.AsNoTracking()
            .Where(i => i.Status == scheduled &&
                i.StartsAt < slot.EndsAt && slot.StartsAt < i.EndsAt &&
                (excludeInterviewId == null || i.Id != excludeInterviewId) &&
                (Panelists.Any(p => p.InterviewId == i.Id && panel.Contains(p.UserId)) ||
                 (normalizedLocation != null && i.Location != null && i.Location.ToLower() == normalizedLocation)))
            .OrderBy(i => i.StartsAt)
            .ThenBy(i => i.Id)
            .ToListAsync(cancellationToken);

        var panels = await LoadPanelsAsync(entities.Select(e => e.Id).ToList(), cancellationToken);

        return entities
            .Select(entity => new ScheduledInterviewSummary(
                entity.Id,
                new InterviewSlot(entity.StartsAt, entity.EndsAt),
                entity.Location,
                PanelOf(entity, panels)))
            .ToList();
    }

    public async Task<Interview> InsertAsync(Interview interview, CoreHrActor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(interview);
        ArgumentNullException.ThrowIfNull(actor);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var entity = new InterviewEntity
        {
            ApplicationId = interview.ApplicationId,
            InterviewType = interview.InterviewType,
            StartsAt = interview.StartsAt,
            EndsAt = interview.EndsAt,
            Timezone = interview.Timezone,
            InterviewerUserId = interview.LeadInterviewerUserId,
            Location = interview.Location,
            MeetingUrl = interview.MeetingUrl,
            Status = interview.Status.ToContract(),
            CancellationReason = null,
            CreatedAt = interview.CreatedAt,
            UpdatedAt = interview.UpdatedAt,
            Version = interview.Version
        };

        Interviews.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        Panelists.AddRange(interview.PanelUserIds.Select(userId => new InterviewPanelistEntity
        {
            InterviewId = entity.Id,
            UserId = userId
        }));

        var candidateId = await CandidateIdOfAsync(interview.ApplicationId, cancellationToken);

        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            "recruitment.interview.schedule",
            EntityType,
            entity.Id,
            before: null,
            after: AuditSnapshot(interview.Snapshot(), interview.PanelUserIds),
            occurredAt: interview.CreatedAt));

        Outbox.Add(CoreHrOutbox.Message(
            ScheduledMessage,
            AggregateType,
            entity.Id,
            InvitationPayload(entity.Id, interview, candidateId),
            interview.CreatedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return ToDomain(entity, interview.PanelUserIds);
    }

    public async Task<bool> SaveTransitionAsync(
        Interview interview,
        InterviewAction action,
        InterviewSnapshot before,
        long expectedVersion,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(interview);
        ArgumentNullException.ThrowIfNull(before);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var rows = await Interviews
            .Where(i => i.Id == interview.Id && i.Version == expectedVersion)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(i => i.StartsAt, interview.StartsAt)
                    .SetProperty(i => i.EndsAt, interview.EndsAt)
                    .SetProperty(i => i.Timezone, interview.Timezone)
                    .SetProperty(i => i.Location, interview.Location)
                    .SetProperty(i => i.MeetingUrl, interview.MeetingUrl)
                    .SetProperty(i => i.Status, interview.Status.ToContract())
                    .SetProperty(i => i.CancellationReason, interview.CancellationReason)
                    .SetProperty(i => i.Version, interview.Version)
                    .SetProperty(i => i.UpdatedAt, interview.UpdatedAt),
                cancellationToken);

        if (rows != 1)
        {
            await transaction.RollbackAsync(cancellationToken);
            return false;
        }

        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            $"recruitment.interview.{action.ToContract()}",
            EntityType,
            interview.Id,
            before: AuditSnapshot(before, interview.PanelUserIds),
            after: AuditSnapshot(interview.Snapshot(), interview.PanelUserIds, interview.CancellationReason),
            occurredAt: interview.UpdatedAt));

        if (action is InterviewAction.Reschedule or InterviewAction.Cancel)
        {
            var candidateId = await CandidateIdOfAsync(interview.ApplicationId, cancellationToken);
            var payload = InvitationPayload(interview.Id, interview, candidateId, before, interview.CancellationReason);
            Outbox.Add(CoreHrOutbox.Message(
                action == InterviewAction.Reschedule ? RescheduledMessage : CancelledMessage,
                AggregateType,
                interview.Id,
                payload,
                interview.UpdatedAt));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return true;
    }

    /// <summary>
    /// Interviews the actor may see: organization-wide scope, the job posting's department is in the actor's set,
    /// or the actor sits on the panel (Interviewer persona with self scope).
    /// </summary>
    private IQueryable<InterviewEntity> Visible(CoreHrActor actor)
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

    private async Task<ILookup<long, long>> LoadPanelsAsync(IReadOnlyCollection<long> interviewIds, CancellationToken cancellationToken)
    {
        if (interviewIds.Count == 0)
        {
            return Array.Empty<InterviewPanelistEntity>().ToLookup(p => p.InterviewId, p => p.UserId);
        }

        var ids = interviewIds.ToList();
        var rows = await Panelists.AsNoTracking()
            .Where(p => ids.Contains(p.InterviewId))
            .OrderBy(p => p.UserId)
            .ToListAsync(cancellationToken);

        return rows.ToLookup(p => p.InterviewId, p => p.UserId);
    }

    private Task<long> CandidateIdOfAsync(long applicationId, CancellationToken cancellationToken) =>
        Applications.AsNoTracking()
            .Where(a => a.Id == applicationId)
            .Select(a => a.CandidateId)
            .SingleAsync(cancellationToken);

    /// <summary>Lead interviewer first, then the remaining panelists by user id.</summary>
    private static IReadOnlyList<long> PanelOf(InterviewEntity entity, ILookup<long, long> panels)
    {
        var members = panels[entity.Id].Where(userId => userId != entity.InterviewerUserId).ToList();
        members.Insert(0, entity.InterviewerUserId);
        return members;
    }

    /// <summary>Calendar payload consumed by the outbox worker, which renders the .ics invitation. No restricted data.</summary>
    private static object InvitationPayload(
        long interviewId,
        Interview interview,
        long candidateId,
        InterviewSnapshot? previous = null,
        string? reason = null) => new
    {
        interviewId,
        applicationId = interview.ApplicationId,
        candidateId,
        interviewType = interview.InterviewType,
        startsAt = interview.StartsAt,
        endsAt = interview.EndsAt,
        timezone = interview.Timezone,
        panelUserIds = interview.PanelUserIds,
        location = interview.Location,
        meetingUrl = interview.MeetingUrl,
        status = interview.Status.ToContract(),
        previousStartsAt = previous?.Slot.StartsAt,
        previousEndsAt = previous?.Slot.EndsAt,
        reason
    };

    private static object AuditSnapshot(InterviewSnapshot snapshot, IReadOnlyList<long> panelUserIds, string? reason = null) => new
    {
        status = snapshot.Status.ToContract(),
        startsAt = snapshot.Slot.StartsAt,
        endsAt = snapshot.Slot.EndsAt,
        timezone = snapshot.Timezone,
        location = snapshot.Location,
        meetingUrl = snapshot.MeetingUrl,
        panelUserIds,
        version = snapshot.Version,
        reason
    };

    private static Interview ToDomain(InterviewEntity entity, ILookup<long, long> panels) => ToDomain(entity, PanelOf(entity, panels));

    private static Interview ToDomain(InterviewEntity entity, IReadOnlyList<long> panelUserIds)
    {
        if (!InterviewStatusNames.TryParseContract(entity.Status, out var status))
        {
            throw new InvalidOperationException($"interviews {entity.Id} has unknown status '{entity.Status}'.");
        }

        return new Interview(
            entity.Id,
            entity.ApplicationId,
            entity.InterviewType,
            new InterviewSlot(entity.StartsAt, entity.EndsAt),
            entity.Timezone,
            panelUserIds,
            entity.Location,
            entity.MeetingUrl,
            status,
            entity.CancellationReason,
            entity.Version,
            entity.CreatedAt,
            entity.UpdatedAt);
    }
}
