namespace Attendance_Management_System.Backend.Configuration;

// Configuration settings for JWT token generation and validation
// Bound to the "JwtSettings" section in appsettings.json
public class JwtSettings
{
    // Configuration section name in appsettings.json
    public const string SectionName = "JwtSettings";

    // Token issuer - identifies the system that issued the token
    public string Issuer { get; set; } = "AttendanceSystem";

    // Token audience - identifies the intended recipients
    public string Audience { get; set; } = "AttendanceSystemUsers";

    // Secret key used to sign tokens (should be stored securely in production)
    public string SecretKey { get; set; } = string.Empty;

    // Number of hours before the token expires
    public int ExpirationHours { get; set; } = 24;
}