using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Domain.Support;
using IndustrialPlatform.Shared.Api;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Support.Queries;

/// <summary>
/// محاسبه ستون «آخرین به‌روزرسانی» برای یک صفحه از تیکت‌ها بدون واکشی کامل پیام‌های هر تیکت —
/// حداکثرِ (CreatedAtUtc، LastModifiedAtUtc، جدیدترین پیام) هر تیکت. طبق فاز «مدیریت پشتیبانی و تیکت‌ها».
/// </summary>
internal static class TicketLastActivityHelper
{
    public static async Task<IReadOnlyList<SupportTicketDto>> ToDtosWithLastActivityAsync(
        IReadOnlyList<SupportTicket> tickets, ISupportMessageRepository messageRepository, CancellationToken cancellationToken)
    {
        var lastMessageTimestamps = await messageRepository.GetLastMessageTimestampsAsync(
            tickets.Select(t => t.Id).ToList(), cancellationToken);

        return tickets.Select(t =>
        {
            var lastActivity = t.LastModifiedAtUtc ?? t.CreatedAtUtc;
            if (lastMessageTimestamps.TryGetValue(t.Id, out var lastMessageAt) && lastMessageAt > lastActivity)
                lastActivity = lastMessageAt;

            return SupportMapper.ToDto(t, lastActivity);
        }).ToList();
    }
}

/// <summary>لیست تیکت‌های کاربر جاری با فیلتر اختیاری وضعیت — طبق ADR-007، گسترش‌یافته در فاز «مدیریت پشتیبانی و تیکت‌ها».</summary>
public sealed record GetMyTicketsQuery(string? Status, int Page, int PageSize) : IRequest<Result<PagedResult<SupportTicketDto>>>;

public sealed class GetMyTicketsQueryHandler : IRequestHandler<GetMyTicketsQuery, Result<PagedResult<SupportTicketDto>>>
{
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly ISupportMessageRepository _messageRepository;
    private readonly ICurrentUserService _currentUser;

    public GetMyTicketsQueryHandler(
        ISupportTicketRepository ticketRepository, ISupportMessageRepository messageRepository, ICurrentUserService currentUser)
    {
        _ticketRepository = ticketRepository;
        _messageRepository = messageRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<PagedResult<SupportTicketDto>>> Handle(GetMyTicketsQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<PagedResult<SupportTicketDto>>(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        SupportTicketStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<SupportTicketStatus>(request.Status, out var parsed))
                return Result.Failure<PagedResult<SupportTicketDto>>(Error.Validation("VALIDATION_ERROR", "وضعیت فیلتر نامعتبر است."));

            statusFilter = parsed;
        }

        var paged = await _ticketRepository.GetByUserIdAsync(_currentUser.UserId.Value, statusFilter, request.Page, request.PageSize, cancellationToken);
        var dtoItems = await TicketLastActivityHelper.ToDtosWithLastActivityAsync(paged.Items, _messageRepository, cancellationToken);

        return Result.Success(PagedResult<SupportTicketDto>.Create(dtoItems, paged.TotalCount, paged.Page, paged.PageSize));
    }
}

/// <summary>شمارنده تیکت‌های کاربر جاری به تفکیک وضعیت — برای تب‌های فیلتر صفحه «تیکت‌های من».</summary>
public sealed record GetMyTicketStatusSummaryQuery : IRequest<Result<SupportTicketStatusSummaryDto>>;

public sealed class GetMyTicketStatusSummaryQueryHandler : IRequestHandler<GetMyTicketStatusSummaryQuery, Result<SupportTicketStatusSummaryDto>>
{
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly ICurrentUserService _currentUser;

