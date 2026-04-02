namespace Attendance_Management_System.Backend.ViewModels.Attendance;

public class AttendanceIndexViewModel
{
    public IReadOnlyList<AttendanceSectionOptionViewModel> Sections { get; set; } = [];
    public IReadOnlyList<AttendanceScheduleOptionViewModel> Schedules { get; set; } = [];
    public IReadOnlyList<AttendanceRecordItemViewModel> Records { get; set; } = [];
    public int? SelectedSectionId { get; set; }
    public int? SelectedScheduleId { get; set; }
    public DateOnly SelectedDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public int TotalStudents { get; set; }
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int AbsentCount { get; set; }
    public string? ErrorMessage { get; set; }
}

public class AttendanceSectionOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class AttendanceScheduleOptionViewModel
{
    public int Id { get; set; }
    public int SectionId { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class AttendanceRecordItemViewModel
{
    public string StudentName { get; set; } = string.Empty;
    public string SubjectName { get; set; } = "-";
    public string TimeIn { get; set; } = "-";
    public string TimeOut { get; set; } = "-";
    public string Remarks { get; set; } = "-";
    public string MarkerName { get; set; } = "-";
}
