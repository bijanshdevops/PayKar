using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Domain.Support;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Support.Commands;

/// <summary>تغییر وضعیت تیکت توسط تیم پشتیبانی — دسترسی «AdminOnly» در سطح Endpoint اعمال می‌شود (طبق ADR-007).</summary>
public sealed record UpdateSupportTicketStatusCommand(Guid TicketId, string NewStatus) : IRequest<Result<SupportTicketDto>>;

public sealed class UpdateSupportTicketStatusCommandHandler : IRequestHandler<UpdateSupportTicketStatusCommand, Result<SupportTicketDto>>
{
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateSupportTicketStatusCommandHandler(ISupportTicketRepository ticketRepository, IUnitOfWork unitOfWork)
    {
        _ticketRepository = ticketRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<SupportTicketDto>> Handle(UpdateSupportTicketStatusCommand request, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<SupportTicketStatus>(request.NewStatus, out var newStatus))
            return Result.Failure<SupportTicketDto>(Error.Validation("VALIDATION_ERROR", "وضعیت تیکت نامعتبر است."));

        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);
        if (ticket is null)
            return Result.Failure<SupportTicketDto>(Error.NotFound("TICKET_NOT_FOUND", "تیکت مورد نظر یافت نشد."));

        var transitionResult = ticket.TransitionTo(newStatus);
        if (transitionResult.IsFailure)
            return Result.Failure<SupportTicketDto>(transitionResult.Error);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(SupportMapper.ToDto(ticket, ticket.LastModifiedAtUtc ?? ticket.CreatedAtUtc));
    }
}
