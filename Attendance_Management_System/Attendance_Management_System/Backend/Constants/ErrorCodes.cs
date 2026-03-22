namespace Attendance_Management_System.Backend.Constants;

/// <summary>
/// Centralized error code constants to prevent typos and ensure consistency
/// across the application's error handling.
/// </summary>
public static class ErrorCodes
{
    public const string ValidationError = "VALIDATION_ERROR";
    public const string NotFound = "NOT_FOUND";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string BadRequest = "BAD_REQUEST";
    public const string Conflict = "CONFLICT";
    public const string InternalServerError = "INTERNAL_SERVER_ERROR";
}