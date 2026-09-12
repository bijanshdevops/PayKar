using IndustrialPlatform.Identity.Entities;
using IndustrialPlatform.Identity.Persistence;
using IndustrialPlatform.Shared.Results;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IndustrialPlatform.Identity.Features.Roles;

/// <summary>
/// طبق ADR-010: وقتی کاربری (با هر نقش اولیه‌ای که دارد — معمولاً Candidate) اولین پروفایل
/// Company خودش را با موفقیت می‌سازد، نقش CompanyManager به‌صورت خودکار به او افزوده می‌شود.
/// Idempotent است — اگر کاربر از قبل این نقش را داشته باشد، تغییری اعمال نمی‌شود.
/// این Command از لایه Api (نه از Application، طبق قانون استقلال Application از Identity)
/// بلافاصله پس از موفقیت CreateCompanyCommand فراخوانی می‌شود.
/// </summary>
public sealed record EnsureCompanyManagerRoleCommand(Guid UserId) : IRequest<Result>;

public sealed class EnsureCompanyManagerRoleCommandHandler : IRequestHandler<EnsureCompanyManagerRoleCommand, Result>
{
    private readonly IdentityDbContext _dbContext;

    public EnsureCompanyManagerRoleCommandHandler(IdentityDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result> Handle(EnsureCompanyManagerRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
            return Result.Failure(Error.NotFound("USER_NOT_FOUND", "کاربر یافت نشد."));

        var companyManagerRole = await _dbContext.Roles
            .FirstAsync(r => r.Name == Role.CompanyManager, cancellationToken);

        user.AssignRole(companyManagerRole.Id);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
