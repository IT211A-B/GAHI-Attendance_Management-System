namespace Attendance_Management_System.Backend.Constants;

// Centralized error code constants for consistent error handling across the application
public static class ErrorCodes
{
    // Request data failed validation rules
    public const string ValidationError = "VALIDATION_ERROR";

    // Requested resource was not found in the database
    public const string NotFound = "NOT_FOUND";

    // User is not authenticated (no valid token)
    public const string Unauthorized = "UNAUTHORIZED";

    // User is authenticated but lacks required permissions
    public const string Forbidden = "FORBIDDEN";

    // Generic bad request error
    public const string BadRequest = "BAD_REQUEST";

    // Resource conflict (e.g., duplicate entry)
    public const string Conflict = "CONFLICT";

    // Unexpected server-side error
    public const string InternalServerError = "INTERNAL_SERVER_ERROR";

    // Resource already exists in the system
    public const string AlreadyExists = "ALREADY_EXISTS";

    // Attempted to create a duplicate assignment relationship
    public const string DuplicateAssignment = "DUPLICATE_ASSIGNMENT";

    // Schedule conflict: Section already has a schedule at this time
    public const string ConflictSectionSlot = "CONFLICT_SECTION_SLOT";

    // Schedule conflict: Classroom is already booked at this time
    public const string ConflictClassroom = "CONFLICT_CLASSROOM";

    // Schedule conflict: Teacher has overlapping schedule in another section
    public const string ConflictTeacher = "CONFLICT_TEACHER";

    // Teacher cannot be removed from section because section has existing schedules
    public const string TeacherHasSchedules = "TEACHER_HAS_SCHEDULES";

    // Resource is in use and cannot be deleted
    public const string InUse = "IN_USE";

    // No available sections for enrollment
    public const string NoAvailableSections = "NO_AVAILABLE_SECTIONS";

    // Student already has an enrollment for this course and academic year
    public const string EnrollmentExists = "ENROLLMENT_EXISTS";

    // Section has reached over-capacity limit
    public const string SectionOverCapacity = "SECTION_OVER_CAPACITY";

    // Invalid capacity settings configuration
    public const string InvalidCapacitySettings = "INVALID_CAPACITY_SETTINGS";
}
