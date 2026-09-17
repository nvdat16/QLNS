using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.Recruitment.Applications;
using Qlns.BusinessLogic.Modules.Recruitment.Intake;
using Qlns.BusinessLogic.Modules.Recruitment.Requisitions;
using Qlns.DataAccess.Modules.CoreHr.Shared;
using Qlns.DataAccess.Modules.Recruitment.Requisitions;
using Qlns.DataAccess.Modules.Recruitment.Shared;

namespace Qlns.DataAccess.Modules.Recruitment.Intake;

/// <summary>
/// PostgreSQL persistence for résumé intakes (resumes), candidates and the application created on confirmation.
/// Reads join job_postings so the actor's data scope is applied in SQL. The privacy acknowledgement of the upload is
/// kept in the <c>parsed_data</c> envelope next to the parser suggestion until it is copied onto the candidate.
/// Audit payloads carry metadata only: never parsed personal data, never the object key.
/// </summary>
public sealed class CandidateIntakeRepository(QlnsDbContext dbContext) : ICandidateIntakeRepository
{
    private const string ResumeEntityType = "resume";
    private const string ApplicationUniqueConstraint = "ux_applications_candidate_job";

    private DbSet<ResumeEntity> Resumes => dbContext.Set<ResumeEntity>();
    private DbSet<CandidateEntity> Candidates => dbContext.Set<CandidateEntity>();
    private DbSet<ApplicationEntity> Applications => dbContext.Set<ApplicationEntity>();
    private DbSet<JobPostingEntity> Postings => dbContext.Set<JobPostingEntity>();
    private DbSet<AuditLogEntity> AuditLogs => dbContext.Set<AuditLogEntity>();

    public async Task<Requisition?> GetVisibleRequisitionAsync(long requisitionId, CoreHrActor actor, CancellationToken cancellationToken)
    {
        var entity = await VisiblePostings(actor).SingleOrDefaultAsync(x => x.Id == requisitionId, cancellationToken);
        return entity is null ? null : RequisitionRepository.ToDomain(entity);
    }

