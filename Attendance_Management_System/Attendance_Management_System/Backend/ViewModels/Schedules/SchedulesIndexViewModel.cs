namespace Attendance_Management_System.Backend.ViewModels.Schedules;

public class SchedulesIndexViewModel
{
    public IReadOnlyList<ScheduleListItemViewModel> Schedules { get; set; } = [];
    public string? ErrorMessage { get; set; }
}

public class ScheduleListItemViewModel
{
    public int Id { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string ClassroomName { get; set; } = string.Empty;
    public string DayName { get; set; } = string.Empty;
    public string TimeRange { get; set; } = string.Empty;
    public bool IsMine { get; set; }
}