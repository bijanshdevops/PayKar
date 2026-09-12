using IndustrialPlatform.Shared.Results;

namespace IndustrialPlatform.Identity.Services;

public interface IOtpService
{
    Task<Result<int>> RequestOtpAsync(string mobileNumber, CancellationToken cancellationToken = default);
    Task<Result> VerifyOtpAsync(string mobileNumber, string otpCode, CancellationToken cancellationToken = default);
}
