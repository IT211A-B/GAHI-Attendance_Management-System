namespace Attendance_Management_System.Backend.DTOs.Responses;

public class AttendanceSummaryDto
{
    public int TotalStudents { get; set; }
    public int PresentCount { get; set; }
    public int AbsentCount { get; set; }
    public int LateCount { get; set; }
    public List<AttendanceDto> Records { get; set; } = new();
}