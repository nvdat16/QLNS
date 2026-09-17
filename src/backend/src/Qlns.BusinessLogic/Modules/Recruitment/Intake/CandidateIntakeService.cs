using Qlns.BusinessLogic.Modules.CoreHr.EmployeeDocuments;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Applications;
using Qlns.BusinessLogic.Modules.Recruitment.Requisitions;

namespace Qlns.BusinessLogic.Modules.Recruitment.Intake;

/// <summary>
/// Use cases of REC-02.1/REC-02.2: scan, store and parse an uploaded résumé; expose the intake state; confirm the
/// reviewed candidate data, resolve duplicates and create the application atomically. Data scope is enforced
/// through the requisition's department (404 outside scope). Nothing is stored before the malware scan is clean.
/// </summary>
public sealed class CandidateIntakeService(
    ICandidateIntakeRepository repository,
    IDocumentStorage storage,
    IMalwareScanner scanner,
    IResumeParser parser,
    TimeProvider timeProvider)
{
    private const string RequisitionResourceName = "Requisition";
    private const string IntakeResourceName = "Intake";

    public const string WriteForbiddenCode = "recruitment.intake.write_forbidden";
    public const string ApplicationAlreadyExistsCode = "recruitment.application.already_exists";

    public async Task<CandidateIntakeView> StartAsync(StartIntakeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var actor = command.Actor;
        RequireWrite(actor);

        var requisition = await repository.GetVisibleRequisitionAsync(command.RequisitionId, actor, cancellationToken)
            ?? throw new CoreHrNotFoundException(RequisitionResourceName, command.RequisitionId);

        ResumeUploadRules.Validate(
            command.FileName,
            command.ContentType,
            command.SizeBytes,
            command.PrivacyNoticeVersion,
            command.Consented);

        if (requisition.Status != RequisitionStatus.ActiveRecruiting)
        {
            throw new CoreHrBusinessRuleException(
                CandidateIntake.RequisitionNotOpenCode,
                $"Résumés can only be submitted to an active_recruiting requisition; this one is {requisition.Status.ToContract()}.")
            {
                Details = new Dictionary<string, object?> { ["currentStatus"] = requisition.Status.ToContract() }
            };
        }

        switch (await scanner.ScanAsync(command.Content, command.FileName, cancellationToken))
        {
            case MalwareScanResult.Infected:
                throw CoreHrValidationException.For("file", "The file failed the malware scan.");
            case MalwareScanResult.Unavailable:
                throw new CoreHrBusinessRuleException(
                    CandidateIntake.ScanUnavailableCode,
                    "The malware scanner is unavailable; the résumé was not stored. Retry later.");
            case MalwareScanResult.Clean:
                break;
            default:
                throw new InvalidOperationException("Unknown malware scan result.");
        }

        var contentType = DocumentUploadRules.NormalizeContentType(command.ContentType)!;
        Rewind(command.Content);
        var parse = await parser.ParseAsync(command.Content, command.FileName, contentType, cancellationToken);

        var now = timeProvider.GetUtcNow();
        var objectKey = CandidateIntake.BuildObjectKey(requisition.Id);
        var intake = CandidateIntake.Start(
            requisition.Id,
            objectKey,
            command.FileName.Trim(),
            contentType,
            command.SizeBytes,
            parse,
            command.PrivacyNoticeVersion!,
            actor.UserId,
            now);

        Rewind(command.Content);
        await storage.StoreAsync(objectKey, command.Content, contentType, cancellationToken);

        try
        {
            var persisted = await repository.InsertAsync(intake, actor, cancellationToken);
            return new CandidateIntakeView(persisted, []);
        }
        catch
        {
            await TryDeleteStoredObjectAsync(objectKey);
            throw;
        }
    }

    public async Task<CandidateIntakeView> GetAsync(Guid intakeId, CoreHrActor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var intake = await repository.GetByIntakeIdAsync(intakeId, actor, cancellationToken)
            ?? throw IntakeNotFound();

        return new CandidateIntakeView(intake, await LoadDuplicateSummariesAsync(intake, cancellationToken));
    }

    public async Task<ConfirmedApplication> ConfirmAsync(ConfirmIntakeCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var actor = command.Actor;
        RequireWrite(actor);

        var intake = await repository.GetByIntakeIdAsync(command.IntakeId, actor, cancellationToken)
            ?? throw IntakeNotFound();

        var requisition = await repository.GetVisibleRequisitionAsync(intake.RequisitionId, actor, cancellationToken)
            ?? throw new CoreHrNotFoundException(RequisitionResourceName, intake.RequisitionId);

        if (intake.Status == IntakeStatus.Completed)
        {
            // Natural idempotency: a retried confirm returns the application the first call created.
            var existing = await repository.GetConfirmationAsync(intake, cancellationToken)
                ?? throw new InvalidOperationException($"Intake {intake.IntakeId} is completed but has no application.");
            return new ConfirmedApplication(existing.Application, existing.Candidate, requisition);
        }

        if (!intake.IsConfirmable)
        {
            throw new CoreHrBusinessRuleException(
                CandidateIntake.NotConfirmableCode,
                $"Intake is {intake.Status.ToContract()} and cannot be confirmed.")
            {
                Details = new Dictionary<string, object?> { ["currentStatus"] = intake.Status.ToContract() }
            };
        }

        var now = timeProvider.GetUtcNow();
        var source = RecruitmentApplication.NormalizeSource(command.Source);
        var proposed = Candidate.Create(command.Candidate, intake.PrivacyNoticeVersion, intake.ConsentedAt, now);

        var duplicates = await repository.FindDuplicatesAsync(proposed.NormalizedEmail, proposed.NormalizedPhone, cancellationToken);

        Candidate candidate;
        long? expectedCandidateVersion;
        if (command.ExistingCandidateId is { } existingCandidateId)
        {
            var chosen = duplicates.SingleOrDefault(duplicate => duplicate.Id == existingCandidateId)
                ?? throw CoreHrValidationException.For(
                    "existingCandidateId",
                    "existingCandidateId must be one of the candidates detected as possible duplicates.");

            expectedCandidateVersion = chosen.Version;
            chosen.Refresh(command.Candidate, now);
            candidate = chosen;
        }
        else if (duplicates.Count > 0)
        {
            intake.MarkDuplicateReview(duplicates.Select(duplicate => duplicate.Id).ToList());
            await repository.SaveDuplicateReviewAsync(intake, actor, now, cancellationToken);

            throw new CoreHrBusinessRuleException(
                CandidateIntake.DuplicateReviewCode,
                "Candidates with the same e-mail or phone already exist. Confirm again with existingCandidateId to link one of them, or change the contact data.")
            {
                Details = new Dictionary<string, object?>
                {
                    ["duplicateCandidates"] = duplicates.Select(DuplicateCandidate.From).ToList()
                }
            };
        }
        else
        {
            candidate = proposed;
            expectedCandidateVersion = null;
        }

        if (candidate.Id > 0 && await repository.ApplicationExistsAsync(candidate.Id, requisition.Id, cancellationToken))
        {
            throw ApplicationAlreadyExists(candidate.Id, requisition.Id);
        }

        var confirmation = await repository.ConfirmAsync(
            intake,
            candidate,
            expectedCandidateVersion,
            source,
            now,
            actor,
            cancellationToken)
            ?? throw new CoreHrConcurrencyConflictException("candidate");

        return new ConfirmedApplication(confirmation.Application, confirmation.Candidate, requisition);
    }

    public static CoreHrBusinessRuleException ApplicationAlreadyExists(long candidateId, long requisitionId) => new(
        ApplicationAlreadyExistsCode,
        "The candidate already has an application for this requisition.")
    {
        Details = new Dictionary<string, object?>
        {
            ["candidateId"] = candidateId,
            ["requisitionId"] = requisitionId
        }
    };

    private async Task<IReadOnlyList<CandidateSummary>> LoadDuplicateSummariesAsync(
        CandidateIntake intake,
        CancellationToken cancellationToken)
    {
        if (intake.DuplicateCandidateIds.Count == 0)
        {
            return [];
        }

        var candidates = await repository.GetCandidatesAsync(intake.DuplicateCandidateIds, cancellationToken);
        return candidates.Select(CandidateSummary.From).ToList();
    }

    private static void RequireWrite(CoreHrActor actor)
    {
        if (!actor.HasPermission(IntakePermissions.Write))
        {
            throw new CoreHrForbiddenException(
                WriteForbiddenCode,
                "Uploading or confirming a résumé requires the recruitment.intake.write permission.");
        }
    }

    /// <summary>Intakes are addressed by UUID; the shared not-found exception carries a numeric id, so 0 is used.</summary>
    private static CoreHrNotFoundException IntakeNotFound() => new(IntakeResourceName, 0);

    private static void Rewind(Stream content)
    {
        if (content.CanSeek)
        {
            content.Position = 0;
        }
    }

    private async Task TryDeleteStoredObjectAsync(string objectKey)
    {
        try
        {
            await storage.DeleteAsync(objectKey, CancellationToken.None);
        }
        catch (Exception)
        {
            // Best effort only: the caller rethrows the original insert failure and storage housekeeping
            // reconciles any orphaned object later.
        }
    }
}
