namespace Attendance_Management_System.Backend.DTOs.Requests;

// Request DTO for updating an existing schedule slot
public class UpdateScheduleRequest
{
    // Optional: Change the subject being taught
    public int? SubjectId { get; set; }

    // Optional: Change day of week (0=Sunday, 1=Monday, ..., 6=Saturday)
    public int? DayOfWeek { get; set; }

    // Optional: Change start time
    public TimeOnly? StartTime { get; set; }

    // Optional: Change end time
    public TimeOnly? EndTime { get; set; }

    // Optional: Change effective from date
    public DateOnly? EffectiveFrom { get; set; }

    // Optional: Change or nullify end date (set to null to remove end date)
    public DateOnly? EffectiveTo { get; set; }
}