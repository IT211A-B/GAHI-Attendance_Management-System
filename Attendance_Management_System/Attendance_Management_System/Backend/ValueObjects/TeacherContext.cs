namespace Attendance_Management_System.Backend.ValueObjects;

// Contains both User ID and Teacher ID for proper authorization and attendance marking.
// UserId: The User table primary key, used for MarkedBy field (FK to User table)
// TeacherId: The Teacher table primary key, used for section assignment validation (SectionTeachers.TeacherId)
public readonly record struct TeacherContext
{
    // The User.Id - used for MarkedBy field in Attendance (FK to User table)
    public int UserId { get; init; }

    // The Teacher.Id - used for section assignment validation (SectionTeachers.TeacherId)
    // Null for admin users who don't have a Teacher record
    public int? TeacherId { get; init; }

    // Indicates if the current user is an admin
    public bool IsAdmin { get; init; }

    // Returns true if the user can mark attendance (either admin or has TeacherId)
    public bool CanMarkAttendance => IsAdmin || TeacherId.HasValue;

    // Gets the TeacherId to use for section validation
    // Returns null for admins (should bypass section assignment check)
    public int? GetSectionValidationId() => TeacherId;

    // Gets the UserId to use for MarkedBy field
    public int GetMarkerId() => UserId;
}
