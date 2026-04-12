namespace Attendance_Management_System.Backend.DTOs.Responses;

// Response shape for the timetable grid endpoint
// Contains a week's schedule organized by day
public class TimetableResponse
{
    public int SectionId { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public Dictionary<string, List<ScheduleSlotDto>> Timetable { get; set; } = new();
}