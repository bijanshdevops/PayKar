using FluentValidation;
using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Domain.Support;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Support.Commands;

/// <summary>
/// ایجاد تیکت پشتیبانی جدید همراه با اولین پیام (و ضمیمه اختیاری) — طبق ADR-007، گسترش‌یافته در فاز
/// «مدیریت پشتیبانی و تیکت‌ها» با افزودن Department/Priority و تولید TicketNumber خوانا.
/// </summary>
public sealed record CreateSupportTicketCommand(
    string Subject,
    string Message,
    string Department,
    string Priority,
    Stream? AttachmentContent = null,
    string? AttachmentFileName = null,
    long AttachmentSizeBytes = 0) : IRequest<Result<SupportTicketDetailDto>>;

public sealed class CreateSupportTicketCommandValidator : AbstractValidator<CreateSupportTicketCommand>
{
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".pdf", ".docx" };
    private const long MaxAttachmentSizeBytes = 10 * 1024 * 1024; // ۱۰ مگابایت

    public CreateSupportTicketCommandValidator()
    {
        RuleFor(x => x.Subject).NotEmpty();
        RuleFor(x => x.Message).NotEmpty();
        RuleFor(x => x.Department).IsEnumName(typeof(TicketDepartment)).WithMessage("دپارتمان انتخابی نامعتبر است.");
        RuleFor(x => x.Priority).IsEnumName(typeof(TicketPriority)).WithMessage("اولویت انتخابی نامعتبر است.");
        // پیش از این اصلاح، فراخوانی Path.GetExtension(name).ToLowerInvariant() صرفاً به تضمین اجرای
        // شرطی .When() برای امنِ‌بودن در برابر null متکی بود — که هشدار کامپایلر CS8602 را در پی
        // داشت (چون تحلیل Nullable ایستا نمی‌تواند رفتار زمان‌اجرای .When() را ببیند). اکنون خودِ
        // Predicate هم مستقل و به‌صورت null-safe نوشته شده (با الگوی NotNullWhen استاندارد
        // string.IsNullOrWhiteSpace)، هم هشدار برطرف شده و هم دیگر به ترتیب اجرای FluentValidation
        // وابسته نیست.
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

public sealed class CreateSupportTicketCommandHandler : IRequestHandler<CreateSupportTicketCommand, Result<SupportTicketDetailDto>>
{
    private readonly ISupportTicketRepository _ticketRepository;
    private readonly ISupportMessageRepository _messageRepository;
    private readonly IFileStorageService _fileStorage;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public CreateSupportTicketCommandHandler(
        ISupportTicketRepository ticketRepository,
        ISupportMessageRepository messageRepository,
        IFileStorageService fileStorage,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _ticketRepository = ticketRepository;
        _messageRepository = messageRepository;
        _fileStorage = fileStorage;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result<SupportTicketDetailDto>> Handle(CreateSupportTicketCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<SupportTicketDetailDto>(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var subject = request.Subject?.Trim() ?? string.Empty;
        var message = request.Message?.Trim() ?? string.Empty;

        if (subject.Length is < 3 or > 200)
            return Result.Failure<SupportTicketDetailDto>(Error.Validation("SUBJECT_INVALID_LENGTH", "موضوع تیکت باید بین ۳ تا ۲۰۰ کاراکتر باشد."));

        if (message.Length is < 5 or > 4000)
            return Result.Failure<SupportTicketDetailDto>(Error.Validation("MESSAGE_INVALID_LENGTH", "متن پیام باید بین ۵ تا ۴۰۰۰ کاراکتر باشد."));

        // طبق CLAUDE.md («الگوی نتیجه»): خطاهای ولیدیشن نباید Exception پرتاب کنند. پیش از این خط با
        // Enum.Parse (نسخهٔ throw-کننده) نوشته شده بود که کاملاً به اجرای موفق ولیدیتور بالا (IsEnumName)
        // متکی بود؛ اگر به هر دلیلی (مثلاً رجیستر نشدن ValidationBehavior در DI، یا فراخوانی این Handler
        // از مسیری غیر از HTTP Pipeline مثل تست واحد) ولیدیشن اجرا نشود، همین خط با یک ArgumentException
        // مدیریت‌نشده کل درخواست را با خطای 500 متوقف می‌کرد. اکنون با TryParse به‌صورت مستقل و ایمن
        // بازنویسی شده و در صورت شکست، یک Result.Failure کنترل‌شده (نه Exception) برمی‌گرداند.
        if (!Enum.TryParse<TicketDepartment>(request.Department, out var department))
            return Result.Failure<SupportTicketDetailDto>(Error.Validation("DEPARTMENT_INVALID", "دپارتمان انتخابی نامعتبر است."));
        if (!Enum.TryParse<TicketPriority>(request.Priority, out var priority))
            return Result.Failure<SupportTicketDetailDto>(Error.Validation("PRIORITY_INVALID", "اولویت انتخابی نامعتبر است."));

        var year = _dateTimeProvider.UtcNow.Year;
        var countForYear = await _ticketRepository.CountByCreationYearAsync(year, cancellationToken);
        var ticketNumber = TicketNumberGenerator.Generate(year, countForYear);

        var ticket = SupportTicket.Create(_currentUser.UserId.Value, _currentUser.MobileNumber ?? string.Empty, ticketNumber, subject, department, priority);
        _ticketRepository.Add(ticket);

        string? attachmentUrl = null;
        if (request.AttachmentContent is not null && !string.IsNullOrWhiteSpace(request.AttachmentFileName))
            attachmentUrl = await _fileStorage.SaveAsync(request.AttachmentContent, request.AttachmentFileName, "support-attachments", cancellationToken);

        var firstMessage = SupportMessage.Create(
            ticket.Id, _currentUser.UserId.Value, isFromSupportTeam: false, message, attachmentUrl, request.AttachmentFileName);
        _messageRepository.Add(firstMessage);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(SupportMapper.ToDetailDto(ticket, new[] { firstMessage }));
    }
}
