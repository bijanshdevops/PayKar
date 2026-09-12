using IndustrialPlatform.Identity.Persistence;
using IndustrialPlatform.Identity.Services;
using IndustrialPlatform.Shared.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IndustrialPlatform.Identity.Features.Auth;

public sealed record LogoutCommand(string RefreshToken) : IRequest<Result>;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IdentityDbContext _dbContext;
    private readonly IJwtTokenService _jwtTokenService;

    public LogoutCommandHandler(IdentityDbContext dbContext, IJwtTokenService jwtTokenService)
    {
        _dbContext = dbContext;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = _jwtTokenService.HashToken(request.RefreshToken);
        var existingToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, cancellationToken);

        if (existingToken is not null && existingToken.IsActive)
        {
            existingToken.Revoke();
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}
