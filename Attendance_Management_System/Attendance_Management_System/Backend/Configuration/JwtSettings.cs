namespace Attendance_Management_System.Backend.Configuration;

public class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string Issuer { get; set; } = "AttendanceSystem";
    public string Audience { get; set; } = "AttendanceSystemUsers";
    public string SecretKey { get; set; } = string.Empty;
    public int ExpirationHours { get; set; } = 24;
}