    public GetMyTicketStatusSummaryQueryHandler(ISupportTicketRepository ticketRepository, ICurrentUserService currentUser)
    {
        _ticketRepository = ticketRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<SupportTicketStatusSummaryDto>> Handle(GetMyTicketStatusSummaryQuery request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<SupportTicketStatusSummaryDto>(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var counts = await _ticketRepository.GetStatusCountsByUserIdAsync(_currentUser.UserId.Value, cancellationToken);
        return Result.Success(SupportMapper.ToSummaryDto(counts));
    }
}

/// <summary>جزئیات یک تیکت به‌همراه تمام پیام‌ها — دسترسی: صاحب تیکت یا Admin (طبق ADR-007).</summary>
public sealed record GetTicketByIdQuery(Guid TicketId) : IRequest<Result<SupportTicketDetailDto>>;

public sealed class GetTicketByIdQueryHandler : IRequestHandler<GetTicketByIdQuery, Result<SupportTicketDetailDto>>
{
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly ISupportMessageRepository _messageRepository;
    private readonly ICurrentUserService _currentUser;

    public GetTicketByIdQueryHandler(
        ISupportTicketRepository ticketRepository, ISupportMessageRepository messageRepository, ICurrentUserService currentUser)
    {
        _ticketRepository = ticketRepository;
        _messageRepository = messageRepository;
        _currentUser = currentUser;
    }

    public async Task<Result<SupportTicketDetailDto>> Handle(GetTicketByIdQuery request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);
        if (ticket is null)
            return Result.Failure<SupportTicketDetailDto>(Error.NotFound("TICKET_NOT_FOUND", "تیکت مورد نظر یافت نشد."));

        var accessCheck = SupportTicketOwnershipGuard.EnsureAccess(ticket, _currentUser);
        if (accessCheck.IsFailure)
            return Result.Failure<SupportTicketDetailDto>(accessCheck.Error);

        var messages = await _messageRepository.GetByTicketIdAsync(ticket.Id, cancellationToken);
        return Result.Success(SupportMapper.ToDetailDto(ticket, messages));
    }
}

/// <summary>لیست همه تیکت‌ها برای پنل پشتیبانی (Admin) با فیلتر اختیاری وضعیت — طبق ADR-007.</summary>
public sealed record GetAllTicketsQuery(string? Status, int Page, int PageSize) : IRequest<Result<PagedResult<SupportTicketDto>>>;

public sealed class GetAllTicketsQueryHandler : IRequestHandler<GetAllTicketsQuery, Result<PagedResult<SupportTicketDto>>>
{
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly ISupportMessageRepository _messageRepository;

    public GetAllTicketsQueryHandler(ISupportTicketRepository ticketRepository, ISupportMessageRepository messageRepository)
    {
        _ticketRepository = ticketRepository;
        _messageRepository = messageRepository;
    }

    public async Task<Result<PagedResult<SupportTicketDto>>> Handle(GetAllTicketsQuery request, CancellationToken cancellationToken)
    {
        SupportTicketStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (!Enum.TryParse<SupportTicketStatus>(request.Status, out var parsed))
                return Result.Failure<PagedResult<SupportTicketDto>>(Error.Validation("VALIDATION_ERROR", "وضعیت فیلتر نامعتبر است."));

            statusFilter = parsed;
        }

        var paged = await _ticketRepository.GetAllAsync(statusFilter, request.Page, request.PageSize, cancellationToken);
        var dtoItems = await TicketLastActivityHelper.ToDtosWithLastActivityAsync(paged.Items, _messageRepository, cancellationToken);

        return Result.Success(PagedResult<SupportTicketDto>.Create(dtoItems, paged.TotalCount, paged.Page, paged.PageSize));
    }
}

/// <summary>شمارنده همه تیکت‌ها (همه کاربران) به تفکیک وضعیت — برای پنل پشتیبانی (Admin).</summary>
public sealed record GetAllTicketStatusSummaryQuery : IRequest<Result<SupportTicketStatusSummaryDto>>;

public sealed class GetAllTicketStatusSummaryQueryHandler : IRequestHandler<GetAllTicketStatusSummaryQuery, Result<SupportTicketStatusSummaryDto>>
{
    private readonly ISupportTicketRepository _ticketRepository;

    public GetAllTicketStatusSummaryQueryHandler(ISupportTicketRepository ticketRepository) => _ticketRepository = ticketRepository;

    public async Task<Result<SupportTicketStatusSummaryDto>> Handle(GetAllTicketStatusSummaryQuery request, CancellationToken cancellationToken)
    {
        var counts = await _ticketRepository.GetStatusCountsAsync(cancellationToken);
        return Result.Success(SupportMapper.ToSummaryDto(counts));
    }
}
