using IndustrialPlatform.Application.Common.Interfaces;
using IndustrialPlatform.Domain.Support;
using IndustrialPlatform.Shared.Results;

namespace IndustrialPlatform.Application.Support;

/// <summary>
/// بررسی دسترسی به یک تیکت: صاحب تیکت یا عضو تیم پشتیبانی (Admin) — طبق ADR-007.
/// نام نقش «Admin» به‌صورت رشته خام مقایسه می‌شود چون طبق ماتریس وابستگی سند 01-Architecture،
/// Application اجازه ارجاع به IndustrialPlatform.Identity را ندارد (مشابه JobAdOwnershipGuard.cs).
/// </summary>
internal static class SupportTicketOwnershipGuard
{
    private const string AdminRoleName = "Admin";

    public static Result EnsureAccess(SupportTicket ticket, ICurrentUserService currentUser)
    {
        if (currentUser.IsInRole(AdminRoleName))
            return Result.Success();

        if (currentUser.UserId is null)
            return Result.Failure(Error.Unauthorized("UNAUTHORIZED", "ابتدا وارد شوید."));

        if (ticket.UserId != currentUser.UserId.Value)
            return Result.Failure(Error.Forbidden("FORBIDDEN", "شما اجازه دسترسی به این تیکت را ندارید."));

        return Result.Success();
    }
}
