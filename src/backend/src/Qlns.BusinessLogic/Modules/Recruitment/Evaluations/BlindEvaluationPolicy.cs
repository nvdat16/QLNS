using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Recruitment.Evaluations;

/// <summary>
/// Blind-evaluation policy (REC-05, story REC-05.1 scenario 2). Actors with
/// <see cref="EvaluationPermissions.ReadAll"/> see every version; a panelist always sees their own versions and sees
/// the other evaluators' latest versions only once their own latest version exists and is locked (not unlocked);
/// anyone else is refused with 403 <see cref="BlindPolicyCode"/>.
/// </summary>
public static class BlindEvaluationPolicy
{
    public const string BlindPolicyCode = "recruitment.evaluation.blind_policy";

    public static IReadOnlyList<Evaluation> Apply(
        CoreHrActor actor,
        EvaluationInterview interview,
        IReadOnlyList<Evaluation> evaluations)
    {
        ArgumentNullException.ThrowIfNull(actor);
        ArgumentNullException.ThrowIfNull(interview);
        ArgumentNullException.ThrowIfNull(evaluations);

        if (actor.HasPermission(EvaluationPermissions.ReadAll))
        {
            return Ordered(evaluations);
        }

        if (!interview.IsPanelist(actor.UserId))
        {
            throw new CoreHrForbiddenException(
                BlindPolicyCode,
                "Only panelists of the interview or holders of recruitment.evaluation.read_all may read its evaluations.");
        }

        var own = evaluations.Where(evaluation => evaluation.EvaluatorUserId == actor.UserId).ToList();
        var ownLatest = own.MaxBy(evaluation => evaluation.Version);
        if (ownLatest is null || ownLatest.IsUnlocked)
        {
            return Ordered(own);
        }

        var othersLatest = evaluations
            .Where(evaluation => evaluation.EvaluatorUserId != actor.UserId)
            .GroupBy(evaluation => evaluation.EvaluatorUserId)
            .Select(group => group.MaxBy(evaluation => evaluation.Version)!);

        return Ordered(own.Concat(othersLatest));
    }

    private static List<Evaluation> Ordered(IEnumerable<Evaluation> evaluations) => evaluations
        .OrderBy(evaluation => evaluation.EvaluatorUserId)
        .ThenBy(evaluation => evaluation.Version)
        .ToList();
}
