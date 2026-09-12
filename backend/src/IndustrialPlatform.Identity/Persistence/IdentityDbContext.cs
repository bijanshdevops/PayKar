using System.Reflection;
using IndustrialPlatform.Identity.Entities;
using Microsoft.EntityFrameworkCore;

namespace IndustrialPlatform.Identity.Persistence;

/// <summary>
/// DbContext اختصاصی ماژول Identity — طبق ماتریس وابستگی سند 01-Architecture مجزا از AppDbContext
/// (پروژه Persistence) نگه‌داری می‌شود تا Identity و Persistence به یکدیگر وابسته نباشند.
/// </summary>
public sealed class IdentityDbContext : DbContext
{
    public IdentityDbContext(DbContextOptions<IdentityDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        base.OnModelCreating(modelBuilder);
    }
}
