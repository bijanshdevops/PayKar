using FluentValidation;
using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Candidates.Commands;

/// <summary>
/// حذف منطقی یک ردیف مدرک/گواهینامه — طبق مانیفست معماری (بخش ۲.۳): حذف فیزیکی ممنوع است؛ از
/// BaseEntity.MarkAsDeleted استفاده می‌شود. فقط برای کارجوی صاحب همان ردیف. هم‌راستا با
/// DeleteCandidateEducationCommand.
/// </summary>
public sealed record DeleteCandidateCertificationCommand(Guid CertificationId) : IRequest<Result>;

public sealed class DeleteCandidateCertificationCommandValidator : AbstractValidator<DeleteCandidateCertificationCommand>
{
    public DeleteCandidateCertificationCommandValidator()
    {
        RuleFor(x => x.CertificationId).NotEmpty();
    }
}

public sealed class DeleteCandidateCertificationCommandHandler : IRequestHandler<DeleteCandidateCertificationCommand, Result>
{
    private readonly ICandidateRepository _candidateRepository;
    private readonly ICandidateCertificationRepository _certificationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteCandidateCertificationCommandHandler(
        ICandidateRepository candidateRepository,
        ICandidateCertificationRepository certificationRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _candidateRepository = candidateRepository;
        _certificationRepository = certificationRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteCandidateCertificationCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var certification = await _certificationRepository.GetByIdAsync(request.CertificationId, cancellationToken);
        if (certification is null)
            return Result.Failure(Error.NotFound("CERTIFICATION_NOT_FOUND", "مدرک/گواهینامه مورد نظر یافت نشد."));

        var candidate = await _candidateRepository.GetByIdAsync(certification.CandidateId, cancellationToken);
        if (candidate is null || candidate.UserId != _currentUser.UserId.Value)
            return Result.Failure(Error.Forbidden("FORBIDDEN", "شما اجازه حذف این مدرک/گواهینامه را ندارید."));

        certification.MarkAsDeleted(_currentUser.UserId, _dateTimeProvider.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
