using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Recruitment.Interviews;

public sealed record ScheduleInterviewCommand(InterviewWrite Write, CoreHrActor Actor);

public sealed record TransitionInterviewCommand(
    long InterviewId,
    InterviewAction Action,
    long ExpectedVersion,
    InterviewChange? Change,
    CoreHrActor Actor);

/// <summary>
/// Filters of <c>GET /recruitment/interviews</c>. <see cref="From"/>/<see cref="To"/> select interviews whose slot
/// overlaps the closed window. Data scope is applied server-side from the actor, never from the query.
/// </summary>
public sealed record InterviewSearchQuery(
    long? ApplicationId,
    long? InterviewerUserId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    InterviewStatus? Status,
    PageRequest Page);
