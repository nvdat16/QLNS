using System.ComponentModel.DataAnnotations;
using Qlns.BusinessLogic.Modules.CoreHr.Offboarding;

namespace Qlns.Api.Modules.CoreHr.Offboarding;

/// <summary>OpenAPI OffboardingCaseWrite. Semantic rules (dates, handover, eligibility) are enforced by the domain and service.</summary>
public sealed record OffboardingCaseWriteRequest(
    [Range(1, long.MaxValue)] long EmployeeId,
    [Required, MaxLength(40)] string SeparationType,
    DateOnly? NoticeReceivedOn,
    [Required] DateOnly? LastWorkingDate,
    long? HandoverToEmployeeId,
    [Required, MaxLength(OffboardingCase.ReasonMaxLength)] string Reason)
{
    public OffboardingCaseWrite ToWrite() => new(
        EmployeeId,
        SeparationType,
        NoticeReceivedOn,
        LastWorkingDate ?? default,
        HandoverToEmployeeId,
        Reason);
}

/// <summary>OpenAPI DecisionRequest (optional body of the transition endpoints).</summary>
public sealed record DecisionRequest([MaxLength(1000)] string? Reason);

/// <summary>OpenAPI OffboardingCase.</summary>
public sealed record OffboardingCaseResponse(
    long Id,
    long EmployeeId,
    long? EmployeeEventId,
    string SeparationType,
    DateOnly? NoticeReceivedOn,
    DateOnly LastWorkingDate,
    long? HandoverToEmployeeId,
    string Reason,
    DateTimeOffset? ExitInterviewAt,
    string FinalSettlementStatus,
    string Status,
    int BlockingTasksOutstanding,
    int? NoticePeriodShortfallDays,
    long? ApprovedBy,
    DateTimeOffset? ApprovedAt,
    DateTimeOffset? CompletedAt,
    long Version,
    DateTimeOffset CreatedAt)
{
    public static OffboardingCaseResponse From(OffboardingCaseView view)
    {
        ArgumentNullException.ThrowIfNull(view);
        var offboardingCase = view.Case;
        return new OffboardingCaseResponse(
            offboardingCase.Id,
            offboardingCase.EmployeeId,
            offboardingCase.EmployeeEventId,
            offboardingCase.SeparationType.ToContract(),
            offboardingCase.NoticeReceivedOn,
            offboardingCase.LastWorkingDate,
            offboardingCase.HandoverToEmployeeId,
            offboardingCase.Reason,
            offboardingCase.ExitInterviewAt,
            offboardingCase.FinalSettlementStatus.ToContract(),
            offboardingCase.Status.ToContract(),
            view.BlockingTasksOutstanding,
            view.NoticePeriodShortfallDays,
            offboardingCase.ApprovedBy,
            offboardingCase.ApprovedAt,
            offboardingCase.CompletedAt,
            offboardingCase.Version,
            offboardingCase.CreatedAt);
    }
}

/// <summary>OpenAPI OffboardingTask.</summary>
public sealed record OffboardingTaskResponse(
    long Id,
    long OffboardingCaseId,
    string TemplateKey,
    string Category,
    string TaskName,
    string? Description,
    long? AssignedToUserId,
    DateTimeOffset? DueAt,
    bool BlocksLastWorkingDay,
    string Status,
    DateTimeOffset? CompletedAt,
    long Version)
{
    public static OffboardingTaskResponse From(OffboardingTask task)
    {
        ArgumentNullException.ThrowIfNull(task);
        return new OffboardingTaskResponse(
            task.Id,
            task.OffboardingCaseId,
            task.TemplateKey,
            task.Category.ToContract(),
            task.TaskName,
            task.Description,
            task.AssignedToUserId,
            task.DueAt,
            task.BlocksLastWorkingDay,
            task.Status.ToContract(),
            task.CompletedAt,
            task.Version);
    }
}
