using FluentValidation;
using IndustrialPlatform.Identity.Persistence;
using IndustrialPlatform.Identity.Services;
using IndustrialPlatform.Shared.Extensions;
using IndustrialPlatform.Shared.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IndustrialPlatform.Identity.Features.Auth;

/// <summary>
/// بازیابی رمز عبور فراموش‌شده — طبق ADR-006. مرحله اول (ارسال کد) از همان اندپوینت موجود
/// otp/request استفاده می‌شود؛ این کامند فقط مرحله دوم (تایید کد + جایگزینی رمز) را انجام می‌دهد.
/// طبق اصل عدم افشای وجود حساب، برای «کد نادرست» و «حساب/رمز قابل‌بازیابی نیست» پیام یکسان برگردانده می‌شود.
/// </summary>
public sealed record ResetPasswordCommand(string MobileNumber, string OtpCode, string NewPassword) : IRequest<Result>;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.MobileNumber)
            .NotEmpty().WithMessage("شماره موبایل الزامی است.")
            .Must(m => m.IsValidIranianMobileNumber())
            .WithMessage("شماره موبایل معتبر نیست.");

        RuleFor(x => x.OtpCode)
            .NotEmpty().WithMessage("کد تایید الزامی است.")
            .Length(6).WithMessage("کد تایید باید ۶ رقم باشد.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("رمز عبور جدید الزامی است.")
            .MinimumLength(8).WithMessage("رمز عبور باید حداقل ۸ کاراکتر باشد.");
    }
}

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IOtpService _otpService;
    private readonly IdentityDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;

    public ResetPasswordCommandHandler(IOtpService otpService, IdentityDbContext dbContext, IPasswordHasher passwordHasher)
    {
        _otpService = otpService;
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var mobileNumber = request.MobileNumber.NormalizePersian();

        var verifyResult = await _otpService.VerifyOtpAsync(mobileNumber, request.OtpCode, cancellationToken);
        if (verifyResult.IsFailure)
            return Result.Failure(verifyResult.Error);

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.MobileNumber == mobileNumber, cancellationToken);

        if (user is null || string.IsNullOrEmpty(user.Username))
        {
            return Result.Failure(Error.Conflict(
                "PASSWORD_RESET_NOT_AVAILABLE",
                "امکان بازیابی رمز عبور برای این شماره وجود ندارد. ابتدا باید از طریق ورود با کد پیامکی، نام‌کاربری و رمز عبور تنظیم کنید."));
        }

        if (!user.IsActive)
            return Result.Failure(Error.Forbidden("USER_DEACTIVATED", "این حساب کاربری غیرفعال شده است."));

        user.ResetPassword(_passwordHasher.Hash(request.NewPassword));

        if (!user.IsMobileVerified)
            user.MarkMobileVerified();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
