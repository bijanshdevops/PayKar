namespace IndustrialPlatform.Shared.Abstractions;

/// <summary>
/// طبق سند 03-Database-Standards.md: ستون‌های حسابرسی اجباری روی تمام انتیتی‌های بیزینسی.
/// این فیلدها توسط SaveChangesInterceptor لایه Persistence به‌صورت خودکار پر می‌شوند.
/// </summary>
public interface IAuditableEntity
{
    DateTime CreatedAtUtc { get; }
    Guid? CreatedBy { get; }
    DateTime? LastModifiedAtUtc { get; }
    Guid? LastModifiedBy { get; }

    void SetCreated(Guid? userId, DateTime utcNow);
    void SetModified(Guid? userId, DateTime utcNow);
}
