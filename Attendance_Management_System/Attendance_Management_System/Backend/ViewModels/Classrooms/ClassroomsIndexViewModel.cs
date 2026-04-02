namespace Attendance_Management_System.Backend.ViewModels.Classrooms;

public class ClassroomsIndexViewModel
{
    public IReadOnlyList<ClassroomListItemViewModel> Classrooms { get; set; } = [];
    public string? ErrorMessage { get; set; }
}

public class ClassroomListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = "-";
}