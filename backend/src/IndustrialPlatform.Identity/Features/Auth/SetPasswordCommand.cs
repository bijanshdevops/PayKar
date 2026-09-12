using FluentValidation;
using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Identity.Persistence;
using IndustrialPlatform.Identity.Services;
using IndustrialPlatform.Shared.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IndustrialPlatform.Identity.Features.Auth;

/// <summary>
/// تنظیم نام‌کاربری/رمزعبور برای کاربر از‌قبل‌احرازشده (ADR-005) — طبق تصمیم، ثبت‌نام همچنان
/// فقط با OTP انجام می‌شود؛ این عملیات صرفاً یک روش ورود دوم و اختیاری برای کاربر موجود اضافه می‌کند.
/// </summary>
public sealed record SetPasswordCommand(string Username, string Password) : IRequest<Result<SetPasswordResponse>>;

public sealed class SetPasswordCommandValidator : AbstractValidator<SetPasswordCommand>
{
    public SetPasswordCommandValidator()
    {
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("نام‌کاربری الزامی است.")
            .Matches("^[a-zA-Z0-9_]{4,30}$").WithMessage("نام‌کاربری باید بین ۴ تا ۳۰ کاراکتر و شامل حروف انگلیسی، عدد و _ باشد.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("رمز عبور الزامی است.")
            .MinimumLength(8).WithMessage("رمز عبور باید حداقل ۸ کاراکتر باشد.");
    }
}

public sealed class SetPasswordCommandHandler : IRequestHandler<SetPasswordCommand, Result<SetPasswordResponse>>
{
    private readonly IdentityDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ICurrentUserService _currentUser;

    public SetPasswordCommandHandler(IdentityDbContext dbContext, IPasswordHasher passwordHasher, ICurrentUserService currentUser)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _currentUser = currentUser;
    }

    public async Task<Result<SetPasswordResponse>> Handle(SetPasswordCommand request, CancellationToken cancellationToken)
    {
        if (_currentUser.UserId is null)
            return Result.Failure<SetPasswordResponse>(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        var normalizedUsername = request.Username.Trim().ToLowerInvariant();

        var usernameTaken = await _dbContext.Users.AnyAsync(
            u => u.Username == normalizedUsername && u.Id != _currentUser.UserId.Value, cancellationToken);

        if (usernameTaken)
            return Result.Failure<SetPasswordResponse>(Error.Conflict("USERNAME_TAKEN", "این نام‌کاربری قبلاً استفاده شده است."));

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == _currentUser.UserId.Value, cancellationToken);
        if (user is null)
            return Result.Failure<SetPasswordResponse>(Error.NotFound("USER_NOT_FOUND", "کاربر یافت نشد."));

        var passwordHash = _passwordHasher.Hash(request.Password);
        user.SetCredentials(normalizedUsername, passwordHash);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(new SetPasswordResponse(normalizedUsername));
    }
}
