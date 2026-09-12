using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Domain.Support;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Support.Commands;

/// <summary>
/// بستن تیکت توسط خودِ صاحب تیکت (یا تیم پشتیبانی) — طبق فاز «مدیریت پشتیبانی و تیکت‌ها»: کاربر عادی
/// دسترسی به دراپ‌داون «تغییر وضعیت» (که صرفاً AdminOnly است) ندارد و فقط می‌تواند تیکت خودش را ببندد.
/// مشابه ReopenSupportTicketCommand، با ownership guard به‌جای AdminOnly.
/// </summary>
public sealed record CloseSupportTicketCommand(Guid TicketId) : IRequest<Result<SupportTicketDto>>;

public sealed class CloseSupportTicketCommandHandler : IRequestHandler<CloseSupportTicketCommand, Result<SupportTicketDto>>
{
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CloseSupportTicketCommandHandler(ISupportTicketRepository ticketRepository, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<SupportTicketDto>> Handle(CloseSupportTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);
        if (ticket is null)
            return Result.Failure<SupportTicketDto>(Error.NotFound("TICKET_NOT_FOUND", "تیکت مورد نظر یافت نشد."));

        var accessResult = SupportTicketOwnershipGuard.EnsureAccess(ticket, _currentUser);
        if (accessResult.IsFailure)
            return Result.Failure<SupportTicketDto>(accessResult.Error);

        var transitionResult = ticket.TransitionTo(SupportTicketStatus.Closed);
        if (transitionResult.IsFailure)
            return Result.Failure<SupportTicketDto>(transitionResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(SupportMapper.ToDto(ticket, ticket.LastModifiedAtUtc ?? ticket.CreatedAtUtc));
    }
}
