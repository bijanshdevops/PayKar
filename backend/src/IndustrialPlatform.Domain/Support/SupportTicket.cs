using IndustrialPlatform.Shared.Entities;
using IndustrialPlatform.Shared.Results;

namespace IndustrialPlatform.Domain.Support;

/// <summary>
/// تیکت پشتیبانی کاربر↔تیم پلتفرم (فانوس سبز ارتباطات) — Aggregate Root طبق ADR-007،
/// گسترش‌یافته در فاز «مدیریت پشتیبانی و تیکت‌ها» (TicketNumber/Department/Priority + مدل ۵ وضعیتی).
/// UserId صرفاً یک شناسه Guid خام است چون طبق ماتریس وابستگی سند 01-Architecture،
/// Domain/Application اجازه ارجاع به Identity را ندارند.
/// </summary>
public sealed class SupportTicket : AggregateRoot<Guid>
{
    private static readonly Dictionary<SupportTicketStatus, SupportTicketStatus[]> AllowedTransitions = new()
    {
        [SupportTicketStatus.PendingResponse] = new[] { SupportTicketStatus.InProgress, SupportTicketStatus.Answered, SupportTicketStatus.Closed },
        [SupportTicketStatus.InProgress] = new[] { SupportTicketStatus.Answered, SupportTicketStatus.Closed },
        // PendingResponse نیز مقصد مجاز است: وقتی کاربر (نه تیم پشتیبانی) روی تیکت پاسخ‌داده‌شده پیام جدید
        // می‌دهد، AddSupportMessageCommand این انتقال را خودکار انجام می‌دهد (طبق فاز «مدیریت پشتیبانی و تیکت‌ها»).
        [SupportTicketStatus.Answered] = new[] { SupportTicketStatus.Closed, SupportTicketStatus.Reopened, SupportTicketStatus.PendingResponse },
        [SupportTicketStatus.Closed] = new[] { SupportTicketStatus.Reopened },
        [SupportTicketStatus.Reopened] = new[] { SupportTicketStatus.InProgress, SupportTicketStatus.Answered, SupportTicketStatus.Closed }
    };

    public Guid UserId { get; private set; }

    /// <summary>
    /// شماره موبایل کاربر ثبت‌کننده در لحظه ایجاد تیکت — Denormalized از ICurrentUserService.MobileNumber
    /// (مشابه الگوی Candidate.Email/City)، چون Domain/Application اجازه ارجاع به Identity برای دریافت
    /// اطلاعات تماس کاربر را ندارند. برای نمایش در کارت «کاربر مرتبط» صفحه جزئیات تیکت (پنل ادمین).
    /// </summary>
    public string RequesterMobileNumber { get; private set; } = string.Empty;

    /// <summary>شناسه خوانا و یکتای تیکت (مثلاً «TK-1403-0012») — تولید در Application layer، طبق فاز «مدیریت پشتیبانی و تیکت‌ها».</summary>
    public string TicketNumber { get; private set; } = string.Empty;

    public string Subject { get; private set; } = string.Empty;

    /// <summary>دپارتمان/بخش مربوطه — طبق فاز «مدیریت پشتیبانی و تیکت‌ها».</summary>
    public TicketDepartment Department { get; private set; }

    /// <summary>اولویت تیکت — طبق فاز «مدیریت پشتیبانی و تیکت‌ها».</summary>
    public TicketPriority Priority { get; private set; }

    public SupportTicketStatus Status { get; private set; }

    private SupportTicket() { }

    private SupportTicket(Guid id, Guid userId, string requesterMobileNumber, string ticketNumber, string subject, TicketDepartment department, TicketPriority priority) : base(id)
    {
        UserId = userId;
        RequesterMobileNumber = requesterMobileNumber;
        TicketNumber = ticketNumber;
        Subject = subject;
        Department = department;
        Priority = priority;
        Status = SupportTicketStatus.PendingResponse;
    }

    public static SupportTicket Create(Guid userId, string requesterMobileNumber, string ticketNumber, string subject, TicketDepartment department, TicketPriority priority) =>
        new(Guid.NewGuid(), userId, requesterMobileNumber ?? string.Empty, ticketNumber, subject, department, priority);

    public Result TransitionTo(SupportTicketStatus newStatus)
    {
        if (!AllowedTransitions.TryGetValue(Status, out var allowed) || !allowed.Contains(newStatus))
        {
            return Result.Failure(Error.Conflict(
                "INVALID_TICKET_STATUS_TRANSITION",
                $"انتقال وضعیت تیکت از {Status} به {newStatus} مجاز نیست."));
        }

        Status = newStatus;
        return Result.Success();
    }
}
