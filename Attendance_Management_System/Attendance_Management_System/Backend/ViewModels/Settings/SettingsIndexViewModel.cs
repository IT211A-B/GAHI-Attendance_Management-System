using System.ComponentModel.DataAnnotations;

namespace Attendance_Management_System.Backend.ViewModels.Settings;

public class SettingsIndexViewModel
{
    public string Username { get; set; } = "-";
    public string Email { get; set; } = "-";
    public string FullName { get; set; } = "-";
    public string Role { get; set; } = "-";
    public UpdateProfileFormViewModel ProfileForm { get; set; } = new();
    public ChangePasswordFormViewModel PasswordForm { get; set; } = new();
}

public class UpdateProfileFormViewModel
{
    [Display(Name = "First name")]
    [MaxLength(100, ErrorMessage = "First name must be 100 characters or less")]
    public string? FirstName { get; set; }

    [Display(Name = "Middle name")]
    [MaxLength(100, ErrorMessage = "Middle name must be 100 characters or less")]
    public string? MiddleName { get; set; }

    [Display(Name = "Last name")]
    [MaxLength(100, ErrorMessage = "Last name must be 100 characters or less")]
    public string? LastName { get; set; }
}

public class ChangePasswordFormViewModel
{
    [Required(ErrorMessage = "Current password is required")]
    [DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required")]
    [MinLength(8, ErrorMessage = "New password must be at least 8 characters")]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm the new password")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm new password")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
