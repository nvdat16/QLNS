using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Interviews;

namespace Qlns.BusinessLogic.Modules.Recruitment.Evaluations;

/// <summary>Payload of <c>POST /recruitment/interviews/{interviewId}/evaluations</c> (OpenAPI EvaluationWrite).</summary>
public sealed record EvaluationWrite(
    decimal TechnicalScore,
    decimal CommunicationScore,
    decimal ProblemSolvingScore,
    decimal TeamworkScore,
    string? Recommendation,
    string? Feedback);

public sealed record SubmitEvaluationCommand(long InterviewId, EvaluationWrite Write, CoreHrActor Actor);

public sealed record UnlockEvaluationCommand(long EvaluationId, long ExpectedVersion, string? Reason, CoreHrActor Actor);

/// <summary>The interview facts the scorecard rules depend on: workflow status and panel membership.</summary>
public sealed record EvaluationInterview(long Id, InterviewStatus Status, IReadOnlyList<long> PanelUserIds)
{
    public bool IsPanelist(long userId) => PanelUserIds.Contains(userId);
}
