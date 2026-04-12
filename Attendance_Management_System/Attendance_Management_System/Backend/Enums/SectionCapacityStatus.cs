namespace Attendance_Management_System.Backend.Enums;

// Represents the capacity status of a section based on enrollment count
public enum SectionCapacityStatus
{
    // Section has normal capacity (0 to WarningThreshold-1 students)
    Available = 0,

    // Section is at warning threshold (exactly WarningThreshold students)
    AtWarning = 1,

    // Section is over warning threshold but within limit (WarningThreshold+1 to OverCapacityLimit)
    OverCapacity = 2
}