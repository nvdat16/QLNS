using System.ComponentModel.DataAnnotations;
using Qlns.Api.Modules.Recruitment.Intake;
using Qlns.BusinessLogic.Modules.Recruitment.Applications;

namespace Qlns.Api.Modules.Recruitment.Applications;

/// <summary>OpenAPI AdvanceApplicationRequest.</summary>
public sealed record AdvanceApplicationRequest(
    [Required, MaxLength(40)] string TargetStage,
    [MaxLength(RecruitmentApplication.ReasonMaxLength)] string? Reason);

/// <summary>OpenAPI ReasonRequest. The reason is mandatory for reject/withdraw; a blank value is a 422 from the domain.</summary>
public sealed record ReasonRequest([MaxLength(RecruitmentApplication.ReasonMaxLength)] string? Reason);

/// <summary>OpenAPI RecruitmentApplication.</summary>
public sealed record RecruitmentApplicationResponse(
    long Id,
    long CandidateId,
    long JobPostingId,
    long? ResumeId,
    string Stage,
    decimal? AiScore,
    string Source,
    DateTimeOffset AppliedAt,
    long Version,
    DateTimeOffset UpdatedAt)
{
    public static RecruitmentApplicationResponse From(RecruitmentApplication application) => new(
        application.Id,
        application.CandidateId,
        application.JobPostingId,
        application.ResumeId,
        application.Stage.ToContract(),
        application.AiScore,
        application.Source,
        application.AppliedAt,
        application.Version,
        application.UpdatedAt);
}

/// <summary>OpenAPI PipelineCard.</summary>
public sealed record PipelineCardResponse(RecruitmentApplicationResponse Application, CandidateSummaryResponse Candidate)
{
    public static PipelineCardResponse From(PipelineCard card) => new(
        RecruitmentApplicationResponse.From(card.Application),
        CandidateSummaryResponse.From(card.Candidate));
}

/// <summary>OpenAPI PipelineColumn.</summary>
public sealed record PipelineColumnResponse(
    string Stage,
    int TotalItems,
    decimal? AverageAiScore,
    IReadOnlyList<PipelineCardResponse> Items)
{
    public static PipelineColumnResponse From(PipelineColumn column) => new(
        column.Stage.ToContract(),
        column.TotalItems,
        column.AverageAiScore,
        column.Items.Select(PipelineCardResponse.From).ToList());
}

/// <summary>OpenAPI RecruitmentPipeline.</summary>
public sealed record RecruitmentPipelineResponse(long RequisitionId, IReadOnlyList<PipelineColumnResponse> Columns)
{
    public static RecruitmentPipelineResponse From(RecruitmentPipeline pipeline) => new(
        pipeline.RequisitionId,
        pipeline.Columns.Select(PipelineColumnResponse.From).ToList());
}
