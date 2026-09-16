using Microsoft.EntityFrameworkCore;
using Qlns.DataAccess.Modules.Recruitment.Applications;

namespace Qlns.DataAccess;

public sealed class QlnsDbContext(DbContextOptions<QlnsDbContext> options) : DbContext(options)
{
    public DbSet<ApplicationEntity> Applications => Set<ApplicationEntity>();
    public DbSet<JobPostingEntity> JobPostings => Set<JobPostingEntity>();
    public DbSet<InterviewEntity> Interviews => Set<InterviewEntity>();
    public DbSet<EvaluationEntity> Evaluations => Set<EvaluationEntity>();
    public DbSet<ApplicationStageEventEntity> ApplicationStageEvents => Set<ApplicationStageEventEntity>();
    public DbSet<AuditLogEntity> AuditLogs => Set<AuditLogEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(QlnsDbContext).Assembly);
    }
}
