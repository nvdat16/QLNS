using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Qlns.Api.Modules.CoreHr.Shared;
using Qlns.BusinessLogic.Modules.CoreHr.Onboarding;
using Qlns.BusinessLogic.Modules.CoreHr.Shared;

namespace Qlns.Api.Modules.CoreHr.Onboarding;

/// <summary>Onboarding checklist endpoints (EMP-03.1): search, transition and assignment update.</summary>
[Route("api/v1/onboarding/tasks")]
public sealed class OnboardingTasksController(OnboardingTaskService service) : CoreHrControllerBase
{
    [HttpGet(Name = "listOnboardingTasks")]
    [Authorize(Policy = CoreHrPolicies.OnboardingRead)]
    public Task<IActionResult> List(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = PageRequest.DefaultPageSize,
        [FromQuery] long? employeeId = null,
        [FromQuery] long? assignedToUserId = null,
        [FromQuery] string? status = null,
        [FromQuery] bool? overdue = null,
        CancellationToken cancellationToken = default)
    {
        if (!TryCreatePage(page, pageSize, out var pageRequest))
        {
            return Task.FromResult<IActionResult>(InvalidPage());
        }

        if (employeeId is <= 0)
        {
            return Task.FromResult<IActionResult>(ValidationProblemResult("employeeId", "employeeId must be a positive identifier."));
        }

        if (assignedToUserId is <= 0)
        {
            return Task.FromResult<IActionResult>(ValidationProblemResult("assignedToUserId", "assignedToUserId must be a positive identifier."));
        }

        OnboardingTaskStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!OnboardingTaskStatusNames.TryParseContract(status, out var parsed))
            {
                return Task.FromResult<IActionResult>(ValidationProblemResult("status", "Unknown onboarding task status. Use pending, in_progress or completed."));
            }

            statusFilter = parsed;
        }

        var query = new OnboardingTaskSearchQuery(employeeId, assignedToUserId, statusFilter, overdue, pageRequest);

        return ExecuteAsync(async actor =>
        {
            var result = await service.SearchAsync(query, actor, cancellationToken);
            return Ok(ToPage(result, ToResponse));
        });
    }

    [HttpPut("{taskId:long:min(1)}", Name = "updateOnboardingTaskAssignment")]
    [Authorize(Policy = CoreHrPolicies.OnboardingManage)]
    public Task<IActionResult> UpdateAssignment(
        long taskId,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromBody] OnboardingTaskWriteRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryParseVersion(ifMatch, out var expectedVersion))
        {
            return Task.FromResult<IActionResult>(InvalidIfMatch());
        }

        var write = new OnboardingTaskWrite(request.TaskName, request.Description, request.AssignedToUserId, request.DueAt);

        return ExecuteAsync(async actor =>
        {
            var view = await service.UpdateAssignmentAsync(
                new UpdateOnboardingTaskCommand(taskId, expectedVersion, write, actor),
                cancellationToken);

            SetETag(view.Task.Version);
            return Ok(ToResponse(view));
        });
    }

    [HttpPost("{taskId:long:min(1)}/{taskAction:regex(^(start|complete|reopen)$)}", Name = "transitionOnboardingTask")]
    [Authorize(Policy = CoreHrPolicies.OnboardingManage)]
    public Task<IActionResult> Transition(
        long taskId,
        string taskAction,
        [FromHeader(Name = "If-Match")] string? ifMatch,
        [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] ReasonRequest? request,
        CancellationToken cancellationToken)
    {
        if (!TryParseVersion(ifMatch, out var expectedVersion))
        {
            return Task.FromResult<IActionResult>(InvalidIfMatch());
        }

        if (!OnboardingTaskActionNames.TryParseContract(taskAction, out var parsedAction))
        {
            return Task.FromResult<IActionResult>(ValidationProblemResult("action", "Unknown onboarding task action. Use start, complete or reopen."));
        }

        return ExecuteAsync(async actor =>
        {
            var view = await service.TransitionAsync(
                new TransitionOnboardingTaskCommand(taskId, parsedAction, expectedVersion, request?.Reason, actor),
                cancellationToken);

            SetETag(view.Task.Version);
            return Ok(ToResponse(view));
        });
    }

    private static OnboardingTaskResponse ToResponse(OnboardingTaskView view) => new(
        view.Task.Id,
        view.Task.EmployeeId,
        view.Task.TemplateKey,
        view.Task.TaskName,
        view.Task.Description,
        view.Task.AssignedToUserId,
        view.Task.DueAt,
        view.Task.Status.ToContract(),
        view.Task.CompletedAt,
        view.Overdue,
        view.Task.Version);
}

/// <summary>OpenAPI OnboardingTaskWrite.</summary>
public sealed record OnboardingTaskWriteRequest(
    [Required, MaxLength(OnboardingTask.TaskNameMaxLength)] string TaskName,
    [MaxLength(OnboardingTask.DescriptionMaxLength)] string? Description,
    long? AssignedToUserId,
    DateTimeOffset? DueAt);

/// <summary>OpenAPI ReasonRequest. Optional for start/complete, mandatory content for reopen (enforced by the domain).</summary>
public sealed record ReasonRequest([MaxLength(1000)] string? Reason);

/// <summary>OpenAPI OnboardingTask.</summary>
public sealed record OnboardingTaskResponse(
    long Id,
    long EmployeeId,
    string TemplateKey,
    string TaskName,
    string? Description,
    long? AssignedToUserId,
    DateTimeOffset? DueAt,
    string Status,
    DateTimeOffset? CompletedAt,
    bool Overdue,
    long Version);
