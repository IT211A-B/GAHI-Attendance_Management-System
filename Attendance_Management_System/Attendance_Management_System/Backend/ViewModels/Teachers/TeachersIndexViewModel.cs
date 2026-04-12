using System.ComponentModel.DataAnnotations;

namespace Attendance_Management_System.Backend.ViewModels.Teachers;

public class TeachersIndexViewModel
{
    public IReadOnlyList<TeacherListItemViewModel> Teachers { get; set; } = [];
    public CreateTeacherAccountFormViewModel CreateForm { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class TeacherListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string SectionsText { get; set; } = "-";
}

public class CreateTeacherAccountFormViewModel
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
    [Display(Name = "Confirm password")]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "First name is required")]
    [Display(Name = "First name")]
    public string FirstName { get; set; } = string.Empty;

    [Display(Name = "Middle name")]
    public string? MiddleName { get; set; }

    [Required(ErrorMessage = "Last name is required")]
    [Display(Name = "Last name")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Department is required")]
    [Display(Name = "Department")]
    public string Department { get; set; } = string.Empty;

    [Display(Name = "Specialization")]
    public string? Specialization { get; set; }
}