using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.BusinessLogic.Modules.CoreHr.Probation;

/// <summary>
/// EMP-06.1 / EMP-06.2 use cases: read the employee's current review, submit the assessment, search reviews
/// (overdue dashboard) and run decide / cancel / unlock. Data scope is enforced here (employee outside scope ⇒ 404);
/// reviewer identity and action-level permissions are re-checked here because endpoint policies only require the coarse claim.
/// </summary>
public sealed class ProbationReviewService(
    IProbationReviewRepository repository,
    TimeProvider timeProvider)
{
    public const string ResourceName = "Probation review";
    public const string ManageForbiddenCode = "corehr.probation.manage_forbidden";
    public const string DecideForbiddenCode = "corehr.probation.decide_forbidden";
    private const string ConflictResource = "probation review";

    public async Task<ProbationReviewView> GetCurrentAsync(long employeeId, CoreHrActor actor, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(actor);

        var entry = await LoadCurrentVisibleAsync(employeeId, actor, cancellationToken);
        return View(entry.Review, timeProvider.GetUtcNow());
    }

    public async Task<ProbationReviewView> SubmitAsync(SubmitProbationReviewCommand command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var actor = command.Actor;

        var entry = await LoadCurrentVisibleAsync(command.EmployeeId, actor, cancellationToken);
        var review = entry.Review;

        if (!review.CanBeAssessedBy(actor))
        {
            throw new CoreHrForbiddenException(
                ProbationReview.NotReviewerCode,
                "Only the assigned reviewer or an actor with corehr.probation.manage may submit the assessment.");
        }

        if (review.Version != command.ExpectedVersion)
        {
            throw new CoreHrConcurrencyConflictException(ConflictResource);
        }

        var now = timeProvider.GetUtcNow();
        var previousStatus = review.Status;
        review.Submit(command.Write, now);

        var saved = await repository.SaveAssessmentAsync(review, previousStatus, command.ExpectedVersion, actor, cancellationToken);
        if (!saved)
        {
            throw new CoreHrConcurrencyConflictException(ConflictResource);
        }

        return View(review, now);
    }

    public async Task<PagedResult<ProbationReviewView>> SearchAsync(
        ProbationReviewSearchQuery query,
        CoreHrActor actor,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(actor);

        var now = timeProvider.GetUtcNow();
        var today = Today(now);
        var result = await repository.SearchAsync(query, actor, today, cancellationToken);
        return result.Map(review => new ProbationReviewView(review, review.IsOverdue(today)));
    }

    public async Task<ProbationReviewView> TransitionAsync(
        TransitionProbationReviewCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        var actor = command.Actor;
        var decision = command.Decision ?? ProbationDecision.Empty;

        var entry = await repository.GetByIdAsync(command.ReviewId, cancellationToken);
        if (entry is null || !actor.CanAccessEmployee(entry.Review.EmployeeId, entry.EmployeeDepartmentId))
        {
            throw new CoreHrNotFoundException(ResourceName, command.ReviewId);
        }

        var review = entry.Review;
        var now = timeProvider.GetUtcNow();

        return command.Action switch
        {
            ProbationReviewAction.Decide => await DecideAsync(review, decision, command.ExpectedVersion, actor, now, cancellationToken),
            ProbationReviewAction.Cancel => await CancelAsync(review, decision.Reason, command.ExpectedVersion, actor, now, cancellationToken),
            ProbationReviewAction.Unlock => await UnlockAsync(review, decision.Reason, command.ExpectedVersion, actor, now, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(command), "Unknown probation review action.")
        };
    }

    private async Task<ProbationReviewView> DecideAsync(
        ProbationReview review,
        ProbationDecision decision,
        long expectedVersion,
        CoreHrActor actor,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        RequireDecide(actor);

        // Idempotent replay (EMP-06.2): a review already decided returns its current state and creates nothing,
        // whatever If-Match the retry carries.
        if (review.Status == ProbationReviewStatus.Decided)
        {
            return View(review, now);
        }

        RequireVersion(review, expectedVersion);

        var plan = review.Decide(decision, actor.UserId, Today(now), now);
        var saved = await repository.SaveDecisionAsync(review, plan, expectedVersion, actor, cancellationToken);
        if (!saved)
        {
            throw new CoreHrConcurrencyConflictException(ConflictResource);
        }

        return View(review, now);
    }

    private async Task<ProbationReviewView> CancelAsync(
        ProbationReview review,
        string? reason,
        long expectedVersion,
        CoreHrActor actor,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (!actor.HasPermission(ProbationPermissions.Manage))
        {
            throw new CoreHrForbiddenException(
                ManageForbiddenCode,
                "Cancelling a probation review requires the corehr.probation.manage permission.");
        }

        RequireVersion(review, expectedVersion);

        var previousStatus = review.Status;
        review.Cancel(reason, now);

        var saved = await repository.SaveCancellationAsync(
            review, previousStatus, expectedVersion, reason!.Trim(), actor, cancellationToken);
        if (!saved)
        {
            throw new CoreHrConcurrencyConflictException(ConflictResource);
        }

        return View(review, now);
    }

    private async Task<ProbationReviewView> UnlockAsync(
        ProbationReview review,
        string? reason,
        long expectedVersion,
        CoreHrActor actor,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        RequireDecide(actor);
        RequireVersion(review, expectedVersion);

        var linkedEventStatus = review.EmployeeEventId is { } eventId
            ? await repository.GetEmployeeEventStatusAsync(eventId, cancellationToken)
            : null;

        var employeeEventId = review.Unlock(reason, linkedEventStatus, now);

        var saved = await repository.SaveUnlockAsync(
            review, employeeEventId, expectedVersion, reason!.Trim(), actor, cancellationToken);
        if (!saved)
        {
            throw new CoreHrConcurrencyConflictException(ConflictResource);
        }

        return View(review, now);
    }

    private async Task<ProbationReviewEntry> LoadCurrentVisibleAsync(long employeeId, CoreHrActor actor, CancellationToken cancellationToken)
    {
        var departmentId = await repository.GetEmployeeDepartmentAsync(employeeId, cancellationToken);
        if (departmentId is not { } visibleDepartment || !actor.CanAccessEmployee(employeeId, visibleDepartment))
        {
            throw new CoreHrNotFoundException("Employee", employeeId);
        }

        return await repository.GetCurrentForEmployeeAsync(employeeId, cancellationToken)
            ?? throw new CoreHrNotFoundException(ResourceName, employeeId);
    }

    private static void RequireDecide(CoreHrActor actor)
    {
        if (!actor.HasPermission(ProbationPermissions.Decide))
        {
            throw new CoreHrForbiddenException(
                DecideForbiddenCode,
                "Deciding or unlocking a probation review requires the corehr.probation.decide permission.");
        }
    }

    private static void RequireVersion(ProbationReview review, long expectedVersion)
    {
        if (review.Version != expectedVersion)
        {
            throw new CoreHrConcurrencyConflictException(ConflictResource);
        }
    }

    private static ProbationReviewView View(ProbationReview review, DateTimeOffset now) =>
        new(review, review.IsOverdue(Today(now)));

    private static DateOnly Today(DateTimeOffset now) => DateOnly.FromDateTime(now.UtcDateTime);
}
