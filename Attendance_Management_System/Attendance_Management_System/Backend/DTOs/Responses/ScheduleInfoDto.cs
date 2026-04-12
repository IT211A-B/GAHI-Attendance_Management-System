namespace Attendance_Management_System.Backend.DTOs.Responses;

// DTO for schedule information in history response
// Contains basic schedule details for display in attendance history
public class ScheduleInfoDto
{
    // Subject name (e.g., "Mathematics")
    public string SubjectName { get; set; } = string.Empty;

    // Section name (e.g., "Grade 7-A")
    public string Section { get; set; } = string.Empty;

    // Classroom name (e.g., "Room 101")
    public string Classroom { get; set; } = string.Empty;

    // Day name (e.g., "Monday")
    public string Day { get; set; } = string.Empty;

    // Start time in "HH:mm" format
    public string StartTime { get; set; } = string.Empty;

    // End time in "HH:mm" format
    public string EndTime { get; set; } = string.Empty;
}