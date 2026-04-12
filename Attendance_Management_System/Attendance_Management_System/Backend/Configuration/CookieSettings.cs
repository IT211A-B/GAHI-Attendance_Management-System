namespace Attendance_Management_System.Backend.Configuration;

// Configuration settings for Identity cookie authentication
// Bound to the "CookieSettings" section in appsettings.json
public class CookieSettings
{
    // Configuration section name in appsettings.json
    public const string SectionName = "CookieSettings";

    // Number of hours before the cookie expires
    public int ExpirationHours { get; set; } = 8;

    // Whether the cookie should be renewed on each request
    public bool SlidingExpiration { get; set; } = true;

    // Prevent JavaScript access to the cookie
    public bool HttpOnly { get; set; } = true;

    // SameSite mode: Lax, Strict, or None
    public string SameSite { get; set; } = "Lax";

    // Secure policy: Always, SameAsRequest, or None
    public string SecurePolicy { get; set; } = "SameAsRequest";
}
