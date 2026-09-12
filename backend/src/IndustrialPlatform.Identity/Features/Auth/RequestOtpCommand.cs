using FluentValidation;
using IndustrialPlatform.Identity.Services;
using IndustrialPlatform.Shared.Extensions;
using IndustrialPlatform.Shared.Results;
using MediatR;

namespace IndustrialPlatform.Identity.Features.Auth;

public sealed record RequestOtpCommand(string MobileNumber) : IRequest<Result<RequestOtpResponse>>;

public sealed class RequestOtpCommandValidator : AbstractValidator<RequestOtpCommand>
{
    public RequestOtpCommandValidator()
    {
        RuleFor(x => x.MobileNumber)
            .NotEmpty().WithMessage("شماره موبایل الزامی است.")
            .Must(m => m.IsValidIranianMobileNumber())
            .WithMessage("شماره موبایل معتبر نیست (نمونه صحیح: 09121234567).");
    }
}

public sealed class RequestOtpCommandHandler : IRequestHandler<RequestOtpCommand, Result<RequestOtpResponse>>
{
    private readonly IOtpService _otpService;

    public RequestOtpCommandHandler(IOtpService otpService)
    {
        _otpService = otpService;
    }

    public async Task<Result<RequestOtpResponse>> Handle(RequestOtpCommand request, CancellationToken cancellationToken)
    {
        var result = await _otpService.RequestOtpAsync(request.MobileNumber.NormalizePersian(), cancellationToken);

        return result.IsSuccess
            ? Result.Success(new RequestOtpResponse(result.Value))
            : Result.Failure<RequestOtpResponse>(result.Error);
    }
}
