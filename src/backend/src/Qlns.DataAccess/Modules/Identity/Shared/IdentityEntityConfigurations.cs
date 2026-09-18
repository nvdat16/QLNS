using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Qlns.DataAccess.Modules.Identity.Shared;

public sealed class UserCredentialEntityConfiguration : IEntityTypeConfiguration<UserCredentialEntity>
{
    public void Configure(EntityTypeBuilder<UserCredentialEntity> builder)
    {
        builder.ToTable("user_credentials");
        builder.HasKey(x => x.UserId);
        builder.Property(x => x.UserId).HasColumnName("user_id").ValueGeneratedNever();
        builder.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(255);
        builder.Property(x => x.PasswordAlgorithm).HasColumnName("password_algorithm").HasMaxLength(40);
        builder.Property(x => x.MustChangePassword).HasColumnName("must_change_password");
        builder.Property(x => x.PasswordUpdatedAt).HasColumnName("password_updated_at");
        builder.Property(x => x.FailedAttempts).HasColumnName("failed_attempts");
        builder.Property(x => x.LockedUntil).HasColumnName("locked_until");
        builder.Property(x => x.LastLoginAt).HasColumnName("last_login_at");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        builder.Property(x => x.Version).HasColumnName("version");
    }
}

public sealed class RoleEntityConfiguration : IEntityTypeConfiguration<RoleEntity>
{
    public void Configure(EntityTypeBuilder<RoleEntity> builder)
    {
        builder.ToTable("roles");
        builder.HasKey(x => x.Code);
        builder.Property(x => x.Code).HasColumnName("code").HasMaxLength(80).ValueGeneratedNever();
        builder.Property(x => x.Name).HasColumnName("name").HasMaxLength(255);
        builder.Property(x => x.Description).HasColumnName("description");
        builder.Property(x => x.IsAssignable).HasColumnName("is_assignable");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
    }
}

public sealed class RolePermissionEntityConfiguration : IEntityTypeConfiguration<RolePermissionEntity>
{
    public void Configure(EntityTypeBuilder<RolePermissionEntity> builder)
    {
        builder.ToTable("role_permissions");
        builder.HasKey(x => new { x.RoleCode, x.Permission });
        builder.Property(x => x.RoleCode).HasColumnName("role_code").HasMaxLength(80);
        builder.Property(x => x.Permission).HasColumnName("permission").HasMaxLength(120);
    }
}

public sealed class UserRoleEntityConfiguration : IEntityTypeConfiguration<UserRoleEntity>
{
    public void Configure(EntityTypeBuilder<UserRoleEntity> builder)
    {
        builder.ToTable("user_roles");
        builder.HasKey(x => new { x.UserId, x.RoleCode, x.DataScopeType, x.DataScopeId });
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.RoleCode).HasColumnName("role_code").HasMaxLength(80);
        builder.Property(x => x.DataScopeType).HasColumnName("data_scope_type").HasMaxLength(30);
        builder.Property(x => x.DataScopeId).HasColumnName("data_scope_id");
        builder.Property(x => x.GrantedAt).HasColumnName("granted_at");
        builder.Property(x => x.GrantedBy).HasColumnName("granted_by");
    }
}

public sealed class RefreshTokenEntityConfiguration : IEntityTypeConfiguration<RefreshTokenEntity>
{
    public void Configure(EntityTypeBuilder<RefreshTokenEntity> builder)
    {
        builder.ToTable("refresh_tokens");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.UserId).HasColumnName("user_id");
        builder.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(64);
        builder.Property(x => x.IssuedAt).HasColumnName("issued_at");
        builder.Property(x => x.ExpiresAt).HasColumnName("expires_at");
        builder.Property(x => x.RevokedAt).HasColumnName("revoked_at");
        builder.Property(x => x.RevokedReason).HasColumnName("revoked_reason").HasMaxLength(40);
        builder.Property(x => x.ReplacedByTokenId).HasColumnName("replaced_by_token_id");
        builder.Property(x => x.ClientIp).HasColumnName("client_ip").HasMaxLength(45);
        builder.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(255);
        builder.HasIndex(x => x.TokenHash).IsUnique();
    }
}
