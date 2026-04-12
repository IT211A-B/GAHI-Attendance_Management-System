namespace Attendance_Management_System.Backend.DTOs.Responses;

// Response DTO for available time slots in a classroom
public class AvailableSlotDto
{
    // ID of the classroom
    public int ClassroomId { get; set; }

    // Name of the classroom (e.g., "Room 101")
    public string ClassroomName { get; set; } = string.Empty;

    // Day of week: 0=Sunday, 1=Monday, ..., 6=Saturday
    public int DayOfWeek { get; set; }

    // Human-readable day name (e.g., "Monday")
    public string DayName { get; set; } = string.Empty;

    // List of available time slots (gaps between scheduled classes)
    public List<TimeSlotRange> AvailableSlots { get; set; } = [];
}

// Represents a time range for an available slot
public class TimeSlotRange
{
    // Start time in HH:mm format (e.g., "07:00")
    public string StartTime { get; set; } = string.Empty;

    // End time in HH:mm format (e.g., "08:00")
    public string EndTime { get; set; } = string.Empty;
}