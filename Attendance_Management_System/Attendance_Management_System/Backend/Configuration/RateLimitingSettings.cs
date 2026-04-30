namespace Attendance_Management_System.Backend.Configuration;

// Configuration settings for ASP.NET Core rate limiting policies.
public class RateLimitingSettings
{
    // Configuration section key used in appsettings.json.
    public const string SectionName = "RateLimiting";

    public FixedWindowPolicySettings Global { get; set; } = new()
    {
        PermitLimit = 120,
        WindowSeconds = 60,
        QueueLimit = 0
    };

    public FixedWindowPolicySettings AuthLogin { get; set; } = new()
    {
        PermitLimit = 8,
        WindowSeconds = 60,
        QueueLimit = 0
    };

    public FixedWindowPolicySettings AuthSignup { get; set; } = new()
    {
        PermitLimit = 3,
        WindowSeconds = 60,
        QueueLimit = 0
    };

    public FixedWindowPolicySettings AuthResendVerification { get; set; } = new()
    {
        PermitLimit = 3,
        WindowSeconds = 60,
        QueueLimit = 0
    };

    public FixedWindowPolicySettings QrSessionMutations { get; set; } = new()
    {
        PermitLimit = 12,
        WindowSeconds = 60,
        QueueLimit = 0
    };

    public FixedWindowPolicySettings QrCheckins { get; set; } = new()
    {
        PermitLimit = 20,
        WindowSeconds = 60,
        QueueLimit = 0
    };

    public FixedWindowPolicySettings QrLiveFeed { get; set; } = new()
    {
        PermitLimit = 30,
        WindowSeconds = 60,
        QueueLimit = 0
    };

    // Validates that all configured rate limiting policies are usable.
    public bool IsValid()
    {
        return Global.IsValid()
            && AuthLogin.IsValid()
            && AuthSignup.IsValid()
            && AuthResendVerification.IsValid()
            && QrSessionMutations.IsValid()
            && QrCheckins.IsValid()
            && QrLiveFeed.IsValid();
    }

    // Provides safe defaults when configuration is missing or invalid.
    public static RateLimitingSettings Default => new();
}

public class FixedWindowPolicySettings
{
    public int PermitLimit { get; set; } = 10;

    public int WindowSeconds { get; set; } = 60;

    public int QueueLimit { get; set; } = 0;

    public bool IsValid()
    {
        return PermitLimit > 0
            && WindowSeconds > 0
            && QueueLimit >= 0;
    }
}
