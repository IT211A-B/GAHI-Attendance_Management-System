namespace Attendance_Management_System.Backend.DTOs.Responses;

// DTO for teacher's schedule list response
// Contains schedule information including section, subject, classroom, and time details
public class TeacherScheduleDto
{
    // Schedule ID
    public int Id { get; set; }

    // Section ID
    public int SectionId { get; set; }

    // Section name (e.g., "Grade 7-A")
    public string SectionName { get; set; } = string.Empty;

    // Subject ID
    public int SubjectId { get; set; }

    // Subject name (e.g., "Mathematics")
    public string SubjectName { get; set; } = string.Empty;

    // Classroom name from Section.Classroom.Name (e.g., "Room 101")
    public string ClassroomName { get; set; } = string.Empty;

    // Day of week: 0=Sunday, 1=Monday, ..., 6=Saturday
    public int DayOfWeek { get; set; }

    // Day name (e.g., "Monday", "Tuesday")
    public string DayName { get; set; } = string.Empty;

    // Start time in "HH:mm" format
    public string StartTime { get; set; } = string.Empty;

    // End time in "HH:mm" format
    public string EndTime { get; set; } = string.Empty;
}