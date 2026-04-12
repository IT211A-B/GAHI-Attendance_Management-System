using System.ComponentModel.DataAnnotations;

namespace Attendance_Management_System.Backend.ViewModels.Schedules;

public class SchedulesIndexViewModel
{
    public IReadOnlyList<ScheduleListItemViewModel> Schedules { get; set; } = [];
    public CreateScheduleFormViewModel CreateForm { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class ScheduleListItemViewModel
{
    public int Id { get; set; }
    public string SectionName { get; set; } = string.Empty;
    public string SubjectName { get; set; } = string.Empty;
    public string ClassroomName { get; set; } = string.Empty;
    public string DayName { get; set; } = string.Empty;
    public string TimeRange { get; set; } = string.Empty;
    public bool IsMine { get; set; }
}

public class CreateScheduleFormViewModel
{
    [Required(ErrorMessage = "Section ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Section ID must be greater than 0")]
    [Display(Name = "Section ID")]
    public int SectionId { get; set; }

    [Required(ErrorMessage = "Subject ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Subject ID must be greater than 0")]
    [Display(Name = "Subject ID")]
    public int SubjectId { get; set; }

    [Required(ErrorMessage = "Day of week is required")]
    [Range(0, 6, ErrorMessage = "Day of week must be between 0 and 6")]
    [Display(Name = "Day of week")]
    public int DayOfWeek { get; set; } = 1;

    [Required(ErrorMessage = "Start time is required")]
    [Display(Name = "Start time")]
    [DataType(DataType.Time)]
    public TimeOnly StartTime { get; set; } = new(8, 0);

    [Required(ErrorMessage = "End time is required")]
    [Display(Name = "End time")]
    [DataType(DataType.Time)]
    public TimeOnly EndTime { get; set; } = new(9, 0);

    [Required(ErrorMessage = "Effective from date is required")]
    [Display(Name = "Effective from")]
    [DataType(DataType.Date)]
    public DateOnly EffectiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = "Effective to")]
    [DataType(DataType.Date)]
    public DateOnly? EffectiveTo { get; set; }
}

public class UpdateScheduleFormViewModel
{
    [Required(ErrorMessage = "Subject ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Subject ID must be greater than 0")]
    [Display(Name = "Subject ID")]
    public int SubjectId { get; set; }

    [Required(ErrorMessage = "Day of week is required")]
    [Range(0, 6, ErrorMessage = "Day of week must be between 0 and 6")]
    [Display(Name = "Day of week")]
    public int DayOfWeek { get; set; } = 1;

    [Required(ErrorMessage = "Start time is required")]
    [Display(Name = "Start time")]
    [DataType(DataType.Time)]
    public TimeOnly StartTime { get; set; } = new(8, 0);

    [Required(ErrorMessage = "End time is required")]
    [Display(Name = "End time")]
    [DataType(DataType.Time)]
    public TimeOnly EndTime { get; set; } = new(9, 0);

    [Required(ErrorMessage = "Effective from date is required")]
    [Display(Name = "Effective from")]
    [DataType(DataType.Date)]
    public DateOnly EffectiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = "Effective to")]
    [DataType(DataType.Date)]
    public DateOnly? EffectiveTo { get; set; }
}