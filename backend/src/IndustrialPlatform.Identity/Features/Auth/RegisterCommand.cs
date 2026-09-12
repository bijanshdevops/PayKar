using System.Text.Json.Serialization;
using FluentValidation;
using IndustrialPlatform.Identity.Entities;
using IndustrialPlatform.Identity.Persistence;
using IndustrialPlatform.Identity.Services;
using IndustrialPlatform.Shared.Extensions;
using IndustrialPlatform.Shared.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IndustrialPlatform.Identity.Features.Auth;

/// <summary>
/// نوع حساب انتخاب‌شده توسط کاربر هنگام ثبت‌نام کامل — طبق ADR-010.
/// با JsonStringEnumConverter محلی (نه سراسری) سریالایز می‌شود تا در بدنه JSON درخواست
/// به‌صورت رشته ("Candidate"/"Company") ارسال شود، هم‌راستا با سایر Enum های رشته‌ای پروژه
/// (مثل VerificationStatus)، بدون تغییر رفتار پیش‌فرض عددی سایر Enum های سیستم.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum RegistrationAccountType
{
    Candidate = 1,
    Company = 2
}

/// <summary>
/// ثبت‌نام کامل با نام‌کاربری+ایمیل+موبایل+رمزعبور — طبق ADR-006. برخلاف مسیر سریع OTP (ADR-003)
/// که با تایید کد بلافاصله حساب می‌سازد، این مسیر ابتدا حساب را با IsMobileVerified=false می‌سازد
/// و سپس یک کد OTP برای تایید مالکیت شماره موبایل ارسال می‌کند (تکمیل با همان اندپوینت otp/verify).
/// طبق ADR-010: کاربر نوع حساب (کارجو/شرکت) را انتخاب می‌کند و همان نقش در لحظه ثبت‌نام assign می‌شود؛
/// انتخاب «شرکت» صرفاً نقش CompanyManager می‌دهد — ساخت پروفایل واقعی Company و احراز مدارک همچنان
/// مرحله جداگانه‌ای است (POST /api/v1/companies) که کاربر پس از ورود انجام می‌دهد.
/// </summary>
public sealed record RegisterCommand(
    string Username,
    string Email,
    string MobileNumber,
    string Password,
    RegistrationAccountType AccountType = RegistrationAccountType.Candidate) : IRequest<Result<RequestOtpResponse>>;

public sealed class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("نام‌کاربری الزامی است.")
            .Matches("^[a-zA-Z0-9_]{4,30}$").WithMessage("نام‌کاربری باید بین ۴ تا ۳۰ کاراکتر و شامل حروف انگلیسی، عدد و _ باشد.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("ایمیل الزامی است.")
            .EmailAddress().WithMessage("ایمیل معتبر نیست.");

        RuleFor(x => x.MobileNumber)
            .NotEmpty().WithMessage("شماره موبایل الزامی است.")
            .Must(m => m.IsValidIranianMobileNumber())
            .WithMessage("شماره موبایل معتبر نیست (نمونه صحیح: 09121234567).");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("رمز عبور الزامی است.")
            .MinimumLength(8).WithMessage("رمز عبور باید حداقل ۸ کاراکتر باشد.");

        RuleFor(x => x.AccountType)
            .IsInEnum().WithMessage("نوع حساب نامعتبر است.");
    }
}

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, Result<RequestOtpResponse>>
{
    private readonly IdentityDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IOtpService _otpService;

    public RegisterCommandHandler(IdentityDbContext dbContext, IPasswordHasher passwordHasher, IOtpService otpService)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _otpService = otpService;
    }

    public async Task<Result<RequestOtpResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var normalizedUsername = request.Username.Trim().ToLowerInvariant();
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();
        var mobileNumber = request.MobileNumber.NormalizePersian();

        if (await _dbContext.Users.AnyAsync(u => u.Username == normalizedUsername, cancellationToken))
            return Result.Failure<RequestOtpResponse>(Error.Conflict("USERNAME_TAKEN", "این نام‌کاربری قبلاً استفاده شده است."));

        if (await _dbContext.Users.AnyAsync(u => u.Email == normalizedEmail, cancellationToken))
            return Result.Failure<RequestOtpResponse>(Error.Conflict("EMAIL_TAKEN", "این ایمیل قبلاً استفاده شده است."));

        if (await _dbContext.Users.AnyAsync(u => u.MobileNumber == mobileNumber, cancellationToken))
            return Result.Failure<RequestOtpResponse>(Error.Conflict("MOBILE_TAKEN", "این شماره موبایل قبلاً ثبت شده است."));

        var passwordHash = _passwordHasher.Hash(request.Password);
        var user = User.CreateWithCredentials(mobileNumber, normalizedUsername, normalizedEmail, passwordHash);

        var roleName = request.AccountType == RegistrationAccountType.Company ? Role.CompanyManager : Role.Candidate;
        var selectedRole = await _dbContext.Roles.FirstAsync(r => r.Name == roleName, cancellationToken);
        user.AssignRole(selectedRole.Id);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var otpResult = await _otpService.RequestOtpAsync(mobileNumber, cancellationToken);
        if (otpResult.IsFailure)
            return Result.Failure<RequestOtpResponse>(otpResult.Error);

        return Result.Success(new RequestOtpResponse(otpResult.Value));
    }
}
