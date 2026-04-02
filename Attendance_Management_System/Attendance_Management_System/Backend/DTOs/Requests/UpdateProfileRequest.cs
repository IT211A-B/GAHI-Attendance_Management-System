using System.ComponentModel.DataAnnotations;

namespace Attendance_Management_System.Backend.DTOs.Requests;

public class UpdateProfileRequest
{
    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? MiddleName { get; set; }

    public string? CurrentPassword { get; set; }

    [MinLength(8, ErrorMessage = "New password must be at least 8 characters")]
    public string? NewPassword { get; set; }
}