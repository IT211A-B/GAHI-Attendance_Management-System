namespace Attendance_Management_System.Backend.Configuration;

// Configuration settings for attendance policy behavior.
public class AttendanceSettings
{
    // Configuration section key used in appsettings.json.
    public const string SectionName = "AttendanceSettings";

    // Number of minutes after schedule start when a student is considered late.
    public int LateGraceMinutes { get; set; } = 15;

    // How many days teachers can backfill attendance from school "today".
    public int TeacherBackfillDays { get; set; } = 7;

    // Allow attendance marking on dates that do not match the schedule weekday.
    public bool AllowOffScheduleAttendance { get; set; } = false;

    // School timezone used for date-window validation.
    public string TimezoneId { get; set; } = "Asia/Manila";

    // Validates that all required settings have acceptable values.
    public bool IsValid()
    {
        return LateGraceMinutes >= 0
            && TeacherBackfillDays >= 0
            && !string.IsNullOrWhiteSpace(TimezoneId);
    }

    // Provides default configuration values for the attendance system.
    public static AttendanceSettings Default => new()
    {
        LateGraceMinutes = 15,
        TeacherBackfillDays = 7,
        AllowOffScheduleAttendance = false,
        TimezoneId = "Asia/Manila"
    };
}
