using System.ComponentModel.DataAnnotations;

namespace Attendance_Management_System.Backend.ViewModels.Auth;

public class StudentSignupViewModel
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Confirm password is required")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    [Display(Name = "Confirm password")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Student number is required")]
    [Display(Name = "Student number")]
    public string StudentNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "First name is required")]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Display(Name = "Middle name")]
    public string? MiddleName { get; set; }

    [Required(ErrorMessage = "Last name is required")]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Birthdate is required")]
    [DataType(DataType.Date)]
    [Display(Name = "Birthdate")]
    public DateOnly Birthdate { get; set; } = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddYears(-16));

    [Required(ErrorMessage = "Gender is required")]
    [RegularExpression("^(M|F|Other)$", ErrorMessage = "Gender must be 'M', 'F', or 'Other'")]
    [Display(Name = "Gender")]
    public string Gender { get; set; } = string.Empty;

    [Required(ErrorMessage = "Address is required")]
    [Display(Name = "Address")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "Guardian name is required")]
    [Display(Name = "Guardian name")]
    public string GuardianName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Guardian contact is required")]
    [Display(Name = "Guardian contact")]
    public string GuardianContact { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid course")]
    [Display(Name = "Course")]
    public int CourseId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid academic period")]
    [Display(Name = "Academic period")]
    public int AcademicYearId { get; set; }

    public IReadOnlyList<SignupCourseOptionViewModel> AvailableCourses { get; set; } = [];
    public IReadOnlyList<SignupAcademicYearOptionViewModel> AvailableAcademicYears { get; set; } = [];

    public string? ErrorMessage { get; set; }
}

public class SignupCourseOptionViewModel
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class SignupAcademicYearOptionViewModel
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
}
