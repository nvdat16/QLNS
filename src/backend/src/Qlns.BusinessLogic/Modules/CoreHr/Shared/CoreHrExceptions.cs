namespace Qlns.BusinessLogic.Modules.CoreHr.Shared;

/// <summary>Resource does not exist or is outside the actor's data scope (HTTP 404).</summary>
public sealed class CoreHrNotFoundException(string resource, long id)
    : Exception($"{resource} {id} was not found or is not visible in the current data scope.")
{
    public string Resource { get; } = resource;
    public long Id { get; } = id;
}

/// <summary>Actor lacks permission, field access or data scope for the operation (HTTP 403).</summary>
public sealed class CoreHrForbiddenException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

/// <summary>Version mismatch (If-Match) or a concurrent write won the race (HTTP 409).</summary>
public sealed class CoreHrConcurrencyConflictException(string resource)
    : Exception($"The {resource} changed concurrently. Reload and retry with the current ETag.")
{
    public string Resource { get; } = resource;
}

/// <summary>Workflow, prerequisite or uniqueness rule violated (HTTP 409 with a stable code).</summary>
public sealed class CoreHrBusinessRuleException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;

    /// <summary>Optional structured detail, for example the list of blocking dependencies.</summary>
    public IReadOnlyDictionary<string, object?> Details { get; init; } =
        new Dictionary<string, object?>();
}

/// <summary>Semantically invalid fields (HTTP 422). Keys are contract field names.</summary>
public sealed class CoreHrValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("Request validation failed.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;

    public static CoreHrValidationException For(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}

/// <summary>Accumulates field errors and throws once when any were recorded.</summary>
public sealed class ValidationErrors
{
    private readonly Dictionary<string, List<string>> _errors = [];

    public bool HasErrors => _errors.Count > 0;

    public ValidationErrors Add(string field, string message)
    {
        if (!_errors.TryGetValue(field, out var list))
        {
            list = [];
            _errors[field] = list;
        }

        list.Add(message);
        return this;
    }

    public void ThrowIfAny()
    {
        if (HasErrors)
        {
            throw new CoreHrValidationException(
                _errors.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray()));
        }
    }
}
