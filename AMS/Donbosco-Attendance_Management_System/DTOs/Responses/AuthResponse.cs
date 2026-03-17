namespace Donbosco_Attendance_Management_System.DTOs.Responses;

/// <summary>
/// Response returned after successful login
/// </summary>
public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public DateTime ExpiresAt { get; set; }
    public UserProfileResponse User { get; set; } = new();
}