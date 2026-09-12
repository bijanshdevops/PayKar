namespace IndustrialPlatform.Shared.Abstractions;

/// <summary>
/// طبق CLAUDE.md: «حذف فیزیکی رکوردها در دیتابیس ممنوع است؛ تمام جداول بیزینسی از این اینترفیس پیروی می‌کنند».
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; }
    DateTime? DeletedAtUtc { get; }
    Guid? DeletedBy { get; }

    void MarkAsDeleted(Guid? deletedBy, DateTime utcNow);
}
