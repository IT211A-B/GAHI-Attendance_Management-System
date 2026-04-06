using Attendance_Management_System.Backend.Configuration;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Enums;

namespace Attendance_Management_System.Backend.Helpers;

public static class AttendancePolicy
{
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

    public static DateOnly GetSchoolDate(AttendanceSettings settings, DateTimeOffset utcNow)
    {
        var schoolTime = TimeZoneInfo.ConvertTime(utcNow, ResolveSchoolTimeZone(settings));
        return DateOnly.FromDateTime(schoolTime.DateTime);
    }

    public static bool IsWithinTeacherWindow(AttendanceSettings settings, DateOnly targetDate, DateOnly schoolToday)
    {
        var earliestDate = schoolToday.AddDays(-settings.TeacherBackfillDays);
        return targetDate >= earliestDate && targetDate <= schoolToday;
    }

    public static bool IsDateAlignedWithSchedule(Schedule schedule, DateOnly targetDate)
    {
        if ((int)targetDate.DayOfWeek != schedule.DayOfWeek)
        {
            return false;
        }

        if (targetDate < schedule.EffectiveFrom)
        {
            return false;
        }

        if (schedule.EffectiveTo.HasValue && targetDate > schedule.EffectiveTo.Value)
        {
            return false;
        }

        return true;
    }

    public static TimeOnly GetLateThreshold(TimeOnly scheduleStartTime, AttendanceSettings settings)
    {
        return scheduleStartTime.AddMinutes(settings.LateGraceMinutes);
    }

    public static AttendanceStatusKind GetMarkedStatus(TimeOnly? timeIn, TimeOnly scheduleStartTime, AttendanceSettings settings)
    {
        if (!timeIn.HasValue)
        {
            return AttendanceStatusKind.Absent;
        }

        var lateThreshold = GetLateThreshold(scheduleStartTime, settings);
        return timeIn.Value > lateThreshold
            ? AttendanceStatusKind.Late
            : AttendanceStatusKind.Present;
    }

    public static bool CountsAsPresent(AttendanceStatusKind status)
    {
        return status == AttendanceStatusKind.Present || status == AttendanceStatusKind.Late;
    }

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
