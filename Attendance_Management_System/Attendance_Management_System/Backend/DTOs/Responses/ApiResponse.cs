namespace Attendance_Management_System.Backend.DTOs.Responses;

// Generic wrapper for all API responses, providing consistent success/error structure
public class ApiResponse<T>
{
    // Indicates whether the request was successful
    public bool Success { get; set; }

    // The response data (populated on success)
    public T? Data { get; set; }

    // Error details (populated on failure)
    public ErrorDetails? Error { get; set; }

    // Factory method for creating successful responses
    public static ApiResponse<T> SuccessResponse(T data)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Error = null
        };
    }

    // Factory method for creating error responses
    public static ApiResponse<T> ErrorResponse(string code, string message)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Data = default,
            Error = new ErrorDetails { Code = code, Message = message }
        };
    }

    // Factory method for creating error responses with additional details
    public static ApiResponse<T> ErrorResponse(string code, string message, object? details)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Data = default,
            Error = new ErrorDetails { Code = code, Message = message, Details = details }
        };
    }
}
