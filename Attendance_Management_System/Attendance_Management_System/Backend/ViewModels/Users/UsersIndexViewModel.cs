namespace Attendance_Management_System.Backend.ViewModels.Users;

public class UsersIndexViewModel
{
    public IReadOnlyList<UserListItemViewModel> Users { get; set; } = [];
    public string? ErrorMessage { get; set; }
}

public class UserListItemViewModel
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}