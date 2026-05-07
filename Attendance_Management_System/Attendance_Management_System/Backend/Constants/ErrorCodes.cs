namespace Attendance_Management_System.Backend.Constants;

// Error codes that are surfaced in schedule conflict responses.
public static class ErrorCodes
{
    // Generic schedule conflict fallback when a specific conflict kind is unavailable.
    public const string Conflict = "CONFLICT";

    // Schedule conflict: Section already has a schedule at this time
    public const string ConflictSectionSlot = "CONFLICT_SECTION_SLOT";

    // Schedule conflict: Classroom is already booked at this time
    public const string ConflictClassroom = "CONFLICT_CLASSROOM";

    // Schedule conflict: Teacher has overlapping schedule in another section
    public const string ConflictTeacher = "CONFLICT_TEACHER";
}
