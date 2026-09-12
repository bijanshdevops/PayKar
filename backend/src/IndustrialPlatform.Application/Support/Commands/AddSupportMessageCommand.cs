using FluentValidation;
using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Domain.Support;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Support.Commands;

/// <summary>
/// افزودن پیام جدید (و ضمیمه اختیاری) به یک تیکت پشتیبانی موجود — طبق ADR-007، گسترش‌یافته در فاز
/// «مدیریت پشتیبانی و تیکت‌ها». اگر خودِ کاربر (نه تیم پشتیبانی) پیام جدید بدهد: روی تیکت «پاسخ داده‌شده»
/// به «در انتظار پاسخ» و روی تیکت «بسته‌شده» به «بازشده مجدد» برمی‌گردد تا دوباره در صدر کار تیم پشتیبانی
/// قرار گیرد (کاربر نیازی به فراخوانی جداگانه Reopen ندارد). تیم پشتیبانی همچنان نمی‌تواند به تیکت بسته پاسخ دهد.
/// </summary>
public sealed record AddSupportMessageCommand(
    Guid TicketId, string Body, Stream? AttachmentContent = null, string? AttachmentFileName = null, long AttachmentSizeBytes = 0) : IRequest<Result<SupportMessageDto>>;

public sealed class AddSupportMessageCommandValidator : AbstractValidator<AddSupportMessageCommand>
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".pdf", ".docx" };
    private const long MaxAttachmentSizeBytes = 10 * 1024 * 1024; // ۱۰ مگابایت

    public AddSupportMessageCommandValidator()
    {
        // همان اصلاح CreateSupportTicketCommand — رفع CS8602 و null-safe مستقل از ترتیب اجرای .When().
        RuleFor(x => x.AttachmentFileName)
            .Must(name => !string.IsNullOrWhiteSpace(name) && AllowedExtensions.Contains(Path.GetExtension(name).ToLowerInvariant()))
            .WithMessage("فرمت ضمیمه باید jpg، jpeg، png، pdf یا docx باشد.")
            .When(x => !string.IsNullOrWhiteSpace(x.AttachmentFileName));
        RuleFor(x => x.AttachmentSizeBytes)
            .LessThanOrEqualTo(MaxAttachmentSizeBytes)
            .When(x => x.AttachmentSizeBytes > 0)
            .WithMessage("حجم ضمیمه نباید بیشتر از ۱۰ مگابایت باشد.");
    }
}

public sealed class AddSupportMessageCommandHandler : IRequestHandler<AddSupportMessageCommand, Result<SupportMessageDto>>
{
    private const string AdminRoleName = "Admin";

    private readonly ISupportTicketRepository _ticketRepository;
    private readonly ISupportMessageRepository _messageRepository;
    private readonly IFileStorageService _fileStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public AddSupportMessageCommandHandler(
        ISupportTicketRepository ticketRepository,
        ISupportMessageRepository messageRepository,
        IFileStorageService fileStorage,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser)
    {
        _ticketRepository = ticketRepository;
        _messageRepository = messageRepository;
        _fileStorage = fileStorage;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<Result<SupportMessageDto>> Handle(AddSupportMessageCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<SupportMessageDto>(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var ticket = await _ticketRepository.GetByIdAsync(request.TicketId, cancellationToken);
        if (ticket is null)
            return Result.Failure<SupportMessageDto>(Error.NotFound("TICKET_NOT_FOUND", "تیکت مورد نظر یافت نشد."));

        var accessCheck = SupportTicketOwnershipGuard.EnsureAccess(ticket, _currentUser);
        if (accessCheck.IsFailure)
            return Result.Failure<SupportMessageDto>(accessCheck.Error);

        var isFromSupportTeam = _currentUser.IsInRole(AdminRoleName);

        // تیم پشتیبانی نمی‌تواند به تیکت بسته‌شده پاسخ دهد؛ ابتدا باید وضعیت را (از طریق دراپ‌داون AdminOnly) تغییر دهد.
        if (isFromSupportTeam && ticket.Status == SupportTicketStatus.Closed)
            return Result.Failure<SupportMessageDto>(Error.Conflict("TICKET_CLOSED", "این تیکت بسته شده است. برای پاسخ، ابتدا وضعیت تیکت را تغییر دهید."));

        var body = request.Body?.Trim() ?? string.Empty;
        if (body.Length is < 1 or > 4000)
            return Result.Failure<SupportMessageDto>(Error.Validation("MESSAGE_INVALID_LENGTH", "متن پیام باید بین ۱ تا ۴۰۰۰ کاراکتر باشد."));

        // طبق تصمیم محصولی این فاز: پیام جدیدِ خودِ کاربر (نه تیم پشتیبانی) یعنی تیکت هنوز نیاز به پیگیری دارد —
        // از «بسته‌شده» خودکار به «بازشده مجدد» و از «پاسخ داده‌شده» خودکار به «در انتظار پاسخ» برمی‌گردد.
        if (!isFromSupportTeam)
        {
            if (ticket.Status == SupportTicketStatus.Closed)
                ticket.TransitionTo(SupportTicketStatus.Reopened);
            else if (ticket.Status == SupportTicketStatus.Answered)
                ticket.TransitionTo(SupportTicketStatus.PendingResponse);
        }

        string? attachmentUrl = null;
        if (request.AttachmentContent is not null && !string.IsNullOrWhiteSpace(request.AttachmentFileName))
            attachmentUrl = await _fileStorage.SaveAsync(request.AttachmentContent, request.AttachmentFileName, "support-attachments", cancellationToken);

        var message = SupportMessage.Create(ticket.Id, _currentUser.UserId.Value, isFromSupportTeam, body, attachmentUrl, request.AttachmentFileName);
        _messageRepository.Add(message);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(SupportMapper.ToDto(message));
    }
}
