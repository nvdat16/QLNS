using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Interviews;

namespace Qlns.BusinessLogic.Modules.Recruitment.Evaluations;

/// <summary>
/// REC-05.1 use cases: list scorecards under the blind-evaluation policy, submit an immutable scorecard as a
/// panelist of a completed interview and unlock a scorecard (HR Manager) by creating a new auditable version.
/// </summary>
public sealed class EvaluationService(
    IEvaluationRepository repository,
    IEvaluationScoringPolicy scoringPolicy,
    TimeProvider timeProvider)
{
    public const string NotPanelistCode = "recruitment.evaluation.not_panelist";
    public const string InterviewNotCompletedCode = "recruitment.evaluation.interview_not_completed";
    public const string AlreadySubmittedCode = "recruitment.evaluation.already_submitted";
    public const string UnlockForbiddenCode = "recruitment.evaluation.unlock_forbidden";
    public const string NotLatestVersionCode = "recruitment.evaluation.not_latest_version";
    public const string AlreadyUnlockedCode = "recruitment.evaluation.already_unlocked";

    private const string InterviewResource = "Interview";
    private const string EvaluationResource = "Evaluation";
    private const string ConflictResource = "evaluation";

    public async Task<IReadOnlyList<Evaluation>> ListAsync(long interviewId, CoreHrActor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var interview = await repository.GetInterviewAsync(interviewId, actor, cancellationToken)
            ?? throw new CoreHrNotFoundException(InterviewResource, interviewId);

        var evaluations = await repository.ListByInterviewAsync(interviewId, cancellationToken);
        return BlindEvaluationPolicy.Apply(actor, interview, evaluations);
    }

    public async Task<Evaluation> SubmitAsync(SubmitEvaluationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var actor = command.Actor;

        var interview = await repository.GetInterviewAsync(command.InterviewId, actor, cancellationToken)
            ?? throw new CoreHrNotFoundException(InterviewResource, command.InterviewId);

        if (!interview.IsPanelist(actor.UserId))
        {
            throw new CoreHrForbiddenException(
                NotPanelistCode,
                "Only interviewers assigned to the interview panel may submit a scorecard.");
        }

        if (interview.Status != InterviewStatus.Completed)
        {
            throw new CoreHrBusinessRuleException(
                InterviewNotCompletedCode,
                $"Interview {interview.Id} is {interview.Status.ToContract()}; scorecards can be submitted only for completed interviews.");
        }

        var latest = await repository.GetLatestAsync(interview.Id, actor.UserId, cancellationToken);
        if (latest is not null && !latest.IsUnlocked)
        {
            throw AlreadySubmitted(latest);
        }

        var version = latest is null ? 1 : latest.Version + 1;
        var evaluation = Evaluation.Submit(interview.Id, actor.UserId, command.Write, version, scoringPolicy, timeProvider.GetUtcNow());

        return await repository.InsertSubmissionAsync(evaluation, actor, cancellationToken)
            ?? throw AlreadySubmitted(evaluation);
    }

    public async Task<Evaluation> UnlockAsync(UnlockEvaluationCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var actor = command.Actor;

        var evaluation = await repository.GetByIdAsync(command.EvaluationId, actor, cancellationToken)
            ?? throw new CoreHrNotFoundException(EvaluationResource, command.EvaluationId);

        if (evaluation.Version != command.ExpectedVersion)
        {
            throw new CoreHrConcurrencyConflictException(ConflictResource);
        }

        if (!actor.HasPermission(EvaluationPermissions.Unlock))
        {
            throw new CoreHrForbiddenException(
                UnlockForbiddenCode,
                "Unlocking a scorecard requires the recruitment.evaluation.unlock permission (HR Manager).");
        }

        var latest = await repository.GetLatestAsync(evaluation.InterviewId, evaluation.EvaluatorUserId, cancellationToken);
        if (latest is null || latest.Id != evaluation.Id)
        {
            throw new CoreHrBusinessRuleException(
                NotLatestVersionCode,
                $"Evaluation {evaluation.Id} is version {evaluation.Version}; only the evaluator's latest version can be unlocked.")
            {
                Details = new Dictionary<string, object?> { ["latestEvaluationId"] = latest?.Id, ["latestVersion"] = latest?.Version }
            };
        }

        var unlocked = evaluation.CreateUnlockedVersion(actor.UserId, command.Reason, timeProvider.GetUtcNow());

        return await repository.InsertUnlockedVersionAsync(unlocked, evaluation.Version, actor, cancellationToken)
            ?? throw new CoreHrConcurrencyConflictException(ConflictResource);
    }

    private static CoreHrBusinessRuleException AlreadySubmitted(Evaluation latest) => new(
        AlreadySubmittedCode,
        $"A scorecard (version {latest.Version}) is already submitted for this interview; submitted scorecards are immutable until unlocked by an HR Manager.")
    {
        Details = new Dictionary<string, object?> { ["evaluationId"] = latest.Id > 0 ? latest.Id : null, ["version"] = latest.Version }
    };
}
