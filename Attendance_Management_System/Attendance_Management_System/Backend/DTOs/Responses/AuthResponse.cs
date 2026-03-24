namespace Attendance_Management_System.Backend.DTOs.Responses;

// Response DTO returned after authentication operations (login, register)
public class AuthResponse
{
    // Indicates whether the authentication operation succeeded
    public bool Success { get; set; }
    // Describes the result or error message
    public string? Message { get; set; }
    // Contains user details if authentication was successful
    public UserDto? User { get; set; }
}
