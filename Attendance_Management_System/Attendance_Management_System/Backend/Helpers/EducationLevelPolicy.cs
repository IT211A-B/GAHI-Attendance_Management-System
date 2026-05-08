using Attendance_Management_System.Backend.Enums;

namespace Attendance_Management_System.Backend.Helpers;

// Provides shared school-stage rules for year-level validation.
public static class EducationLevelPolicy
{
    public static (int MinYearLevel, int MaxYearLevel) GetAllowedYearRange(EducationLevel level)
    {
        return level switch
        {
            EducationLevel.Elementary => (1, 6),
            EducationLevel.JuniorHigh => (7, 10),
            EducationLevel.SeniorHigh => (11, 12),
            EducationLevel.College => (1, 4),
            EducationLevel.Tvet => (1, 2),
            _ => (1, 12)
        };
    }

    public static bool IsYearLevelAllowed(EducationLevel level, int yearLevel)
    {
        var range = GetAllowedYearRange(level);
        return yearLevel >= range.MinYearLevel && yearLevel <= range.MaxYearLevel;
    }

    public static string ToDisplayLabel(EducationLevel level)
    {
        return level switch
        {
            EducationLevel.Elementary => "Elementary",
            EducationLevel.JuniorHigh => "Junior High",
            EducationLevel.SeniorHigh => "Senior High",
            EducationLevel.College => "College",
            EducationLevel.Tvet => "TVET",
            _ => "Unknown"
        };
    }
}