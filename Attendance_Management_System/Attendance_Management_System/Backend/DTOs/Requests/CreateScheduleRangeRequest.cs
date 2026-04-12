using System.ComponentModel.DataAnnotations;

namespace Attendance_Management_System.Backend.DTOs.Requests;

// Request DTO for creating one time range across multiple weekdays
public class CreateScheduleRangeRequest
{
    // Target section where schedule slots will be created
    [Required(ErrorMessage = "Section ID is required")]
    public int SectionId { get; set; }

    // Subject applied to all created slots
    [Required(ErrorMessage = "Subject ID is required")]
    public int SubjectId { get; set; }

    // Days of week to create schedules for (0=Sunday..6=Saturday)
    [Required(ErrorMessage = "At least one day is required")]
    public IReadOnlyList<int> DaysOfWeek { get; set; } = [];

    // Shared start time of the class range
    [Required(ErrorMessage = "Start time is required")]
    public TimeOnly StartTime { get; set; }

    // Shared end time of the class range
    [Required(ErrorMessage = "End time is required")]
    public TimeOnly EndTime { get; set; }

    // Date when created schedules become effective
    [Required(ErrorMessage = "Effective from date is required")]
    public DateOnly EffectiveFrom { get; set; }

    // Optional end date for created schedules
    public DateOnly? EffectiveTo { get; set; }
}
