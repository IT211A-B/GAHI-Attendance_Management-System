namespace Attendance_Management_System.Backend.ViewModels.TeacherHistory;

public class TeacherHistoryIndexViewModel
{
    public IReadOnlyList<TeacherScheduleOptionViewModel> Schedules { get; set; } = [];
    public int? SelectedScheduleId { get; set; }
    public DateOnly SelectedDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string? ErrorMessage { get; set; }

    public TeacherScheduleDetailsViewModel? ScheduleDetails { get; set; }
    public IReadOnlyList<TeacherAttendanceRecordViewModel> Records { get; set; } = [];

    public int TotalStudents { get; set; }
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int AbsentCount { get; set; }
    public int UnmarkedCount { get; set; }
}

public class TeacherScheduleOptionViewModel
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class TeacherScheduleDetailsViewModel
{
    public string SubjectName { get; set; } = string.Empty;
    public string Section { get; set; } = string.Empty;
    public string Classroom { get; set; } = string.Empty;
    public string Day { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
}

public class TeacherAttendanceRecordViewModel
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = "-";
    public string TimeInText { get; set; } = "-";
    public string TimeOutText { get; set; } = "-";
    public string Remarks { get; set; } = "-";
}
