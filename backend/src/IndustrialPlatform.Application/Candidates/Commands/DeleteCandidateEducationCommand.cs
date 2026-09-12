using FluentValidation;
using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Candidates.Commands;

/// <summary>
/// حذف منطقی یک ردیف سابقه تحصیلی — طبق مانیفست معماری (بخش ۲.۳): حذف فیزیکی ممنوع است؛ از
/// BaseEntity.MarkAsDeleted استفاده می‌شود. فقط برای کارجوی صاحب همان ردیف.
/// </summary>
public sealed record DeleteCandidateEducationCommand(Guid EducationId) : IRequest<Result>;

public sealed class DeleteCandidateEducationCommandValidator : AbstractValidator<DeleteCandidateEducationCommand>
{
    public DeleteCandidateEducationCommandValidator()
    {
        RuleFor(x => x.EducationId).NotEmpty();
    }
}

public sealed class DeleteCandidateEducationCommandHandler : IRequestHandler<DeleteCandidateEducationCommand, Result>
{
    private readonly ICandidateRepository _candidateRepository;
    private readonly ICandidateEducationRepository _educationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteCandidateEducationCommandHandler(
        ICandidateRepository candidateRepository,
        ICandidateEducationRepository educationRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _candidateRepository = candidateRepository;
        _educationRepository = educationRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteCandidateEducationCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var education = await _educationRepository.GetByIdAsync(request.EducationId, cancellationToken);
        if (education is null)
            return Result.Failure(Error.NotFound("EDUCATION_NOT_FOUND", "سابقه تحصیلی مورد نظر یافت نشد."));

        var candidate = await _candidateRepository.GetByIdAsync(education.CandidateId, cancellationToken);
        if (candidate is null || candidate.UserId != _currentUser.UserId.Value)
            return Result.Failure(Error.Forbidden("FORBIDDEN", "شما اجازه حذف این سابقه تحصیلی را ندارید."));

        education.MarkAsDeleted(_currentUser.UserId, _dateTimeProvider.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
