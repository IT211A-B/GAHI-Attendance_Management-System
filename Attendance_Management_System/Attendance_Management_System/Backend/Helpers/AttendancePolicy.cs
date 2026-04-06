using Attendance_Management_System.Backend.Configuration;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Enums;

namespace Attendance_Management_System.Backend.Helpers;

// Provides static helper methods for attendance policy calculations and validations.
public static class AttendancePolicy
{
    // Resolves the school's timezone from settings, falling back to UTC if invalid.
    public static TimeZoneInfo ResolveSchoolTimeZone(AttendanceSettings settings)
    {
        var timezoneId = string.IsNullOrWhiteSpace(settings.TimezoneId)
            ? AttendanceSettings.Default.TimezoneId
            : settings.TimezoneId;

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timezoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.Utc;
        }
        catch (InvalidTimeZoneException)
        {
            return TimeZoneInfo.Utc;
        }
    }

    // Converts UTC time to the school's local date based on configured timezone.
    public static DateOnly GetSchoolDate(AttendanceSettings settings, DateTimeOffset utcNow)
    {
        var schoolTime = TimeZoneInfo.ConvertTime(utcNow, ResolveSchoolTimeZone(settings));
        return DateOnly.FromDateTime(schoolTime.DateTime);
    }

    // Checks if a target date falls within the teacher's allowed backfill window.
    public static bool IsWithinTeacherWindow(AttendanceSettings settings, DateOnly targetDate, DateOnly schoolToday)
    {
        var earliestDate = schoolToday.AddDays(-settings.TeacherBackfillDays);
        return targetDate >= earliestDate && targetDate <= schoolToday;
    }

    // Validates that a date matches the schedule's day of week and effective date range.
    public static bool IsDateAlignedWithSchedule(Schedule schedule, DateOnly targetDate)
    {
        // Check if the day of week matches the schedule.
        if ((int)targetDate.DayOfWeek != schedule.DayOfWeek)
        {
            return false;
        }

        // Ensure the date is on or after the schedule's effective start date.
        if (targetDate < schedule.EffectiveFrom)
        {
            return false;
        }

        // Ensure the date is on or before the schedule's effective end date (if set).
        if (schedule.EffectiveTo.HasValue && targetDate > schedule.EffectiveTo.Value)
        {
            return false;
        }

        return true;
    }

    // Calculates the time after which a student is considered late.
    public static TimeOnly GetLateThreshold(TimeOnly scheduleStartTime, AttendanceSettings settings)
    {
        return scheduleStartTime.AddMinutes(settings.LateGraceMinutes);
    }

    // Determines the attendance status based on time-in relative to schedule start.
    public static AttendanceStatusKind GetMarkedStatus(TimeOnly? timeIn, TimeOnly scheduleStartTime, AttendanceSettings settings)
    {
        // No time-in recorded means the student is absent.
        if (!timeIn.HasValue)
        {
            return AttendanceStatusKind.Absent;
        }

        var lateThreshold = GetLateThreshold(scheduleStartTime, settings);
        return timeIn.Value > lateThreshold
            ? AttendanceStatusKind.Late
            : AttendanceStatusKind.Present;
    }

    // Returns true if the status indicates the student was present (on time or late).
    public static bool CountsAsPresent(AttendanceStatusKind status)
    {
        return status == AttendanceStatusKind.Present || status == AttendanceStatusKind.Late;
    }

    // Converts an attendance status to a human-readable label for display.
    public static string ToLabel(AttendanceStatusKind status)
    {
        return status switch
        {
            AttendanceStatusKind.Present => "Present",
            AttendanceStatusKind.Late => "Late",
            AttendanceStatusKind.Absent => "Absent",
            _ => "Unmarked"
        };
    }

    // Converts an attendance status to a CSS class name for styling.
    public static string ToCssClass(AttendanceStatusKind status)
    {
        return status switch
        {
            AttendanceStatusKind.Present => "success",
            AttendanceStatusKind.Late => "warning",
            AttendanceStatusKind.Absent => "danger",
            _ => "inactive"
        };
    }
}
