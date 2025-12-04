using Microsoft.AspNetCore.Identity;

namespace LayeredArchitecture.Infrastructure.Identity;

public class ApplicationRole : IdentityRole
{
    public string? Description { get; set; }
    public DateTime DateCreated { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual ICollection<ApplicationUserRole> UserRoles { get; set; } = new List<ApplicationUserRole>();
}