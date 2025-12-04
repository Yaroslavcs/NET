namespace LayeredArchitecture.Infrastructure.DTOs;

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiry { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public IList<string> Roles { get; set; } = new List<string>();
    public bool EmailVerified { get; set; }
}