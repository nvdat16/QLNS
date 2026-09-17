using System.Globalization;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.Recruitment.Offers;

/// <summary>Application row as read inside the handoff (stage and version are the conditional-update guard).</summary>
public sealed record HandoffApplication(long Id, long CandidateId, long JobPostingId, string Stage, long Version);

/// <summary>Candidate identity copied into the new employee record.</summary>
public sealed record HandoffCandidate(string FirstName, string LastName, string Email, string? Phone);

/// <summary>Job posting placement copied into the new employee record.</summary>
public sealed record HandoffJobPosting(long DepartmentId, long? PositionId);

/// <summary>Everything the handoff reads besides the offer itself.</summary>
public sealed record OfferHandoffContext(
    HandoffApplication Application,
    HandoffCandidate Candidate,
    HandoffJobPosting JobPosting);

/// <summary>Conditional stage change of the application, written in the same transaction as the acceptance.</summary>
public sealed record ApplicationHireTransition(
    long ApplicationId,
    long CandidateId,
    string FromStage,
    string ToStage,
    long ExpectedVersion,
    long NewVersion);

/// <summary>New <c>employees</c> row without its generated code (assigned from <c>employee_code_seq</c> inside the transaction).</summary>
public sealed record NewEmployee(
    long SourceApplicationId,
    string FirstName,
    string LastName,
    string? PersonalEmail,
    string? Phone,
    long DepartmentId,
    long PositionId,
    DateOnly HireDate,
    string Status);

/// <summary>Terms of the initial probation contract (<c>contracts</c>, status draft).</summary>
public sealed record ProbationContractTerms(
    string ContractType,
    DateOnly StartDate,
    DateOnly EndDate,
    decimal Salary,
    string Currency,
    int NoticePeriodDays,
    bool IsPrimary,
    string Status);

/// <summary>One onboarding checklist item generated from the template (<c>onboarding_tasks</c>).</summary>
public sealed record OnboardingTaskBlueprint(string TemplateKey, string TaskName, string? Description, DateTimeOffset DueAt);

/// <summary>Everything the repository writes when a candidate accepts, besides the offer row itself.</summary>
public sealed record OfferHandoffPlan(
    ApplicationHireTransition Application,
    NewEmployee Employee,
    ProbationContractTerms Contract,
    IReadOnlyList<OnboardingTaskBlueprint> Tasks);

/// <summary>Identifiers created (or found) by the handoff; the body of OfferResponseResult on accept.</summary>
public sealed record OfferHandoffResult(long EmployeeId, long? InitialContractId, IReadOnlyList<long> OnboardingTaskIds);

/// <summary>
/// Candidate-to-employee handoff rules (REC-06.2, sequence diagram §4): what is created when an offer is accepted.
/// Pure domain — the repository executes the plan in one transaction and the unique index on
/// <c>employees.source_application_id</c> guarantees at most one employee per application.
/// </summary>
public static class OfferHandoff
{
    public const string PositionRequiredCode = "recruitment.offer.position_required";
    public const string ApplicationNotInOfferStageCode = "recruitment.offer.application_not_in_offer_stage";

    public const string ProbationContractType = "probation";
    public const string DraftContractStatus = "draft";
    public const string ProbationEmployeeStatus = "probation";
    public const int ProbationDays = 60;
    public const int ProbationNoticePeriodDays = 3;

    public static OfferHandoffPlan Plan(Offer offer, OfferHandoffContext context)
    {
        ArgumentNullException.ThrowIfNull(offer);
        ArgumentNullException.ThrowIfNull(context);

        if (context.Application.Id != offer.ApplicationId)
        {
            throw new ArgumentException("Handoff context belongs to a different application.", nameof(context));
        }

        if (context.Application.Stage != OfferApplicationStages.OfferLetter)
        {
            throw new CoreHrBusinessRuleException(
                ApplicationNotInOfferStageCode,
                $"Application {context.Application.Id} is in stage {context.Application.Stage}; only applications in offer_letter can be hired.")
            {
                Details = new Dictionary<string, object?> { ["currentStage"] = context.Application.Stage }
            };
        }

        if (context.JobPosting.PositionId is not { } positionId)
        {
            throw new CoreHrBusinessRuleException(
                PositionRequiredCode,
                $"Job posting {context.Application.JobPostingId} has no position; assign one before the candidate can be hired.");
        }

        var application = new ApplicationHireTransition(
            context.Application.Id,
            context.Application.CandidateId,
            context.Application.Stage,
            OfferApplicationStages.HiredReady,
            context.Application.Version,
            context.Application.Version + 1);

        var employee = new NewEmployee(
            offer.ApplicationId,
            context.Candidate.FirstName,
            context.Candidate.LastName,
            context.Candidate.Email,
            context.Candidate.Phone,
            context.JobPosting.DepartmentId,
            positionId,
            offer.StartDate,
            ProbationEmployeeStatus);

        var contract = new ProbationContractTerms(
            ProbationContractType,
            offer.StartDate,
            offer.StartDate.AddDays(ProbationDays),
            offer.BaseSalary,
            offer.Currency,
            ProbationNoticePeriodDays,
            IsPrimary: true,
            DraftContractStatus);

        return new OfferHandoffPlan(application, employee, contract, OnboardingChecklistTemplate.ForStart(offer.StartDate));
    }

    /// <summary>Formats a value of <c>employee_code_seq</c> as the employee code (<c>EMP-00128</c>).</summary>
    public static string EmployeeCode(long sequenceValue)
    {
        if (sequenceValue <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sequenceValue));
        }

        return string.Create(CultureInfo.InvariantCulture, $"EMP-{sequenceValue:D5}");
    }

    /// <summary>Contract number of the initial probation contract (<c>HD-TV-EMP-00128</c>).</summary>
    public static string ProbationContractNumber(string employeeCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(employeeCode);
        return $"HD-TV-{employeeCode}";
    }
}

/// <summary>
/// Default onboarding checklist (EMP-03). Due dates are relative to the start date at 09:00 UTC; assignees are left
/// empty for the HR Officer to dispatch. A per-department / per-position template is a later extension.
/// </summary>
public static class OnboardingChecklistTemplate
{
    private static readonly TimeOnly DueTime = new(9, 0);

    private static readonly (string Key, string Name, string? Description, int DayOffset)[] Items =
    [
        ("it.email", "Tạo email công vụ", "Tạo mailbox và cấp quyền Slack/Git", -1),
        ("it.laptop", "Chuẩn bị laptop", null, -1),
        ("admin.badge", "Cấp thẻ nhân viên", null, 0),
        ("hr.contract", "Chuẩn bị hợp đồng thử việc", null, -2),
        ("manager.buddy", "Bố trí buddy/mentor", null, 3)
    ];

    public static IReadOnlyList<string> TemplateKeys => Items.Select(item => item.Key).ToList();

    public static IReadOnlyList<OnboardingTaskBlueprint> ForStart(DateOnly startDate) => Items
        .Select(item => new OnboardingTaskBlueprint(
            item.Key,
            item.Name,
            item.Description,
            new DateTimeOffset(startDate.AddDays(item.DayOffset).ToDateTime(DueTime), TimeSpan.Zero)))
        .ToList();
}
