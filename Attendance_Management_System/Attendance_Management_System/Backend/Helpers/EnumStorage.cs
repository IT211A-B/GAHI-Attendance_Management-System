using Attendance_Management_System.Backend.Enums;

namespace Attendance_Management_System.Backend.Helpers;

// Centralizes enum <-> persisted string conversions (lowercase) used across the app.
public static class EnumStorage
{
    public static string ToStorageValue(this UserRole role)
    {
        return role.ToString().ToLowerInvariant();
    }

    public static string ToStorageValue(this EnrollmentStatus status)
    {
        return status.ToString().ToLowerInvariant();
    }

    public static string ToStorageValue(this AttendanceStatusKind status)
    {
        return status.ToString().ToLowerInvariant();
    }

    public static bool IsRole(this string? role, UserRole expectedRole)
    {
        return string.Equals(role, expectedRole.ToStorageValue(), StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsEnrollmentStatus(this string? status, EnrollmentStatus expectedStatus)
    {
        return string.Equals(status, expectedStatus.ToStorageValue(), StringComparison.OrdinalIgnoreCase);
    }

    public static bool TryParseRole(string? role, out UserRole parsedRole)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            parsedRole = default;
            return false;
        }

        var normalized = role.Trim();
        if (int.TryParse(normalized, out _))
        {
            parsedRole = default;
            return false;
        }

        if (!Enum.TryParse(normalized, ignoreCase: true, out parsedRole))
        {
            return false;
        }

        return Enum.IsDefined(parsedRole);
    }

    public static bool TryParseEnrollmentStatus(string? status, out EnrollmentStatus parsedStatus)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            parsedStatus = default;
            return false;
        }

        var normalized = status.Trim();
        if (int.TryParse(normalized, out _))
        {
            parsedStatus = default;
            return false;
        }

        if (!Enum.TryParse(normalized, ignoreCase: true, out parsedStatus))
        {
            return false;
        }

        return Enum.IsDefined(parsedStatus);
    }

    public static bool TryParseAttendanceStatus(string? status, out AttendanceStatusKind parsedStatus)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            parsedStatus = default;
            return false;
        }

        var normalized = status.Trim();
        if (int.TryParse(normalized, out _))
        {
            parsedStatus = default;
            return false;
        }

        if (!Enum.TryParse(normalized, ignoreCase: true, out parsedStatus))
        {
            return false;
        }

        return Enum.IsDefined(parsedStatus);
    }
}
