using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Qlns.DataAccess.Modules.CoreHr.Probation;

/// <summary>probation_reviews — exactly one per probation contract (ux_probation_review_contract).</summary>
public sealed class ProbationReviewEntity
{
    public long Id { get; set; }
    public long EmployeeId { get; set; }
    public long ContractId { get; set; }
    public DateOnly ReviewDueDate { get; set; }
    public long? ReviewerUserId { get; set; }
    public string Status { get; set; } = null!;
    public string? Outcome { get; set; }
    public decimal? OverallScore { get; set; }
    public string? Strengths { get; set; }
    public string? Improvements { get; set; }
    public DateOnly? EffectiveDate { get; set; }
    public long? DecidedBy { get; set; }
    public DateTimeOffset? DecidedAt { get; set; }
    public long? EmployeeEventId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long Version { get; set; }
}

public sealed class ProbationReviewEntityConfiguration : IEntityTypeConfiguration<ProbationReviewEntity>
{
    public void Configure(EntityTypeBuilder<ProbationReviewEntity> builder)
    {
        builder.ToTable("probation_reviews");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.EmployeeId).HasColumnName("employee_id");
        builder.Property(x => x.ContractId).HasColumnName("contract_id");
        builder.Property(x => x.ReviewDueDate).HasColumnName("review_due_date");
        builder.Property(x => x.ReviewerUserId).HasColumnName("reviewer_user_id");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30).HasDefaultValue("pending");
        builder.Property(x => x.Outcome).HasColumnName("outcome").HasMaxLength(30);
        builder.Property(x => x.OverallScore).HasColumnName("overall_score").HasPrecision(3, 1);
        builder.Property(x => x.Strengths).HasColumnName("strengths");
        builder.Property(x => x.Improvements).HasColumnName("improvements");
        builder.Property(x => x.EffectiveDate).HasColumnName("effective_date");
        builder.Property(x => x.DecidedBy).HasColumnName("decided_by");
        builder.Property(x => x.DecidedAt).HasColumnName("decided_at");
        builder.Property(x => x.EmployeeEventId).HasColumnName("employee_event_id");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at").HasDefaultValueSql("now()");
        builder.Property(x => x.Version).HasColumnName("version").HasDefaultValue(1L).IsConcurrencyToken();
        builder.HasIndex(x => x.ContractId).IsUnique().HasDatabaseName("ux_probation_review_contract");
    }
}
