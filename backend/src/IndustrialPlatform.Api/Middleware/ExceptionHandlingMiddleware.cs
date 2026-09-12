using System.Net;
using IndustrialPlatform.Shared.Api;

namespace IndustrialPlatform.Api.Middleware;

/// <summary>
/// Middleware مدیریت خطای سراسری — طبق سند 01-Architecture بخش 2.7.
/// این Middleware فقط برای خطاهای پیش‌بینی‌نشده سیستمی (Exception واقعی) است؛
/// خطاهای بیزینسی/ولیدیشن هرگز نباید به این‌جا برسند چون از طریق Result Pattern هندل می‌شوند.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "خطای پیش‌بینی‌نشده سیستمی رخ داد.");

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

            var response = ApiResponse<object>.Fail(
                "خطای پیش‌بینی‌نشده سیستمی رخ داد. لطفاً بعداً دوباره تلاش کنید.",
                (int)HttpStatusCode.InternalServerError,
                new Dictionary<string, string[]> { ["code"] = new[] { "INTERNAL_ERROR" } });

            await context.Response.WriteAsJsonAsync(response);
        }
    }
}
