using System.ComponentModel.DataAnnotations;

namespace Attendance_Management_System.Backend.DTOs.Requests;

public class RegisterRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required")]
    [Compare("Password", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Student number is required")]
    public string StudentNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "First name is required")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Last name is required")]
    public string LastName { get; set; } = string.Empty;

    public string? MiddleName { get; set; }

    [Required(ErrorMessage = "Birthdate is required")]
    public DateOnly Birthdate { get; set; }

    [Required(ErrorMessage = "Gender is required")]
    [RegularExpression("^(M|F|Other)$", ErrorMessage = "Gender must be 'M', 'F', or 'Other'")]
    public string Gender { get; set; } = string.Empty;

    [Required(ErrorMessage = "Address is required")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "Guardian name is required")]
    public string GuardianName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Guardian contact is required")]
    public string GuardianContact { get; set; } = string.Empty;

    [Required(ErrorMessage = "Course ID is required")]
    public int CourseId { get; set; }

    // Optional for compatibility. If omitted, enrollment flow auto-assigns a section.
    public int? SectionId { get; set; }

    [Required(ErrorMessage = "Academic year ID is required")]
    public int AcademicYearId { get; set; }
}