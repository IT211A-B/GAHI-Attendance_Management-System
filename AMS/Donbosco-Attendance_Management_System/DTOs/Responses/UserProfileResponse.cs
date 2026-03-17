namespace Donbosco_Attendance_Management_System.DTOs.Responses;

/// <summary>
/// User profile data returned in responses
/// </summary>
public class UserProfileResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}