using Attendance_Management_System.Backend.Enums;

namespace Attendance_Management_System.Backend.DTOs.Responses;

public class SectionCapacityDto
{
    public int SectionId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public int YearLevel { get; set; }
    public int CurrentEnrollment { get; set; }
    public int WarningThreshold { get; set; }
    public int OverCapacityLimit { get; set; }
    public SectionCapacityStatus Status { get; set; }
    public int AvailableSlots { get; set; }
}