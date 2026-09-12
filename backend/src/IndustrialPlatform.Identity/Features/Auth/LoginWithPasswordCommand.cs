using FluentValidation;
using IndustrialPlatform.Identity.Entities;
using IndustrialPlatform.Identity.Persistence;
using IndustrialPlatform.Identity.Services;
using IndustrialPlatform.Identity.Settings;
using IndustrialPlatform.Shared.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IndustrialPlatform.Identity.Features.Auth;

/// <summary>ورود با نام‌کاربری/رمزعبور — روش دوم و اختیاری در کنار OTP، طبق ADR-005.</summary>
public sealed record LoginWithPasswordCommand(string Username, string Password) : IRequest<Result<LoginWithPasswordResponse>>;

public sealed class LoginWithPasswordCommandValidator : AbstractValidator<LoginWithPasswordCommand>
{
    public LoginWithPasswordCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("نام‌کاربری الزامی است.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("رمز عبور الزامی است.");
    }
}

public sealed class LoginWithPasswordCommandHandler : IRequestHandler<LoginWithPasswordCommand, Result<LoginWithPasswordResponse>>
{
    private readonly IdentityDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly JwtSettings _jwtSettings;

    public LoginWithPasswordCommandHandler(
        IdentityDbContext dbContext, IPasswordHasher passwordHasher, IJwtTokenService jwtTokenService, IOptions<JwtSettings> jwtSettings)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<Result<LoginWithPasswordResponse>> Handle(LoginWithPasswordCommand request, CancellationToken cancellationToken)
    {
        var normalizedUsername = request.Username.Trim().ToLowerInvariant();

        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Username == normalizedUsername, cancellationToken);

        // یادداشت امنیتی: پیام یکسان برای «کاربر یافت نشد» و «رمز نادرست» تا از User Enumeration جلوگیری شود.
        if (user is null || !user.HasPassword || !_passwordHasher.Verify(request.Password, user.PasswordHash!))
            return Result.Failure<LoginWithPasswordResponse>(Error.Unauthorized("INVALID_CREDENTIALS", "نام‌کاربری یا رمز عبور نادرست است."));

        if (!user.IsActive)
            return Result.Failure<LoginWithPasswordResponse>(Error.Forbidden("USER_DEACTIVATED", "این حساب کاربری غیرفعال شده است."));

        // طبق ADR-006: کاربرانی که از مسیر ثبت‌نام کامل آمده‌اند، تا تایید شماره موبایل با OTP
        // اجازه ورود با رمز عبور را ندارند (اثبات مالکیت شماره پیش‌نیاز فعال‌سازی کامل حساب است).
        if (!user.IsMobileVerified)
            return Result.Failure<LoginWithPasswordResponse>(Error.Forbidden(
                "MOBILE_NOT_VERIFIED", "ابتدا باید شماره موبایل خود را با کد پیامکی تایید کنید."));

        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
        var roleNames = await _dbContext.Roles
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => r.Name)
            .ToListAsync(cancellationToken);

        var accessToken = _jwtTokenService.GenerateAccessToken(user, roleNames);
        var rawRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var refreshTokenHash = _jwtTokenService.HashToken(rawRefreshToken);

        var refreshToken = RefreshToken.Create(
            user.Id, refreshTokenHash, DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays));

        _dbContext.RefreshTokens.Add(refreshToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new LoginWithPasswordResponse(
            accessToken, rawRefreshToken, _jwtSettings.AccessTokenExpirationMinutes * 60,
            new AuthenticatedUserDto(user.Id, user.MobileNumber, roleNames));

        return Result.Success(response);
    }
}
