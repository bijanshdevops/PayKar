using FluentValidation;
using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Application.Candidates.Commands;

/// <summary>
/// حذف منطقی یک ردیف سابقه شغلی — طبق مانیفست معماری (بخش ۲.۳): حذف فیزیکی ممنوع است؛ از
/// BaseEntity.MarkAsDeleted استفاده می‌شود. فقط برای کارجوی صاحب همان ردیف.
/// </summary>
public sealed record DeleteCandidateWorkExperienceCommand(Guid WorkExperienceId) : IRequest<Result>;

public sealed class DeleteCandidateWorkExperienceCommandValidator : AbstractValidator<DeleteCandidateWorkExperienceCommand>
{
    public DeleteCandidateWorkExperienceCommandValidator()
    {
        RuleFor(x => x.WorkExperienceId).NotEmpty();
    }
}

public sealed class DeleteCandidateWorkExperienceCommandHandler : IRequestHandler<DeleteCandidateWorkExperienceCommand, Result>
{
    private readonly ICandidateRepository _candidateRepository;
    private readonly ICandidateWorkExperienceRepository _workExperienceRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IDateTimeProvider _dateTimeProvider;

    public DeleteCandidateWorkExperienceCommandHandler(
        ICandidateRepository candidateRepository,
        ICandidateWorkExperienceRepository workExperienceRepository,
        IUnitOfWork unitOfWork,
        ICurrentUserService currentUser,
        IDateTimeProvider dateTimeProvider)
    {
        _candidateRepository = candidateRepository;
        _workExperienceRepository = workExperienceRepository;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<Result> Handle(DeleteCandidateWorkExperienceCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var workExperience = await _workExperienceRepository.GetByIdAsync(request.WorkExperienceId, cancellationToken);
        if (workExperience is null)
            return Result.Failure(Error.NotFound("WORK_EXPERIENCE_NOT_FOUND", "سابقه شغلی مورد نظر یافت نشد."));

        var candidate = await _candidateRepository.GetByIdAsync(workExperience.CandidateId, cancellationToken);
        if (candidate is null || candidate.UserId != _currentUser.UserId.Value)
            return Result.Failure(Error.Forbidden("FORBIDDEN", "شما اجازه حذف این سابقه شغلی را ندارید."));

        workExperience.MarkAsDeleted(_currentUser.UserId, _dateTimeProvider.UtcNow);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
