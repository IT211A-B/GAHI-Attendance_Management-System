namespace Attendance_Management_System.Backend.ViewModels.Sections;

public class SectionAttendanceIndexViewModel
{
    public IReadOnlyList<SectionOptionViewModel> SectionOptions { get; set; } = [];
    public IReadOnlyList<SectionAttendanceScheduleOptionViewModel> AttendanceSchedules { get; set; } = [];
    public IReadOnlyList<SectionAttendanceStudentRowViewModel> AttendanceStudents { get; set; } = [];
    public int? SelectedSectionId { get; set; }
    public int? SelectedAttendanceScheduleId { get; set; }
    public DateOnly SelectedAttendanceDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public bool IsAdmin { get; set; }
    public bool IsTeacher { get; set; }
    public int AttendanceTotalStudents { get; set; }
    public int AttendancePresentCount { get; set; }
    public int AttendanceLateCount { get; set; }
    public int AttendanceAbsentCount { get; set; }
    public int AttendanceUnmarkedCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string? AttendanceErrorMessage { get; set; }
}
