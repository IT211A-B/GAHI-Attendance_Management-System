namespace Attendance_Management_System.Backend.Enums;

// Represents the possible attendance statuses for a student in a class session.
public enum AttendanceStatusKind
{
    // No attendance mark has been recorded yet.
    Unmarked = 0,
    // Student arrived on time or within the grace period.
    Present = 1,
    // Student arrived after the grace period ended.
    Late = 2,
    // Student did not attend or no time-in was recorded.
    Absent = 3
}
