namespace Donbosco_Attendance_Management_System.DTOs.Responses;

// standard json envelope for all api responses
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public ApiError? Error { get; set; }

    public static ApiResponse<T> SuccessResponse(T data)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Data = data,
            Error = null
        };
    }

    public static ApiResponse<T> FailureResponse(string code, string message)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Data = default,
            Error = new ApiError
            {
                Code = code,
                Message = message
            }
        };
    }

    public static ApiResponse<T> FailureResponse(string code, string message, object? details)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Data = default,
            Error = new ApiError
            {
                Code = code,
                Message = message,
                Details = details
            }
        };
    }
}

public class ApiError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public object? Details { get; set; }
}

// non-generic version for responses without data
public class ApiResponse : ApiResponse<object>
{
    public static ApiResponse SuccessResponse()
    {
        return new ApiResponse
        {
            Success = true,
            Data = null,
            Error = null
        };
    }

    public new static ApiResponse FailureResponse(string code, string message)
    {
        return new ApiResponse
        {
            Success = false,
            Data = null,
            Error = new ApiError
            {
                Code = code,
                Message = message
            }
        };
    }
}

// error codes used throughout the api
public static class ErrorCodes
{
    public const string UNAUTHORIZED = "UNAUTHORIZED";
    public const string FORBIDDEN = "FORBIDDEN";
    public const string NOT_FOUND = "NOT_FOUND";
    public const string VALIDATION_ERROR = "VALIDATION_ERROR";
    public const string CONFLICT_SECTION_SLOT = "CONFLICT_SECTION_SLOT";
    public const string CONFLICT_CLASSROOM = "CONFLICT_CLASSROOM";
    public const string CONFLICT_TEACHER = "CONFLICT_TEACHER";
    public const string INVALID_CREDENTIALS = "INVALID_CREDENTIALS";
    public const string USER_INACTIVE = "USER_INACTIVE";
    public const string TOKEN_EXPIRED = "TOKEN_EXPIRED";
    public const string TOKEN_INVALID = "TOKEN_INVALID";
}