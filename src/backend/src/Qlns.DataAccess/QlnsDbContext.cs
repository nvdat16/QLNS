using Microsoft.EntityFrameworkCore;

namespace Qlns.DataAccess;

/// <summary>
/// Single EF Core context over database/schema.sql. Entity mappings are discovered from this assembly
/// (<c>Modules/&lt;Module&gt;/Shared</c> or the feature folder). Repositories address tables through
/// <see cref="DbContext.Set{TEntity}"/>, so no per-table DbSet property is required here.
/// </summary>
public sealed class QlnsDbContext(DbContextOptions<QlnsDbContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(QlnsDbContext).Assembly);
    }
}
