using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Qlns.DataAccess.Modules.Contracts.Shared;

public sealed class ContractEntityConfiguration : IEntityTypeConfiguration<ContractEntity>
{
    public void Configure(EntityTypeBuilder<ContractEntity> builder)
    {
        builder.ToTable("contracts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.EmployeeId).HasColumnName("employee_id");
        builder.Property(x => x.ContractNumber).HasColumnName("contract_number").HasMaxLength(100);
        builder.Property(x => x.ContractType).HasColumnName("contract_type").HasMaxLength(40);
        builder.Property(x => x.StartDate).HasColumnName("start_date");
        builder.Property(x => x.EndDate).HasColumnName("end_date");
        builder.Property(x => x.Salary).HasColumnName("salary").HasPrecision(15, 2);
        builder.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3).IsFixedLength();
        builder.Property(x => x.NoticePeriodDays).HasColumnName("notice_period_days");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30);
        builder.Property(x => x.IsPrimary).HasColumnName("is_primary");
        builder.Property(x => x.DocumentObjectKey).HasColumnName("document_object_key").HasMaxLength(1024);
        builder.Property(x => x.SignedAt).HasColumnName("signed_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        builder.HasIndex(x => x.ContractNumber).IsUnique();
    }
}

public sealed class ContractAddendumEntityConfiguration : IEntityTypeConfiguration<ContractAddendumEntity>
{
    public void Configure(EntityTypeBuilder<ContractAddendumEntity> builder)
    {
        builder.ToTable("contract_addenda");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ContractId).HasColumnName("contract_id");
        builder.Property(x => x.AddendumNumber).HasColumnName("addendum_number").HasMaxLength(100);
        builder.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30);
        builder.Property(x => x.EffectiveDate).HasColumnName("effective_date");
        builder.Property(x => x.BeforeTerms).HasColumnName("before_terms").HasColumnType("jsonb");
        builder.Property(x => x.AfterTerms).HasColumnName("after_terms").HasColumnType("jsonb");
        builder.Property(x => x.Reason).HasColumnName("reason");
        builder.Property(x => x.DocumentObjectKey).HasColumnName("document_object_key").HasMaxLength(1024);
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.ApprovedBy).HasColumnName("approved_by");
        builder.Property(x => x.ApprovedAt).HasColumnName("approved_at");
        builder.Property(x => x.SignedAt).HasColumnName("signed_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.HasIndex(x => x.AddendumNumber).IsUnique();
    }
}
