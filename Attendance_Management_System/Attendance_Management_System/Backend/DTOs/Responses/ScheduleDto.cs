namespace Attendance_Management_System.Backend.DTOs.Responses;

// Response DTO for schedule data
public class ScheduleDto
{
    // Unique identifier for the schedule
    public int Id { get; set; }

    // ID of the section this schedule belongs to
    public int SectionId { get; set; }

    // Name of the section (e.g., "Grade 7-A")
    public string SectionName { get; set; } = string.Empty;

    // ID of the subject being taught
    public int SubjectId { get; set; }

    // Name of the subject (e.g., "Mathematics")
    public string SubjectName { get; set; } = string.Empty;

    // ID of the classroom where the class is held
    public int ClassroomId { get; set; }

    // Name of the classroom (e.g., "Room 101")
    public string ClassroomName { get; set; } = string.Empty;

    // Day of week: 0=Sunday, 1=Monday, ..., 6=Saturday
    public int DayOfWeek { get; set; }

    // Human-readable day name (e.g., "Monday")
    public string DayName { get; set; } = string.Empty;

    // Start time in HH:mm format (e.g., "08:00")
    public string StartTime { get; set; } = string.Empty;

    // End time in HH:mm format (e.g., "10:00")
    public string EndTime { get; set; } = string.Empty;

    // Date when this schedule becomes effective
    public DateOnly EffectiveFrom { get; set; }

    // Optional end date for schedule changes (null if currently active)
    public DateOnly? EffectiveTo { get; set; }

    // When this schedule record was created
    public DateTimeOffset CreatedAt { get; set; }

    // True if the current teacher is assigned to this section
    public bool IsMine { get; set; }

    // List of teachers assigned to this section
    public List<TeacherInfo> Teachers { get; set; } = [];
}

// Information about a teacher assigned to a section
public class TeacherInfo
{
    // Unique identifier for the teacher
    public int Id { get; set; }

    // Teacher's first name
    public string FirstName { get; set; } = string.Empty;

    // Teacher's last name
    public string LastName { get; set; } = string.Empty;

    // Teacher's department
    public string Department { get; set; } = string.Empty;
}