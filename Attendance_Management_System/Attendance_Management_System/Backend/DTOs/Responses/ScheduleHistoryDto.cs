namespace Attendance_Management_System.Backend.DTOs.Responses;

// DTO for schedule history response
// Contains schedule details, attendance records, and summary statistics
public class ScheduleHistoryDto
{
    // Schedule details
    public ScheduleInfoDto Schedule { get; set; } = new();

    // The filtered date
    public DateOnly Date { get; set; }

    // Attendance records - uses existing AttendanceDto
    public List<AttendanceDto> Records { get; set; } = new();

    // Summary statistics - uses existing AttendanceSummaryDto
    public AttendanceSummaryDto Summary { get; set; } = new();
}