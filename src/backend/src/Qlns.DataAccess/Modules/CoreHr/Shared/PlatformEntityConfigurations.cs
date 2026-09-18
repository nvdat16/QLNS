using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Qlns.DataAccess.Modules.CoreHr.Shared;

public sealed class AuditLogEntityConfiguration : IEntityTypeConfiguration<AuditLogEntity>
{
    public void Configure(EntityTypeBuilder<AuditLogEntity> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ActorUserId).HasColumnName("actor_user_id");
        builder.Property(x => x.Action).HasColumnName("action").HasMaxLength(120);
        builder.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(100);
        builder.Property(x => x.EntityId).HasColumnName("entity_id").HasMaxLength(100);
        builder.Property(x => x.BeforeData).HasColumnName("before_data").HasColumnType("jsonb");
        builder.Property(x => x.AfterData).HasColumnName("after_data").HasColumnType("jsonb");
        builder.Property(x => x.Result).HasColumnName("result").HasMaxLength(30);
        builder.Property(x => x.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100);
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at").HasDefaultValueSql("now()");
    }
}

public sealed class OutboxMessageEntityConfiguration : IEntityTypeConfiguration<OutboxMessageEntity>
{
    public void Configure(EntityTypeBuilder<OutboxMessageEntity> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.MessageType).HasColumnName("message_type").HasMaxLength(200);
        builder.Property(x => x.AggregateType).HasColumnName("aggregate_type").HasMaxLength(100);
        builder.Property(x => x.AggregateId).HasColumnName("aggregate_id").HasMaxLength(100);
        builder.Property(x => x.Payload).HasColumnName("payload").HasColumnType("jsonb");
        builder.Property(x => x.OccurredAt).HasColumnName("occurred_at");
        builder.Property(x => x.AvailableAt).HasColumnName("available_at").HasDefaultValueSql("now()");
        builder.Property(x => x.ProcessedAt).HasColumnName("processed_at");
        builder.Property(x => x.Attempts).HasColumnName("attempts").HasDefaultValue(0);
        builder.Property(x => x.LastError).HasColumnName("last_error");
    }
}
