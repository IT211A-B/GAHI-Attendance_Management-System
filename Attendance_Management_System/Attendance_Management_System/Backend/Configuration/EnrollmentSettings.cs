namespace Attendance_Management_System.Backend.Configuration;

// Configuration settings for enrollment capacity management
public class EnrollmentSettings
{
    public const string SectionName = "EnrollmentSettings";

    // Number of students before warning is triggered (default: 50)
    public int WarningThreshold { get; set; } = 50;

    // Maximum students allowed before auto-creating new section (default: 55)
    // Must be greater than WarningThreshold
    public int OverCapacityLimit { get; set; } = 55;

    // Whether to automatically create new sections when all are at capacity
    public bool AutoCreateSections { get; set; } = true;

    // Validates that settings are properly configured
    // Returns true if valid, false if defaults should be used
    public bool IsValid()
    {
        return OverCapacityLimit > WarningThreshold && WarningThreshold > 0;
    }

    // Returns default settings
    public static EnrollmentSettings Default => new()
    {
        WarningThreshold = 50,
        OverCapacityLimit = 55,
        AutoCreateSections = true
    };
}