using System.Security.Claims;
using IndustrialPlatform.Application.Common.Interfaces;

namespace IndustrialPlatform.Api.Services;

/// <summary>
/// پیاده‌سازی ICurrentUserService با خواندن Claims توکن JWT — طبق سند 01-Architecture،
/// این پیاده‌سازی فقط در لایه Api مجاز است چون به HttpContext وابسته است.
/// </summary>
public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var value = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(value, out var id) ? id : null;
        }
    }

    public string? MobileNumber => _httpContextAccessor.HttpContext?.User.FindFirstValue("mobile_number");

    public IReadOnlyCollection<string> Roles =>
        _httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList()
        ?? new List<string>();

    public bool IsInRole(string role) =>
        _httpContextAccessor.HttpContext?.User.IsInRole(role) ?? false;
}
