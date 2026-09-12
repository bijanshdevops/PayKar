using IndustrialPlatform.Domain.Support;

namespace IndustrialPlatform.Application.Support;

public interface ISupportMessageRepository
{
    Task<IReadOnlyList<SupportMessage>> GetByTicketIdAsync(Guid ticketId, CancellationToken cancellationToken = default);

    /// <summary>
    /// جدیدترین زمان ارسال پیام برای هر یک از تیکت‌های داده‌شده — برای محاسبه ستون «آخرین به‌روزرسانی»
    /// در صفحات لیست، بدون واکشی کامل پیام‌های هر تیکت. تیکت‌های بدون پیامِ اضافه در نتیجه غایب‌اند.
    /// </summary>
    Task<IReadOnlyDictionary<Guid, DateTime>> GetLastMessageTimestampsAsync(IReadOnlyCollection<Guid> ticketIds, CancellationToken cancellationToken = default);

    void Add(SupportMessage message);
}
