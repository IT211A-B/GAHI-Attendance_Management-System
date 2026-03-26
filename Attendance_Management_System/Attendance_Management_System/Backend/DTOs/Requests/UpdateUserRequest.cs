using System.ComponentModel.DataAnnotations;

namespace Attendance_Management_System.Backend.DTOs.Requests;

public class UpdateUserRequest
{
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string? Email { get; set; }

    public string? Role { get; set; }

    public bool? IsActive { get; set; }
}