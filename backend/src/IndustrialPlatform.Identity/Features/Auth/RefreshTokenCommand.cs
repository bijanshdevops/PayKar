using FluentValidation;
using IndustrialPlatform.Identity.Persistence;
using IndustrialPlatform.Identity.Services;
using IndustrialPlatform.Identity.Settings;
using IndustrialPlatform.Shared.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace IndustrialPlatform.Identity.Features.Auth;

/// <summary>
/// چرخش Refresh Token طبق سند 05-Security-Rules.md بخش ۲: هر استفاده، توکن قبلی را باطل
/// و توکن جدید صادر می‌کند (Rotation). استفاده مجدد از توکن باطل‌شده مشکوک تلقی می‌شود.
/// </summary>
public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<Result<RefreshTokenResponse>>;

public sealed class RefreshTokenCommandValidator : AbstractValidator<RefreshTokenCommand>
{
    public RefreshTokenCommandValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().WithMessage("Refresh Token الزامی است.");
    }
}

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<RefreshTokenResponse>>
{
    private readonly IdentityDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly JwtSettings _jwtSettings;

    public RefreshTokenCommandHandler(IdentityDbContext dbContext, IJwtTokenService jwtTokenService, IOptions<JwtSettings> jwtSettings)
    {
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
        _jwtSettings = jwtSettings.Value;
    }

    public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _jwtTokenService.HashToken(request.RefreshToken);

        var existingToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (existingToken is null || !existingToken.IsActive)
        {
            // یادداشت امنیتی: استفاده از توکن باطل‌شده/ناموجود = تشخیص احتمالی سرقت توکن (Reuse Detection).
            return Result.Failure<RefreshTokenResponse>(Error.Unauthorized("UNAUTHORIZED", "Refresh Token نامعتبر یا منقضی شده است."));
        }

        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == existingToken.UserId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return Result.Failure<RefreshTokenResponse>(Error.Unauthorized("UNAUTHORIZED", "کاربر مرتبط با این نشست یافت نشد یا غیرفعال است."));
        }

        var roleIds = user.UserRoles.Select(ur => ur.RoleId).ToList();
        var roleNames = await _dbContext.Roles
            .Where(r => roleIds.Contains(r.Id))
            .Select(r => r.Name)
            .ToListAsync(cancellationToken);

        var newAccessToken = _jwtTokenService.GenerateAccessToken(user, roleNames);
        var newRawRefreshToken = _jwtTokenService.GenerateRefreshToken();
        var newRefreshTokenHash = _jwtTokenService.HashToken(newRawRefreshToken);

        existingToken.Revoke(newRefreshTokenHash);

        var newRefreshTokenEntity = Entities.RefreshToken.Create(
            user.Id,
            newRefreshTokenHash,
            DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenExpirationDays));

        _dbContext.RefreshTokens.Add(newRefreshTokenEntity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(new RefreshTokenResponse(
            newAccessToken,
            newRawRefreshToken,
            _jwtSettings.AccessTokenExpirationMinutes * 60,
            new AuthenticatedUserDto(user.Id, user.MobileNumber, roleNames)));
    }
}
