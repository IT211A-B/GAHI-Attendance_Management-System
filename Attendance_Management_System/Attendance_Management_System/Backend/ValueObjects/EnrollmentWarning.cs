namespace Attendance_Management_System.Backend.ValueObjects;

// Value object representing a capacity-related warning during enrollment
public class EnrollmentWarning
{
    // Error or warning code
    public string Code { get; set; } = string.Empty;

    // Human-readable warning message
    public string Message { get; set; } = string.Empty;

    // ID of the section that triggered the warning
    public int SectionId { get; set; }

    // Current enrollment count in the section
    public int CurrentCount { get; set; }

    // The warning threshold setting
    public int WarningThreshold { get; set; }

    // The over-capacity limit setting
    public int OverCapacityLimit { get; set; }

    // Creates a warning for section at warning threshold
    public static EnrollmentWarning AtWarningThreshold(int sectionId, int currentCount, int warningThreshold, int overCapacityLimit)
    {
        return new EnrollmentWarning
        {
            Code = "AT_WARNING_THRESHOLD",
            Message = $"Section has reached {currentCount} students (warning threshold: {warningThreshold}). Enrollment allowed but section is near capacity.",
            SectionId = sectionId,
            CurrentCount = currentCount,
            WarningThreshold = warningThreshold,
            OverCapacityLimit = overCapacityLimit
        };
    }

    // Creates a warning for section over capacity
    public static EnrollmentWarning OverCapacity(int sectionId, int currentCount, int warningThreshold, int overCapacityLimit)
    {
        return new EnrollmentWarning
        {
            Code = "OVER_CAPACITY_WARNING",
            Message = $"Section has {currentCount} students, exceeding warning threshold of {warningThreshold}. Enrollment allowed but section is over recommended capacity.",
            SectionId = sectionId,
            CurrentCount = currentCount,
            WarningThreshold = warningThreshold,
            OverCapacityLimit = overCapacityLimit
        };
    }
}