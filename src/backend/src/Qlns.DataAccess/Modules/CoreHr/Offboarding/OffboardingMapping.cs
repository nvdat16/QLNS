using Qlns.BusinessLogic.Modules.CoreHr.EmployeeEvents;
using Qlns.BusinessLogic.Modules.CoreHr.Offboarding;
using Qlns.DataAccess.Modules.CoreHr.Shared;

namespace Qlns.DataAccess.Modules.CoreHr.Offboarding;

/// <summary>
/// Entity ↔ domain mapping shared by the offboarding repositories and by the probation repository (which opens an
/// offboarding case on a terminated decision). Unknown enum strings are data corruption and throw.
/// </summary>
internal static class OffboardingMapping
{
    /// <summary>offboarding_cases.status values covered by ux_offboarding_open_case.</summary>
    public static readonly string[] OpenCaseStatuses =
    [
        OffboardingCaseStatus.Draft.ToContract(),
        OffboardingCaseStatus.PendingApproval.ToContract(),
        OffboardingCaseStatus.Approved.ToContract(),
        OffboardingCaseStatus.InProgress.ToContract()
    ];

    public static OffboardingCase ToDomain(OffboardingCaseEntity entity)
    {
        if (!SeparationTypeNames.TryParseContract(entity.SeparationType, out var separationType))
        {
            throw new InvalidOperationException($"offboarding_cases {entity.Id} has unknown separation_type '{entity.SeparationType}'.");
        }

        if (!FinalSettlementStatusNames.TryParseContract(entity.FinalSettlementStatus, out var settlement))
        {
            throw new InvalidOperationException($"offboarding_cases {entity.Id} has unknown final_settlement_status '{entity.FinalSettlementStatus}'.");
        }

        return new OffboardingCase(
            entity.Id,
            entity.EmployeeId,
            entity.EmployeeEventId,
            separationType,
            entity.NoticeReceivedOn,
            entity.LastWorkingDate,
            entity.HandoverToEmployeeId,
            entity.ExitInterviewAt,
            settlement,
            ParseCaseStatus(entity.Id, entity.Status),
            entity.Reason,
            entity.CreatedBy,
            entity.ApprovedBy,
            entity.ApprovedAt,
            entity.CompletedAt,
            entity.Version,
            entity.CreatedAt,
            entity.UpdatedAt);
    }

    public static OffboardingCaseStatus ParseCaseStatus(long caseId, string value)
    {
        if (!OffboardingCaseStatusNames.TryParseContract(value, out var status))
        {
            throw new InvalidOperationException($"offboarding_cases {caseId} has unknown status '{value}'.");
        }

        return status;
    }

    public static OffboardingCaseEntity ToEntity(OffboardingCase offboardingCase) => new()
    {
        EmployeeId = offboardingCase.EmployeeId,
        EmployeeEventId = offboardingCase.EmployeeEventId,
        SeparationType = offboardingCase.SeparationType.ToContract(),
        NoticeReceivedOn = offboardingCase.NoticeReceivedOn,
        LastWorkingDate = offboardingCase.LastWorkingDate,
        HandoverToEmployeeId = offboardingCase.HandoverToEmployeeId,
        ExitInterviewAt = offboardingCase.ExitInterviewAt,
        FinalSettlementStatus = offboardingCase.FinalSettlementStatus.ToContract(),
        Status = offboardingCase.Status.ToContract(),
        Reason = offboardingCase.Reason,
        CreatedBy = offboardingCase.CreatedBy,
        ApprovedBy = offboardingCase.ApprovedBy,
        ApprovedAt = offboardingCase.ApprovedAt,
        CompletedAt = offboardingCase.CompletedAt,
        CreatedAt = offboardingCase.CreatedAt,
        UpdatedAt = offboardingCase.UpdatedAt,
        Version = offboardingCase.Version
    };

    public static OffboardingTask ToDomain(OffboardingTaskEntity entity)
    {
        if (!OffboardingTaskCategoryNames.TryParseContract(entity.Category, out var category))
        {
            throw new InvalidOperationException($"offboarding_tasks {entity.Id} has unknown category '{entity.Category}'.");
        }

        if (!OffboardingTaskStatusNames.TryParseContract(entity.Status, out var status))
        {
            throw new InvalidOperationException($"offboarding_tasks {entity.Id} has unknown status '{entity.Status}'.");
        }

        return new OffboardingTask(
            entity.Id,
            entity.OffboardingCaseId,
            entity.TemplateKey,
            category,
            entity.TaskName,
            entity.Description,
            entity.AssignedToUserId,
            entity.DueAt,
            entity.BlocksLastWorkingDay,
            status,
            entity.CompletedAt,
            entity.Version,
            entity.CreatedAt,
            entity.UpdatedAt);
    }

    public static OffboardingTaskEntity ToEntity(OffboardingTask task) => new()
    {
        OffboardingCaseId = task.OffboardingCaseId,
        TemplateKey = task.TemplateKey,
        Category = task.Category.ToContract(),
        TaskName = task.TaskName,
        Description = task.Description,
        AssignedToUserId = task.AssignedToUserId,
        DueAt = task.DueAt,
        BlocksLastWorkingDay = task.BlocksLastWorkingDay,
        Status = task.Status.ToContract(),
        CompletedAt = task.CompletedAt,
        CreatedAt = task.CreatedAt,
        UpdatedAt = task.UpdatedAt,
        Version = task.Version
    };

    /// <summary>employee_events row in status approved, created and approved by the deciding actor.</summary>
    public static EmployeeEventEntity ToApprovedEventEntity(ApprovedEmployeeEvent employeeEvent, long actorUserId, DateTimeOffset now) => new()
    {
        EmployeeId = employeeEvent.EmployeeId,
        EventType = employeeEvent.EventType.ToContract(),
        Status = EmployeeEventStatus.Approved.ToContract(),
        EffectiveDate = employeeEvent.EffectiveDate,
        BeforeData = employeeEvent.BeforeData.ToJsonString(),
        AfterData = employeeEvent.AfterData.ToJsonString(),
        Reason = employeeEvent.Reason,
        CompensatesEventId = null,
        CreatedBy = actorUserId,
        ApprovedBy = actorUserId,
        ApprovedAt = now,
        AppliedAt = null,
        CreatedAt = now,
        UpdatedAt = now,
        Version = 1
    };

    /// <summary>Audit payload of a generated approved event: statuses and field names only.</summary>
    public static object ApprovedEventAuditSnapshot(ApprovedEmployeeEvent employeeEvent, string sourceEntityType, long sourceEntityId) => new
    {
        status = EmployeeEventStatus.Approved.ToContract(),
        version = 1,
        eventType = employeeEvent.EventType.ToContract(),
        fields = employeeEvent.ChangedFields,
        effectiveDate = employeeEvent.EffectiveDate,
        source = new { entityType = sourceEntityType, entityId = sourceEntityId }
    };
}
