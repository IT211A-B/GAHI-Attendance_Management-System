using Microsoft.AspNetCore.Identity;

namespace Attendance_Management_System.Backend.Entities;

public class User : IdentityUser<int>
{
    public string Role { get; set; } = "student";
    public bool IsActive { get; set; } = true;
}