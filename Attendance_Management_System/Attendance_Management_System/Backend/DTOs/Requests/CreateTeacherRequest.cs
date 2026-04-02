using System.ComponentModel.DataAnnotations;

namespace Attendance_Management_System.Backend.DTOs.Requests;

// Request body for admin to create a Teacher profile for an existing User with role='teacher'
public class CreateTeacherRequest
{
    // ID of the existing user account to link with the teacher profile
    [Required]
    public int UserId { get; set; }

    // Unique employee identifier assigned by the institution
    [Required]
    [MaxLength(50)]
    public string EmployeeNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? MiddleName { get; set; }

    // Academic department the teacher belongs to
    [Required]
    [MaxLength(100)]
    public string Department { get; set; } = string.Empty;

    // Optional area of expertise
    [MaxLength(200)]
    public string? Specialization { get; set; }
}