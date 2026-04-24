namespace Attendance_Management_System.Backend.Constants;

public static class NotificationTypes
{
    public const string Signup = "signup";
    public const string Enrollment = "enrollment";
    public const string Checkin = "checkin";

    private static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        Signup,
        Enrollment,
        Checkin
    };

    public static string Normalize(string? type)
    {
        return (type ?? string.Empty).Trim().ToLowerInvariant();
    }

    public static bool IsSupported(string normalizedType)
    {
        return SupportedTypes.Contains(normalizedType);
    }

    public static string TypeCheckConstraintSql =>
        $"\"Type\" IN ('{Signup}', '{Enrollment}', '{Checkin}')";
}

public static class NotificationHubChannels
{
    public const string NewEventName = "notification:new";

    public static string BuildUserGroupName(int userId)
    {
        return $"user:{userId}";
    }

    public static string BuildRoleGroupName(string? role)
    {
        return $"role:{NormalizeRole(role)}";
    }

    public static string NormalizeRole(string? role)
    {
        return (role ?? string.Empty).Trim().ToLowerInvariant();
    }
}

public static class NotificationLinks
{
    public const string Enrollments = "/enrollments";
    public const string AttendanceQr = "/attendance/qr";
}
