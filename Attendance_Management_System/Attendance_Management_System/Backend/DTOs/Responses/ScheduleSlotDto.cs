namespace Attendance_Management_System.Backend.DTOs.Responses;

// Represents a single schedule slot within a timetable
public class ScheduleSlotDto
{
    public int ScheduleId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public string Classroom { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;  // "HH:mm" format
    public string EndTime { get; set; } = string.Empty;    // "HH:mm" format
    public bool IsMine { get; set; }
}