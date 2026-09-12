using IndustrialPlatform.Api.Common;
using IndustrialPlatform.Identity.Features.Auth;
using MediatR;

namespace IndustrialPlatform.Api.Endpoints;

/// <summary>اندپوینت‌های احراز هویت — طبق سند 04-Api-Contract.md بخش ۵.۱ و ۵.۲.</summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth").WithTags("Auth");

        // ثبت‌نام کامل (نام‌کاربری+ایمیل+موبایل+رمزعبور) — طبق ADR-006. پس از ثبت، کد OTP برای
        // تایید موبایل ارسال می‌شود؛ تکمیل با همان اندپوینت otp/verify انجام می‌گیرد.
        group.MapPost("/register", async (RegisterRequestDto dto, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RegisterCommand(dto.Username, dto.Email, dto.MobileNumber, dto.Password), ct);
            return result.ToApiResult("ثبت‌نام با موفقیت انجام شد. کد تایید به شماره موبایل شما ارسال شد.", 201);
        });

        group.MapPost("/otp/request", async (RequestOtpRequestDto dto, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RequestOtpCommand(dto.MobileNumber), ct);
            return result.ToApiResult("کد تایید ارسال شد.");
        });

        group.MapPost("/otp/verify", async (VerifyOtpRequestDto dto, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new VerifyOtpCommand(dto.MobileNumber, dto.OtpCode), ct);
            return result.ToApiResult("ورود موفقیت‌آمیز بود.");
        });

        // ورود با نام‌کاربری/رمزعبور — روش دوم و اختیاری در کنار OTP، طبق ADR-005.
        group.MapPost("/login", async (LoginRequestDto dto, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new LoginWithPasswordCommand(dto.Username, dto.Password), ct);
            return result.ToApiResult("ورود موفقیت‌آمیز بود.");
        });

        // تنظیم نام‌کاربری/رمزعبور برای کاربر از‌قبل‌احرازشده (نیازمند توکن معتبر OTP).
        group.MapPost("/set-password", async (SetPasswordRequestDto dto, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new SetPasswordCommand(dto.Username, dto.Password), ct);
            return result.ToApiResult("نام‌کاربری و رمز عبور با موفقیت تنظیم شد.");
        }).RequireAuthorization();

        // بازیابی رمز عبور فراموش‌شده — مرحله اول همان اندپوینت otp/request است؛ این مرحله دوم است.
        group.MapPost("/password/reset", async (ResetPasswordRequestDto dto, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new ResetPasswordCommand(dto.MobileNumber, dto.OtpCode, dto.NewPassword), ct);
            return result.ToApiResult("رمز عبور شما با موفقیت بازنشانی شد.");
        });

        group.MapPost("/refresh-token", async (RefreshTokenRequestDto dto, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new RefreshTokenCommand(dto.RefreshToken), ct);
            return result.ToApiResult();
        });

        group.MapPost("/logout", async (RefreshTokenRequestDto dto, ISender sender, CancellationToken ct) =>
        {
            var result = await sender.Send(new LogoutCommand(dto.RefreshToken), ct);
            return result.ToApiResult("خروج با موفقیت انجام شد.");
        });

        return app;
    }

    private sealed record RequestOtpRequestDto(string MobileNumber);
    private sealed record VerifyOtpRequestDto(string MobileNumber, string OtpCode);
    private sealed record RefreshTokenRequestDto(string RefreshToken);
    private sealed record LoginRequestDto(string Username, string Password);
    private sealed record SetPasswordRequestDto(string Username, string Password);
    private sealed record RegisterRequestDto(string Username, string Email, string MobileNumber, string Password);
    private sealed record ResetPasswordRequestDto(string MobileNumber, string OtpCode, string NewPassword);
}
