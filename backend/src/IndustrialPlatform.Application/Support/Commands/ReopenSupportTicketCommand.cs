using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Domain.Support;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Support.Commands;

/// <summary>
/// بازگشایی تیکت بسته‌شده توسط صاحب تیکت (یا تیم پشتیبانی) — طبق درخواست محصولی «قابلیت باز شدن تیکت».
/// برخلاف UpdateSupportTicketStatusCommand (که صرفاً AdminOnly است)، این Command با ownership guard
/// به خودِ کاربر صاحب تیکت هم اجازه می‌دهد تیکت بسته‌شده‌اش را دوباره باز کند و سؤال جدید بپرسد.
/// </summary>
public sealed record ReopenSupportTicketCommand(Guid TicketId) : IRequest<Result<SupportTicketDto>>;

public sealed class ReopenSupportTicketCommandHandler : IRequestHandler<ReopenSupportTicketCommand, Result<SupportTicketDto>>
{
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public ReopenSupportTicketCommandHandler(ISupportTicketRepository ticketRepository, IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<SupportTicketDto>> Handle(ReopenSupportTicketCommand request, CancellationToken cancellationToken)
    {
        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);
        if (ticket is null)
            return Result.Failure<SupportTicketDto>(Error.NotFound("TICKET_NOT_FOUND", "تیکت مورد نظر یافت نشد."));

        var accessResult = SupportTicketOwnershipGuard.EnsureAccess(ticket, _currentUser);
        if (accessResult.IsFailure)
            return Result.Failure<SupportTicketDto>(accessResult.Error);

        var transitionResult = ticket.TransitionTo(SupportTicketStatus.Reopened);
        if (transitionResult.IsFailure)
            return Result.Failure<SupportTicketDto>(transitionResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(SupportMapper.ToDto(ticket, ticket.LastModifiedAtUtc ?? ticket.CreatedAtUtc));
    }
}
