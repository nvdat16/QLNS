using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Intake;

namespace Qlns.BusinessLogic.Modules.Recruitment.Applications;

/// <summary>Filters of <c>GET /recruitment/pipeline</c>. Page and pageSize apply to every column independently.</summary>
public sealed record RecruitmentPipelineQuery(
    long RequisitionId,
    string? Search,
    ApplicationStage? Stage,
    decimal? MinimumAiScore,
    PageRequest Page);

/// <summary>Kanban board of one requisition (OpenAPI RecruitmentPipeline).</summary>
public sealed record RecruitmentPipeline(long RequisitionId, IReadOnlyList<PipelineColumn> Columns);

/// <summary>One Kanban column: the stage, the total after filters, the average AI score (null when none) and one page of cards.</summary>
public sealed record PipelineColumn(
    ApplicationStage Stage,
    int TotalItems,
    decimal? AverageAiScore,
    IReadOnlyList<PipelineCard> Items);

public sealed record PipelineCard(RecruitmentApplication Application, CandidateSummary Candidate);
