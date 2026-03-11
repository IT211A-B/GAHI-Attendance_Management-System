using Microsoft.AspNetCore.Identity;

namespace WebApplication1.Models.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
}

public enum UserRole
{
    Admin,
    Teacher
}
