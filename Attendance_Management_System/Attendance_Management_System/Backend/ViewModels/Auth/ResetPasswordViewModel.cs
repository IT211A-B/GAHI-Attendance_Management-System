using System.ComponentModel.DataAnnotations;

namespace Attendance_Management_System.Backend.ViewModels.Auth;

public class ResetPasswordViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "Invalid user account")]
    public int UserId { get; set; }

    [Required(ErrorMessage = "Reset token is required")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required")]
    [DataType(DataType.Password)]
    [MinLength(8, ErrorMessage = "New password must be at least 8 characters")]
    [Display(Name = "New password")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Please confirm the new password")]
    [DataType(DataType.Password)]
    [Display(Name = "Confirm new password")]
    [Compare(nameof(NewPassword), ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
