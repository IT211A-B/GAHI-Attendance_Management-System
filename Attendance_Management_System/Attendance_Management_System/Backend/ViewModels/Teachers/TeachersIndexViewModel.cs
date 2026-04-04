namespace Attendance_Management_System.Backend.ViewModels.Teachers;

public class TeachersIndexViewModel
{
    public IReadOnlyList<TeacherListItemViewModel> Teachers { get; set; } = [];
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