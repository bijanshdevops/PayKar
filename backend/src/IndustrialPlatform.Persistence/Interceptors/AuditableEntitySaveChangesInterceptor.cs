using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Shared.Abstractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace IndustrialPlatform.Persistence.Interceptors;

/// <summary>
/// طبق سند 01-Architecture.md بخش 2.5: «ثبت خودکار فیلدهای CreatedAtUtc, CreatedBy,
/// LastModifiedAtUtc, LastModifiedBy از طریق SaveChangesInterceptor».
/// همچنین تبدیل خودکار Remove() به Soft Delete برای انتیتی‌های ISoftDeletable.
/// </summary>
public sealed class AuditableEntitySaveChangesInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AuditableEntitySaveChangesInterceptor(ICurrentUserService currentUserService, IDateTimeProvider dateTimeProvider)
    {
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateEntities(DbContext? context)
    {
        if (context is null) return;

        var utcNow = _dateTimeProvider.UtcNow;
        var userId = _currentUserService.UserId;

        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.SetCreated(userId, utcNow);
                    break;
                case EntityState.Modified:
                    entry.Entity.SetModified(userId, utcNow);
                    break;
            }
        }

        // قانون CLAUDE.md: حذف فیزیکی ممنوع — هر Remove() به Soft Delete تبدیل می‌شود.
        foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State != EntityState.Deleted) continue;

            entry.State = EntityState.Modified;
            entry.Entity.MarkAsDeleted(userId, utcNow);
        }
    }
}
