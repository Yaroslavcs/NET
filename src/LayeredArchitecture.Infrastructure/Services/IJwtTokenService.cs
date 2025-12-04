using LayeredArchitecture.Infrastructure.Identity;

namespace LayeredArchitecture.Infrastructure.Services;

public interface IJwtTokenService
{
    Task<string> GenerateAccessTokenAsync(ApplicationUser user, IList<string> roles);
    string GenerateRefreshToken();
    Task<bool> ValidateTokenAsync(string token);
    Task<string?> GetUserIdFromTokenAsync(string token);
    Task<DateTime?> GetTokenExpirationDateAsync(string token);
    Task<bool> IsTokenExpiredAsync(string token);
    Task RevokeRefreshTokenAsync(ApplicationUser user);
    Task<bool> ValidateRefreshTokenAsync(ApplicationUser user, string refreshToken);
}