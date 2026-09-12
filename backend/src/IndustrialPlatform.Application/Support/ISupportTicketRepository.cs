using IndustrialPlatform.Domain.Support;
using IndustrialPlatform.Shared.Api;

namespace IndustrialPlatform.Application.Support;

public interface ISupportTicketRepository
{
    Task<SupportTicket?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedResult<SupportTicket>> GetByUserIdAsync(
        Guid userId, SupportTicketStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);

    Task<PagedResult<SupportTicket>> GetAllAsync(SupportTicketStatus? status, int page, int pageSize, CancellationToken cancellationToken = default);

    /// <summary>شمارش تیکت‌های یک کاربر به تفکیک وضعیت — برای شمارنده‌های تب فیلتر صفحه «تیکت‌های من».</summary>
    Task<IReadOnlyDictionary<SupportTicketStatus, int>> GetStatusCountsByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>شمارش همه تیکت‌ها (همه کاربران) به تفکیک وضعیت — برای پنل پشتیبانی (Admin).</summary>
    Task<IReadOnlyDictionary<SupportTicketStatus, int>> GetStatusCountsAsync(CancellationToken cancellationToken = default);

    /// <summary>تعداد تیکت‌های ثبت‌شده در یک سال میلادی مشخص — برای تولید TicketNumber (مثلاً «TK-2026-0001»).</summary>
    Task<int> CountByCreationYearAsync(int year, CancellationToken cancellationToken = default);

    void Add(SupportTicket ticket);
}
