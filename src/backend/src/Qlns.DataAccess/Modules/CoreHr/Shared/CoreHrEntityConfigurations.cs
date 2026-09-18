using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Qlns.DataAccess.Modules.CoreHr.Shared;

public sealed class DepartmentEntityConfiguration : IEntityTypeConfiguration<DepartmentEntity>
{
    public void Configure(EntityTypeBuilder<DepartmentEntity> builder)
    {
        builder.ToTable("departments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(50);
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(255);
        builder.Property(x => x.ParentDepartmentId).HasColumnName("parent_department_id");
        builder.Property(x => x.CostCenter).HasColumnName("cost_center").HasMaxLength(100);
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public sealed class PositionEntityConfiguration : IEntityTypeConfiguration<PositionEntity>
{
    public void Configure(EntityTypeBuilder<PositionEntity> builder)
    {
        builder.ToTable("positions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(50);
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(255);
        builder.Property(x => x.Level).HasColumnName("level").HasMaxLength(50);
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}

public sealed class UserEntityConfiguration : IEntityTypeConfiguration<UserEntity>
{
    public void Configure(EntityTypeBuilder<UserEntity> builder)
    {
        builder.ToTable("users");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ExternalSubject).HasColumnName("external_subject").HasMaxLength(255);
        builder.Property(x => x.Email).HasColumnName("email").HasMaxLength(320);
        builder.Property(x => x.DisplayName).HasColumnName("display_name").HasMaxLength(255);
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.Version).HasColumnName("version");
    }
}

public sealed class EmployeeEntityConfiguration : IEntityTypeConfiguration<EmployeeEntity>
{
    public void Configure(EntityTypeBuilder<EmployeeEntity> builder)
    {
        builder.ToTable("employees");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.EmployeeCode).HasColumnName("employee_code").HasMaxLength(50);
        builder.Property(x => x.SourceApplicationId).HasColumnName("source_application_id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.FirstName).HasColumnName("first_name").HasMaxLength(100);
        builder.Property(x => x.LastName).HasColumnName("last_name").HasMaxLength(100);
        builder.Property(x => x.WorkEmail).HasColumnName("work_email").HasMaxLength(320);
        builder.Property(x => x.PersonalEmail).HasColumnName("personal_email").HasMaxLength(320);
        builder.Property(x => x.Phone).HasColumnName("phone").HasMaxLength(30);
        builder.Property(x => x.DateOfBirth).HasColumnName("date_of_birth");
        builder.Property(x => x.Gender).HasColumnName("gender").HasMaxLength(30);
        builder.Property(x => x.OfficeLocation).HasColumnName("office_location").HasMaxLength(255);
        builder.Property(x => x.PermanentAddress).HasColumnName("permanent_address");
        builder.Property(x => x.TemporaryAddress).HasColumnName("temporary_address");
        builder.Property(x => x.EmergencyContact).HasColumnName("emergency_contact").HasColumnType("jsonb");
        builder.Property(x => x.ManagerId).HasColumnName("manager_id");
        builder.Property(x => x.DepartmentId).HasColumnName("department_id");
        builder.Property(x => x.PositionId).HasColumnName("position_id");
        builder.Property(x => x.HireDate).HasColumnName("hire_date");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30);
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        builder.HasIndex(x => x.EmployeeCode).IsUnique();
        builder.HasIndex(x => new { x.DepartmentId, x.Status, x.LastName, x.FirstName })
            .HasDatabaseName("ix_employees_directory");
    }
}

public sealed class OnboardingTaskEntityConfiguration : IEntityTypeConfiguration<OnboardingTaskEntity>
{
    public void Configure(EntityTypeBuilder<OnboardingTaskEntity> builder)
    {
        builder.ToTable("onboarding_tasks");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.EmployeeId).HasColumnName("employee_id");
        builder.Property(x => x.TemplateKey).HasColumnName("template_key").HasMaxLength(100);
        builder.Property(x => x.TaskName).HasColumnName("task_name").HasMaxLength(255);
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.AssignedToUserId).HasColumnName("assigned_to_user_id");
        builder.Property(x => x.DueAt).HasColumnName("due_at");
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30);
        builder.Property(x => x.CompletedAt).HasColumnName("completed_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
        builder.HasIndex(x => new { x.EmployeeId, x.TemplateKey }).IsUnique()
            .HasDatabaseName("ux_onboarding_task_template");
    }
}

public sealed class EmployeeDocumentEntityConfiguration : IEntityTypeConfiguration<EmployeeDocumentEntity>
{
    public void Configure(EntityTypeBuilder<EmployeeDocumentEntity> builder)
    {
        builder.ToTable("employee_documents");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.EmployeeId).HasColumnName("employee_id");
        builder.Property(x => x.DocumentType).HasColumnName("document_type").HasMaxLength(80);
        builder.Property(x => x.Version).HasColumnName("version");
        builder.Property(x => x.OriginalFileName).HasColumnName("original_file_name").HasMaxLength(255);
        builder.Property(x => x.ObjectKey).HasColumnName("object_key").HasMaxLength(1024);
        builder.Property(x => x.ContentType).HasColumnName("content_type").HasMaxLength(100);
        builder.Property(x => x.SizeBytes).HasColumnName("size_bytes");
        builder.Property(x => x.UploadedBy).HasColumnName("uploaded_by");
        builder.Property(x => x.UploadedAt).HasColumnName("uploaded_at");
        builder.Property(x => x.RetentionUntil).HasColumnName("retention_until");
        builder.Property(x => x.DeletedAt).HasColumnName("deleted_at");
        builder.HasIndex(x => x.ObjectKey).IsUnique();
        builder.HasIndex(x => new { x.EmployeeId, x.DocumentType, x.Version }).IsUnique()
            .HasDatabaseName("ux_employee_document_version");
    }
}

public sealed class EmployeeEventEntityConfiguration : IEntityTypeConfiguration<EmployeeEventEntity>
{
    public void Configure(EntityTypeBuilder<EmployeeEventEntity> builder)
    {
        builder.ToTable("employee_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.EmployeeId).HasColumnName("employee_id");
        builder.Property(x => x.EventType).HasColumnName("event_type").HasMaxLength(50);
        builder.Property(x => x.Status).HasColumnName("status").HasMaxLength(30);
        builder.Property(x => x.EffectiveDate).HasColumnName("effective_date");
        builder.Property(x => x.BeforeData).HasColumnName("before_data").HasColumnType("jsonb");
        builder.Property(x => x.AfterData).HasColumnName("after_data").HasColumnType("jsonb");
        builder.Property(x => x.Reason).HasColumnName("reason");
        builder.Property(x => x.CompensatesEventId).HasColumnName("compensates_event_id");
        builder.Property(x => x.CreatedBy).HasColumnName("created_by");
        builder.Property(x => x.ApprovedBy).HasColumnName("approved_by");
        builder.Property(x => x.ApprovedAt).HasColumnName("approved_at");
        builder.Property(x => x.AppliedAt).HasColumnName("applied_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.Version).HasColumnName("version").IsConcurrencyToken();
    }
}
