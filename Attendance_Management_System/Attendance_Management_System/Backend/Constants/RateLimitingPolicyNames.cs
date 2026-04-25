namespace Attendance_Management_System.Backend.Constants;

// Named rate limiting policy identifiers used by middleware and endpoint attributes.
public static class RateLimitingPolicyNames
{
    public const string AuthLogin = "auth-login";

    public const string AuthSignup = "auth-signup";

    public const string AuthResendVerification = "auth-resend-verification";

    public const string QrSessionMutation = "qr-session-mutation";

    public const string QrCheckin = "qr-checkin";

    public const string QrLiveFeed = "qr-live-feed";
}
