using Qlns.BusinessLogic.Modules.Recruitment.Applications;
using Qlns.BusinessLogic.Modules.Recruitment.Requisitions;

namespace Qlns.BusinessLogic.Modules.Recruitment.Intake;

/// <summary>Read model of <c>GET /recruitment/intakes/{intakeId}</c>: the intake plus the resolved duplicate candidates.</summary>
public sealed record CandidateIntakeView(CandidateIntake Intake, IReadOnlyList<CandidateSummary> DuplicateCandidates);

/// <summary>Outcome of the atomic confirm transaction as persisted: the application and its (created or linked) candidate.</summary>
public sealed record IntakeConfirmation(RecruitmentApplication Application, Candidate Candidate);

/// <summary>Result of <c>POST /recruitment/intakes/{intakeId}/confirm</c> (OpenAPI RecruitmentApplicationDetail).</summary>
public sealed record ConfirmedApplication(
    RecruitmentApplication Application,
    Candidate Candidate,
    Requisition Requisition);
