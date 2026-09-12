using IndustrialPlatform.Identity.Entities;

namespace IndustrialPlatform.Identity.Services;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user, IEnumerable<string> roles);
    string GenerateRefreshToken();
    string HashToken(string rawToken);
}
