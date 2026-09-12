using IndustrialPlatform.Identity.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IndustrialPlatform.Identity.Persistence.Configurations;

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");
        builder.HasKey(ur => new { ur.UserId, ur.RoleId });

        builder.Property(ur => ur.UserId).HasColumnName("user_id");
        builder.Property(ur => ur.RoleId).HasColumnName("role_id");

        // یادداشت: WithMany(u => u.UserRoles) صریحاً باید به ناوبری موجود روی User اشاره کند،
        // وگرنه EF Core یک رابطه دومِ ناخواسته (Shadow FK با نام UserId1) هم به‌صورت Convention می‌سازد.
        builder.HasOne<User>()
            .WithMany(u => u.UserRoles)
            .HasForeignKey(ur => ur.UserId)
            .HasConstraintName("fk_user_roles_users_user_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Role>()
            .WithMany()
            .HasForeignKey(ur => ur.RoleId)
            .HasConstraintName("fk_user_roles_roles_role_id")
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(ur => ur.RoleId).HasDatabaseName("ix_user_roles_role_id");
    }
}
