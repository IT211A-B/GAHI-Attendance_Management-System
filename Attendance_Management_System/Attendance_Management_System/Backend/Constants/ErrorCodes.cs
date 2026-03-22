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
}
