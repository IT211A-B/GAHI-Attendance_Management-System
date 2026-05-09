using Microsoft.AspNetCore.Identity;
using Attendance_Management_System.Backend.Enums;
using Attendance_Management_System.Backend.Helpers;

namespace Attendance_Management_System.Backend.Entities;

// Application user that extends ASP.NET Identity with custom properties
// Uses int as the primary key type instead of default string
public class User : IdentityUser<int>
{
    // User's role in the system: "admin", "teacher", or "student"
    public string Role { get; set; } = UserRole.Student.ToStorageValue();

    // Active status allows admins to deactivate accounts without deleting them
    public bool IsActive { get; set; } = true;
}
