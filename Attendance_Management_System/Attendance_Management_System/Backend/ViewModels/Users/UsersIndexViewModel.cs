using System.ComponentModel.DataAnnotations;

namespace Attendance_Management_System.Backend.ViewModels.Users;

public class UsersIndexViewModel
{
    public IReadOnlyList<UserListItemViewModel> Users { get; set; } = [];
    public IReadOnlyList<string> AvailableRoles { get; set; } = ["admin", "teacher", "student"];
    public CreateUserFormViewModel CreateForm { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class UserListItemViewModel
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreateUserFormViewModel
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    [DataType(DataType.Password)]
    [Display(Name = "Temporary password")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required")]
    [Display(Name = "Role")]
    public string Role { get; set; } = "student";
}