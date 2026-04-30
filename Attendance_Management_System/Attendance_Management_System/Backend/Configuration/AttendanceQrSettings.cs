namespace Attendance_Management_System.Backend.Configuration;

// Configuration settings for QR attendance session lifecycle and token signing.
public class AttendanceQrSettings
{
    // Configuration section key used in appsettings.json.
    public const string SectionName = "AttendanceQrSettings";

    // Time-to-live for each QR token/session in seconds. 15mins
    public int SessionTtlSeconds { get; set; } = 900;

    // When remaining time reaches this value, frontend should rotate token.
    public int RefreshThresholdSeconds { get; set; } = 60;

    // Polling interval for teacher live check-in feed.
    public int LiveFeedPollSeconds { get; set; } = 3;

    // Shared secret used for HMAC token signing.
    public string SigningKey { get; set; } = "CHANGE_THIS_DEVELOPMENT_QR_SIGNING_KEY";

    // Validates that all required settings have acceptable values.
    public bool IsValid()
    {
        return SessionTtlSeconds >= 30
            && RefreshThresholdSeconds >= 1
            && RefreshThresholdSeconds < SessionTtlSeconds
            && LiveFeedPollSeconds >= 1
            && !string.IsNullOrWhiteSpace(SigningKey)
            && SigningKey.Trim().Length >= 16;
    }

    // Provides default values when configuration is missing or invalid.
    public static AttendanceQrSettings Default => new()
    {
        SessionTtlSeconds = 90,
        RefreshThresholdSeconds = 10,
        LiveFeedPollSeconds = 3,
        SigningKey = "CHANGE_THIS_DEVELOPMENT_QR_SIGNING_KEY"
    };
}
