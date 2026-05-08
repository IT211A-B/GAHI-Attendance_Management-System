using System.ComponentModel.DataAnnotations;

namespace Attendance_Management_System.Backend.ViewModels.Auth;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
}