    public async Task<CandidateIntake?> GetByIntakeIdAsync(Guid intakeId, CoreHrActor actor, CancellationToken cancellationToken)
    {
        var entity = await (
            from resume in Resumes.AsNoTracking()
            join posting in VisiblePostings(actor) on resume.JobPostingId equals posting.Id
            where resume.IntakeId == intakeId
            select resume)
            .SingleOrDefaultAsync(cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IReadOnlyList<Candidate>> GetCandidatesAsync(
        IReadOnlyCollection<long> candidateIds,
        CancellationToken cancellationToken)
    {
        if (candidateIds.Count == 0)
        {
            return [];
        }

        var entities = await Candidates.AsNoTracking()
            .Where(x => candidateIds.Contains(x.Id))
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Candidate>> FindDuplicatesAsync(
        string normalizedEmail,
        string? normalizedPhone,
        CancellationToken cancellationToken)
    {
        var entities = await Candidates.AsNoTracking()
            .Where(x => x.NormalizedEmail == normalizedEmail ||
                (normalizedPhone != null && x.NormalizedPhone == normalizedPhone))
            .OrderBy(x => x.Id)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain).ToList();
    }

    public Task<bool> ApplicationExistsAsync(long candidateId, long requisitionId, CancellationToken cancellationToken) =>
        Applications.AsNoTracking().AnyAsync(
            x => x.CandidateId == candidateId && x.JobPostingId == requisitionId,
            cancellationToken);

    public async Task<IntakeConfirmation?> GetConfirmationAsync(CandidateIntake intake, CancellationToken cancellationToken)
    {
        var row = await (
            from application in Applications.AsNoTracking()
            join candidate in Candidates.AsNoTracking() on application.CandidateId equals candidate.Id
            where application.ResumeId == intake.Id
            orderby application.Id
            select new { Application = application, Candidate = candidate })
            .FirstOrDefaultAsync(cancellationToken);

        return row is null ? null : new IntakeConfirmation(ToDomain(row.Application), ToDomain(row.Candidate));
    }

    public async Task<CandidateIntake> InsertAsync(CandidateIntake intake, CoreHrActor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(intake);
        ArgumentNullException.ThrowIfNull(actor);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var entity = new ResumeEntity
        {
            IntakeId = intake.IntakeId,
            JobPostingId = intake.RequisitionId,
            CandidateId = intake.CandidateId,
            ObjectKey = intake.ObjectKey,
            OriginalFileName = intake.OriginalFileName,
            ContentType = intake.ContentType,
            SizeBytes = intake.SizeBytes,
            IntakeStatus = intake.Status.ToContract(),
            MalwareScanStatus = intake.MalwareScanStatus,
            ParserStatus = intake.ParserStatus,
            ParsedData = JsonSerializer.Serialize(ParsedDataEnvelope.From(intake), CoreHrAudit.JsonOptions),
            ParseConfidence = JsonSerializer.Serialize(intake.Confidence, CoreHrAudit.JsonOptions),
            ParserVersion = intake.ParserVersion,
            DuplicateCandidateIds = intake.DuplicateCandidateIds.ToArray(),
            UploadedBy = intake.UploadedBy,
            UploadedAt = intake.UploadedAt,
            ConfirmedBy = intake.ConfirmedBy,
            ConfirmedAt = intake.ConfirmedAt
        };

        Resumes.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        var persisted = ToDomain(entity);
        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            "recruitment.intake.start",
            ResumeEntityType,
            persisted.Id,
            before: null,
            after: new
            {
                intakeId = persisted.IntakeId,
                requisitionId = persisted.RequisitionId,
                originalFileName = persisted.OriginalFileName,
                contentType = persisted.ContentType,
                sizeBytes = persisted.SizeBytes,
                intakeStatus = persisted.Status.ToContract(),
                malwareScanStatus = persisted.MalwareScanStatus,
                parserStatus = persisted.ParserStatus,
                parserVersion = persisted.ParserVersion,
                privacyNoticeVersion = persisted.PrivacyNoticeVersion
            },
            occurredAt: persisted.UploadedAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return persisted;
    }

    public async Task SaveDuplicateReviewAsync(
        CandidateIntake intake,
        CoreHrActor actor,
        DateTimeOffset occurredAt,
        CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var duplicateCandidateIds = intake.DuplicateCandidateIds.ToArray();
        await Resumes
            .Where(x => x.Id == intake.Id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.IntakeStatus, intake.Status.ToContract())
                    .SetProperty(x => x.DuplicateCandidateIds, duplicateCandidateIds),
                cancellationToken);

        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            "recruitment.intake.duplicate_review",
            ResumeEntityType,
            intake.Id,
            before: null,
            after: new { intakeStatus = intake.Status.ToContract(), duplicateCandidateIds },
            occurredAt));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IntakeConfirmation?> ConfirmAsync(
        CandidateIntake intake,
        Candidate candidate,
        long? expectedCandidateVersion,
        string source,
        DateTimeOffset now,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(intake);
        ArgumentNullException.ThrowIfNull(candidate);
        ArgumentNullException.ThrowIfNull(actor);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        Candidate persistedCandidate;
        var candidateCreated = candidate.Id == 0;
        if (candidateCreated)
        {
            var candidateEntity = new CandidateEntity
            {
                FirstName = candidate.FirstName,
                LastName = candidate.LastName,
                Email = candidate.Email,
                NormalizedEmail = candidate.NormalizedEmail,
                Phone = candidate.Phone,
                NormalizedPhone = candidate.NormalizedPhone,
                LinkedinUrl = candidate.LinkedinUrl,
                PortfolioUrl = candidate.PortfolioUrl,
                PrivacyNoticeVersion = candidate.PrivacyNoticeVersion,
                ConsentedAt = candidate.ConsentedAt,
                RetentionUntil = null,
                CreatedAt = candidate.CreatedAt,
                UpdatedAt = candidate.UpdatedAt,
                Version = candidate.Version
            };

            Candidates.Add(candidateEntity);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (IsUniqueViolation(exception, out _))
            {
                // A concurrent confirm created the same e-mail between duplicate detection and this insert.
                await transaction.RollbackAsync(cancellationToken);
                throw new CoreHrConcurrencyConflictException("candidate");
            }

            persistedCandidate = ToDomain(candidateEntity);
        }
        else
        {
            var rows = await Candidates
                .Where(x => x.Id == candidate.Id && x.Version == expectedCandidateVersion)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(x => x.Phone, candidate.Phone)
                        .SetProperty(x => x.NormalizedPhone, candidate.NormalizedPhone)
                        .SetProperty(x => x.LinkedinUrl, candidate.LinkedinUrl)
                        .SetProperty(x => x.PortfolioUrl, candidate.PortfolioUrl)
                        .SetProperty(x => x.Version, candidate.Version)
                        .SetProperty(x => x.UpdatedAt, candidate.UpdatedAt),
                    cancellationToken);

            if (rows != 1)
            {
                await transaction.RollbackAsync(cancellationToken);
                return null;
            }

            persistedCandidate = candidate;
        }

        var application = RecruitmentApplication.Create(persistedCandidate.Id, intake.RequisitionId, intake.Id, source, now);
        var applicationEntity = new ApplicationEntity
        {
            CandidateId = application.CandidateId,
            JobPostingId = application.JobPostingId,
            ResumeId = application.ResumeId,
            Stage = application.Stage.ToContract(),
            AiScore = application.AiScore,
            Source = application.Source,
            AppliedAt = application.AppliedAt,
            UpdatedAt = application.UpdatedAt,
            Version = application.Version
        };

