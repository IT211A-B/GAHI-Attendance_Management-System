using System.ComponentModel.DataAnnotations;

namespace Attendance_Management_System.Backend.DTOs.Requests;

// Request DTO for creating a new schedule slot
public class CreateScheduleRequest
{
    // Target section where the schedule will be created
    [Required(ErrorMessage = "Section ID is required")]
    public int SectionId { get; set; }

    // Subject being taught in this time slot
    [Required(ErrorMessage = "Subject ID is required")]
    public int SubjectId { get; set; }

    // Day of week: 0=Sunday, 1=Monday, ..., 6=Saturday
    [Required(ErrorMessage = "Day of week is required")]
    [Range(0, 6, ErrorMessage = "Day of week must be between 0 (Sunday) and 6 (Saturday)")]
    public int DayOfWeek { get; set; }

    // Start time of the class
    [Required(ErrorMessage = "Start time is required")]
    public TimeOnly StartTime { get; set; }

    // End time of the class (must be after start time)
    [Required(ErrorMessage = "End time is required")]
    public TimeOnly EndTime { get; set; }

    // Date when this schedule becomes effective
    [Required(ErrorMessage = "Effective from date is required")]
    public DateOnly EffectiveFrom { get; set; }

    // Optional end date for schedule changes (null if currently active)
    public DateOnly? EffectiveTo { get; set; }
}