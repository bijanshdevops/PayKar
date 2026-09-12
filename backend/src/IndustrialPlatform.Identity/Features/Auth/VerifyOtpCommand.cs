using FluentValidation;
using IndustrialPlatform.Identity.Entities;
using IndustrialPlatform.Identity.Persistence;
using IndustrialPlatform.Identity.Services;
using IndustrialPlatform.Identity.Settings;
using IndustrialPlatform.Shared.Extensions;
using IndustrialPlatform.Shared.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IndustrialPlatform.Identity.Features.Auth;

public sealed record VerifyOtpCommand(string MobileNumber, string OtpCode) : IRequest<Result<VerifyOtpResponse>>;

public sealed class VerifyOtpCommandValidator : AbstractValidator<VerifyOtpCommand>
{
    public VerifyOtpCommandValidator()
    {
        RuleFor(x => x.MobileNumber)
            .NotEmpty().WithMessage("شماره موبایل الزامی است.")
            .Must(m => m.IsValidIranianMobileNumber())
            .WithMessage("شماره موبایل معتبر نیست.");

        RuleFor(x => x.OtpCode)
            .NotEmpty().WithMessage("کد تایید الزامی است.")
            .Length(6).WithMessage("کد تایید باید ۶ رقم باشد.");
    }
}

public sealed class VerifyOtpCommandHandler : IRequestHandler<VerifyOtpCommand, Result<VerifyOtpResponse>>
{
    private readonly IOtpService _otpService;
    private readonly IdentityDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly JwtSettings _jwtSettings;

    public VerifyOtpCommandHandler(
        IOtpService otpService,
        IdentityDbContext dbContext,
        IJwtTokenService jwtTokenService,
        IOptions<JwtSettings> jwtSettings)
    {
        _otpService = otpService;
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<Result<VerifyOtpResponse>> Handle(VerifyOtpCommand request, CancellationToken cancellationToken)
    {
        var mobileNumber = request.MobileNumber.NormalizePersian();

        var verifyResult = await _otpService.VerifyOtpAsync(mobileNumber, request.OtpCode, cancellationToken);
        if (verifyResult.IsFailure)
            return Result.Failure<VerifyOtpResponse>(verifyResult.Error);

        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.MobileNumber == mobileNumber, cancellationToken);

        if (user is null)
        {
            user = User.Create(mobileNumber);
            var candidateRole = await _dbContext.Roles.FirstAsync(r => r.Name == Role.Candidate, cancellationToken);
            user.AssignRole(candidateRole.Id);
            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        if (!user.IsActive)
            return Result.Failure<VerifyOtpResponse>(Error.Forbidden("USER_DEACTIVATED", "این حساب کاربری غیرفعال شده است."));

        // طبق ADR-006: تایید موفق OTP، مالکیت شماره موبایل را اثبات می‌کند — چه برای مسیر سریع OTP
        // و چه برای تکمیل تایید موبایل پس از ثبت‌نام کامل (RegisterCommand). عملیات Idempotent است.
        if (!user.IsMobileVerified)
        {
            user.MarkMobileVerified();
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
        var roleNames = await _dbContext.Roles
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => r.Name)
            .ToListAsync(cancellationToken);

        var accessToken = _jwtTokenService.GenerateAccessToken(user, roleNames);
        var rawRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var refreshTokenHash = _jwtTokenService.HashToken(rawRefreshToken);

        var refreshToken = RefreshToken.Create(
            user.Id,
            refreshTokenHash,
            DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays));

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new VerifyOtpResponse(
            accessToken,
            rawRefreshToken,
            _jwtSettings.AccessTokenExpirationMinutes * 60,
            new AuthenticatedUserDto(user.Id, user.MobileNumber, roleNames));

        return Result.Success(response);
    }
}
