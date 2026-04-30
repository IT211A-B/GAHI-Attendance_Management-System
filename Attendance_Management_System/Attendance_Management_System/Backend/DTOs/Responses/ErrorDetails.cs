namespace Attendance_Management_System.Backend.DTOs.Responses;

// Standardized error response format for API error handling
public class ErrorDetails
{
    // Unique error code identifier for programmatic error handling
    public string Code { get; set; } = string.Empty;

    // Human-readable error message for display purposes
    public string Message { get; set; } = string.Empty;

    // Optional additional details for structured error information (e.g., conflict details)
    public object? Details { get; set; }
}