        Applications.Add(applicationEntity);
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueViolation(exception, out var constraint) && constraint == ApplicationUniqueConstraint)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw CandidateIntakeService.ApplicationAlreadyExists(persistedCandidate.Id, intake.RequisitionId);
        }

        dbContext.Set<ApplicationStageEventEntity>().Add(new ApplicationStageEventEntity
        {
            ApplicationId = applicationEntity.Id,
            FromStage = null,
            ToStage = application.Stage.ToContract(),
            Reason = null,
            ChangedBy = actor.UserId,
            ChangedAt = application.AppliedAt,
            ApplicationVersion = application.Version
        });

        intake.Complete(persistedCandidate.Id, actor.UserId, now);
        await Resumes
            .Where(x => x.Id == intake.Id)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.IntakeStatus, intake.Status.ToContract())
                    .SetProperty(x => x.ParserStatus, intake.ParserStatus)
                    .SetProperty(x => x.CandidateId, intake.CandidateId)
                    .SetProperty(x => x.ConfirmedBy, intake.ConfirmedBy)
                    .SetProperty(x => x.ConfirmedAt, intake.ConfirmedAt),
                cancellationToken);

        AuditLogs.Add(CoreHrAudit.Entry(
            actor,
            "recruitment.intake.confirm",
            ResumeEntityType,
            intake.Id,
            before: null,
            after: new
            {
                intakeStatus = intake.Status.ToContract(),
                candidateId = persistedCandidate.Id,
                candidateCreated,
                applicationId = applicationEntity.Id,
                source = application.Source
            },
            occurredAt: now));

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new IntakeConfirmation(ToDomain(applicationEntity), persistedCandidate);
    }

    /// <summary>Postings the actor may see: organization-wide scope, or the posting's department in the actor's set.</summary>
    private IQueryable<JobPostingEntity> VisiblePostings(CoreHrActor actor)
    {
        var postings = Postings.AsNoTracking();
        if (actor.DataScope.OrganizationWide)
        {
            return postings;
        }

        var departmentIds = actor.DataScope.DepartmentIds;
        return postings.Where(x => departmentIds.Contains(x.DepartmentId));
    }

    private static bool IsUniqueViolation(DbUpdateException exception, out string? constraintName)
    {
        if (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } postgres)
        {
            constraintName = postgres.ConstraintName;
            return true;
        }

        constraintName = null;
        return false;
    }

    private static CandidateIntake ToDomain(ResumeEntity entity)
    {
        if (!IntakeStatusNames.TryParseContract(entity.IntakeStatus, out var status))
        {
            throw new InvalidOperationException($"resumes {entity.Id} has unknown intake_status '{entity.IntakeStatus}'.");
        }

        var envelope = entity.ParsedData is null
            ? throw new InvalidOperationException($"resumes {entity.Id} has no parsed_data envelope.")
            : JsonSerializer.Deserialize<ParsedDataEnvelope>(entity.ParsedData, CoreHrAudit.JsonOptions)
                ?? throw new InvalidOperationException($"resumes {entity.Id} has an invalid parsed_data envelope.");

        var confidence = entity.ParseConfidence is null
            ? new Dictionary<string, double>()
            : JsonSerializer.Deserialize<Dictionary<string, double>>(entity.ParseConfidence, CoreHrAudit.JsonOptions)
                ?? new Dictionary<string, double>();

        return new CandidateIntake(
            entity.Id,
            entity.IntakeId,
            entity.JobPostingId,
            entity.CandidateId,
            entity.ObjectKey,
            entity.OriginalFileName,
            entity.ContentType,
            entity.SizeBytes,
            status,
            entity.MalwareScanStatus,
            entity.ParserStatus,
            envelope.Candidate,
            confidence,
            entity.ParserVersion,
            envelope.PrivacyNoticeVersion,
            envelope.ConsentedAt,
            entity.DuplicateCandidateIds,
            entity.UploadedBy ?? throw new InvalidOperationException($"resumes {entity.Id} has no uploaded_by user."),
            entity.UploadedAt,
            entity.ConfirmedBy,
            entity.ConfirmedAt);
    }

    private static Candidate ToDomain(CandidateEntity entity) => new(
        entity.Id,
        entity.FirstName,
        entity.LastName,
        entity.Email,
        entity.NormalizedEmail,
        entity.Phone,
        entity.NormalizedPhone,
        entity.LinkedinUrl,
        entity.PortfolioUrl,
        entity.PrivacyNoticeVersion,
        entity.ConsentedAt,
        entity.Version,
        entity.CreatedAt,
        entity.UpdatedAt);

    private static RecruitmentApplication ToDomain(ApplicationEntity entity)
    {
        if (!ApplicationStageNames.TryParseContract(entity.Stage, out var stage))
        {
            throw new InvalidOperationException($"applications {entity.Id} has unknown stage '{entity.Stage}'.");
        }

        return new RecruitmentApplication(
            entity.Id,
            entity.CandidateId,
            entity.JobPostingId,
            entity.ResumeId,
            stage,
            entity.AiScore,
            entity.Source,
            entity.AppliedAt,
            entity.Version,
            entity.UpdatedAt);
    }

    /// <summary>
    /// Shape of <c>resumes.parsed_data</c>: the parser suggestion plus the privacy acknowledgement captured at upload,
    /// which has no column of its own on resumes and is copied to the candidate at confirmation.
    /// </summary>
    private sealed record ParsedDataEnvelope(string PrivacyNoticeVersion, DateTimeOffset ConsentedAt, CandidateInput? Candidate)
    {
        public static ParsedDataEnvelope From(CandidateIntake intake) =>
            new(intake.PrivacyNoticeVersion, intake.ConsentedAt, intake.ParsedCandidate);
    }
}
