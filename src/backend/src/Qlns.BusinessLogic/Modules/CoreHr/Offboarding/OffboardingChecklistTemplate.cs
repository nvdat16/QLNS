namespace Qlns.BusinessLogic.Modules.CoreHr.Offboarding;

/// <summary>
/// Checklist template applied when an offboarding case is approved (EMP-07.1, SRS EMP-07 step 3). One task per
/// template key across the five categories; keys already present on the case are skipped so regeneration never
/// duplicates a task (ux_offboarding_task_template). Tasks in the <c>manager</c> category are assigned to the
/// employee's line manager when that manager has a user account; every other task starts unassigned.
/// </summary>
public static class OffboardingChecklistTemplate
{
    public sealed record Item(
        string TemplateKey,
        OffboardingTaskCategory Category,
        string TaskName,
        string Description,
        bool BlocksLastWorkingDay);

    public static IReadOnlyList<Item> Items { get; } =
    [
        new("it.devices", OffboardingTaskCategory.It, "Recover laptop and devices",
            "Collect laptop, monitors, phone, tokens and other company hardware.", BlocksLastWorkingDay: true),
        new("it.accounts", OffboardingTaskCategory.It, "Disable accounts",
            "Disable email, chat, source control and ERP access on the last working date, not earlier.", BlocksLastWorkingDay: true),
        new("it.repositories", OffboardingTaskCategory.It, "Transfer repositories and documents",
            "Transfer ownership of repositories, shared drives and documents to the handover employee.", BlocksLastWorkingDay: false),
        new("admin.badge", OffboardingTaskCategory.Admin, "Collect badge and parking card",
            "Collect the employee badge and parking card.", BlocksLastWorkingDay: true),
        new("admin.workspace", OffboardingTaskCategory.Admin, "Hand over workspace",
            "Clear and hand over the desk, locker and keys.", BlocksLastWorkingDay: false),
        new("hr.exit_interview", OffboardingTaskCategory.Hr, "Exit interview",
            "Conduct and record the exit interview.", BlocksLastWorkingDay: false),
        new("hr.termination_decision", OffboardingTaskCategory.Hr, "Issue termination decision",
            "Prepare and sign the termination decision.", BlocksLastWorkingDay: false),
        new("hr.insurance_book", OffboardingTaskCategory.Hr, "Close insurance book and return records",
            "Close the social insurance book and return original personal records.", BlocksLastWorkingDay: false),
        new("manager.handover", OffboardingTaskCategory.Manager, "Confirm work handover",
            "Confirm that work, documents and contacts were handed over to the receiving employee.", BlocksLastWorkingDay: true),
        new("finance.settlement", OffboardingTaskCategory.Finance, "Settle outstanding amounts",
            "Settle debts, advances and remaining payments due at termination.", BlocksLastWorkingDay: true)
    ];

    /// <summary>Tasks are due at the end of the last working day (UTC).</summary>
    public static DateTimeOffset DueAt(DateOnly lastWorkingDate) =>
        new(lastWorkingDate.ToDateTime(new TimeOnly(23, 59, 59)), TimeSpan.Zero);

    /// <summary>
    /// Builds the unsaved tasks (id 0, pending, version 1) for every template key not in
    /// <paramref name="existingTemplateKeys"/>.
    /// </summary>
    public static IReadOnlyList<OffboardingTask> Generate(
        long offboardingCaseId,
        DateOnly lastWorkingDate,
        long? managerUserId,
        IReadOnlySet<string> existingTemplateKeys,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(existingTemplateKeys);

        var dueAt = DueAt(lastWorkingDate);
        return Items
            .Where(item => !existingTemplateKeys.Contains(item.TemplateKey))
            .Select(item => new OffboardingTask(
                id: 0,
                offboardingCaseId,
                item.TemplateKey,
                item.Category,
                item.TaskName,
                item.Description,
                assignedToUserId: item.Category == OffboardingTaskCategory.Manager ? managerUserId : null,
                dueAt,
                item.BlocksLastWorkingDay,
                OffboardingTaskStatus.Pending,
                completedAt: null,
                version: 1,
                createdAt: now,
                updatedAt: now))
            .ToList();
    }
}
