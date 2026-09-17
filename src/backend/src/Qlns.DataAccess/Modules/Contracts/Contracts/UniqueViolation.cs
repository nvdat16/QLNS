using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Qlns.DataAccess.Modules.Contracts.Contracts;

/// <summary>
/// Recognises PostgreSQL unique-constraint violations (SQLSTATE 23505) raised either directly by
/// <c>ExecuteUpdateAsync</c> or wrapped in <see cref="DbUpdateException"/> by <c>SaveChangesAsync</c>, so the
/// repositories can map the race between the pre-check and the insert to the module's 409 code.
/// </summary>
internal static class UniqueViolation
{
    private const string UniqueViolationSqlState = "23505";

    public static bool IsOn(Exception exception, string constraintFragment)
    {
        var postgres = exception as PostgresException ?? exception.InnerException as PostgresException;
        return postgres is { SqlState: UniqueViolationSqlState } &&
            (postgres.ConstraintName ?? string.Empty).Contains(constraintFragment, StringComparison.OrdinalIgnoreCase);
    }
}
