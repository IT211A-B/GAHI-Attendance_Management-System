using System.ComponentModel.DataAnnotations;

namespace Donbosco_Attendance_Management_System.DTOs.Requests;

// request model for user updating their own profile
public class UpdateProfileRequest
{
    [MaxLength(255, ErrorMessage = "Name cannot exceed 255 characters")]
    public string? Name { get; set; }

    [MinLength(6, ErrorMessage = "Password must be at least 6 characters")]
    [MaxLength(100, ErrorMessage = "Password cannot exceed 100 characters")]
    public string? Password { get; set; }
}