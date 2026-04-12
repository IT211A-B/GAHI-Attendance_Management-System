namespace Attendance_Management_System.Backend.DTOs.Responses;

// Response DTO for detailed conflict information
public class ConflictDetailDto
{
    // Type of conflict: CONFLICT_SECTION_SLOT, CONFLICT_CLASSROOM, or CONFLICT_TEACHER
    public string ConflictType { get; set; } = string.Empty;

    // Human-readable conflict description
    public string Message { get; set; } = string.Empty;

    // Details about the conflicting slot (null if no specific conflict found)
    public ConflictInfo? Info { get; set; }
}

// Details about a conflicting schedule slot
public class ConflictInfo
{
    // Subject name in the conflicting slot
    public string? SubjectName { get; set; }

    // Teacher who owns the conflicting slot
    public string? TeacherName { get; set; }

    // Section with the conflicting slot
    public string? SectionName { get; set; }

    // Classroom in conflict
    public string? ClassroomName { get; set; }

    // Start time of the conflicting slot (HH:mm format)
    public string StartTime { get; set; } = string.Empty;

    // End time of the conflicting slot (HH:mm format)
    public string EndTime { get; set; } = string.Empty;
}