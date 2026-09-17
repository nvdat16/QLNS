using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Recruitment.Interviews;

/// <summary>
/// REC-04.1 use cases: search the interview calendar, schedule an interview with double-booking detection and run
/// the reschedule / complete / cancel workflow. Data scope is enforced by the repository (out of scope ⇒ 404);
/// permissions are enforced here (<see cref="InterviewPermissions.Manage"/>, panelists may complete their own interview).
/// </summary>
public sealed class InterviewService(IInterviewRepository repository, TimeProvider timeProvider)
{
    public const string StageNotInterviewableCode = "recruitment.interview.stage_not_interviewable";
    public const string ManageForbiddenCode = "recruitment.interview.manage_forbidden";

    private const string ResourceName = "Interview";
    private const string ConflictResource = "interview";

    public Task<PagedResult<Interview>> SearchAsync(
        InterviewSearchQuery query,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(actor);
        return repository.SearchAsync(query, actor, cancellationToken);
    }

    public async Task<Interview> ScheduleAsync(ScheduleInterviewCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var actor = command.Actor;

        var application = await repository.GetApplicationAsync(command.Write.ApplicationId, actor, cancellationToken)
            ?? throw new CoreHrNotFoundException("Application", command.Write.ApplicationId);

        RequireManage(actor);

        if (!InterviewApplicationStages.IsInterviewable(application.Stage))
        {
            throw new CoreHrBusinessRuleException(
                StageNotInterviewableCode,
                $"Application {application.Id} is in stage {application.Stage}; interviews can be scheduled only in ai_screening, tech_interview or executive_round.")
            {
                Details = new Dictionary<string, object?> { ["currentStage"] = application.Stage }
            };
        }

        var interview = Interview.Schedule(command.Write, timeProvider.GetUtcNow());

        var activeUsers = await repository.FindActiveUserIdsAsync(interview.PanelUserIds, cancellationToken);
        var unknown = interview.PanelUserIds.Where(userId => !activeUsers.Contains(userId)).ToList();
        if (unknown.Count > 0)
        {
            throw CoreHrValidationException.For(
                "interviewerUserIds",
                $"Interviewer user(s) {string.Join(", ", unknown)} do not exist or are not active.");
        }

        await EnsureNoConflictAsync(interview, excludeInterviewId: null, cancellationToken);

        return await repository.InsertAsync(interview, actor, cancellationToken);
    }

    public async Task<Interview> TransitionAsync(TransitionInterviewCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var actor = command.Actor;

        var interview = await repository.GetByIdAsync(command.InterviewId, actor, cancellationToken)
            ?? throw new CoreHrNotFoundException(ResourceName, command.InterviewId);

        if (interview.Version != command.ExpectedVersion)
        {
            throw new CoreHrConcurrencyConflictException(ConflictResource);
        }

        if (command.Action == InterviewAction.Complete)
        {
            RequireManageOrPanelist(actor, interview);
        }
        else
        {
            RequireManage(actor);
        }

        var now = timeProvider.GetUtcNow();
        var before = interview.Snapshot();

        switch (command.Action)
        {
            case InterviewAction.Reschedule:
                interview.Reschedule(command.Change, now);
                await EnsureNoConflictAsync(interview, interview.Id, cancellationToken);
                break;
            case InterviewAction.Complete:
                interview.Complete(now);
                break;
            case InterviewAction.Cancel:
                interview.Cancel(command.Change?.Reason, now);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(command), "Unknown interview action.");
        }

        var saved = await repository.SaveTransitionAsync(
            interview,
            command.Action,
            before,
            command.ExpectedVersion,
            actor,
            cancellationToken);

        if (!saved)
        {
            throw new CoreHrConcurrencyConflictException(ConflictResource);
        }

        return interview;
    }

    private async Task EnsureNoConflictAsync(Interview interview, long? excludeInterviewId, CancellationToken cancellationToken)
    {
        var overlapping = await repository.ListOverlappingScheduledAsync(
            interview.Slot,
            interview.PanelUserIds,
            interview.Location,
            excludeInterviewId,
            cancellationToken);

        var conflict = InterviewConflictDetector.FindFirst(
            interview.Slot,
            interview.PanelUserIds,
            interview.Location,
            overlapping,
            excludeInterviewId);

        if (conflict is not null)
        {
            throw conflict.ToBusinessRule();
        }
    }

    private static void RequireManage(CoreHrActor actor)
    {
        if (!actor.HasPermission(InterviewPermissions.Manage))
        {
            throw new CoreHrForbiddenException(
                ManageForbiddenCode,
                "Scheduling, rescheduling and cancelling interviews requires the recruitment.interview.manage permission.");
        }
    }

    private static void RequireManageOrPanelist(CoreHrActor actor, Interview interview)
    {
        if (!actor.HasPermission(InterviewPermissions.Manage) && !interview.IsPanelist(actor.UserId))
        {
            throw new CoreHrForbiddenException(
                ManageForbiddenCode,
                "Completing an interview requires the recruitment.interview.manage permission or a seat on its panel.");
        }
    }
}
