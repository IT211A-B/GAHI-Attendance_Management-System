using System.ComponentModel.DataAnnotations;

namespace Attendance_Management_System.Backend.ViewModels.AcademicPeriods;

public class AcademicPeriodsIndexViewModel
{
    public IReadOnlyList<AcademicPeriodListItemViewModel> Periods { get; set; } = [];
    public CreateAcademicPeriodFormViewModel CreateForm { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class AcademicPeriodListItemViewModel
{
    public int Id { get; set; }
    public string YearLabel { get; set; } = string.Empty;
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class CreateAcademicPeriodFormViewModel
{
    [Required(ErrorMessage = "Year label is required")]
    [Display(Name = "Year label")]
    public string YearLabel { get; set; } = string.Empty;

    [Required(ErrorMessage = "Start date is required")]
    [Display(Name = "Start date")]
    [DataType(DataType.Date)]
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required(ErrorMessage = "End date is required")]
    [Display(Name = "End date")]
    [DataType(DataType.Date)]
    public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddMonths(10));
}

public class UpdateAcademicPeriodFormViewModel
{
    [Required(ErrorMessage = "Year label is required")]
    [Display(Name = "Year label")]
    public string YearLabel { get; set; } = string.Empty;

    [Required(ErrorMessage = "Start date is required")]
    [Display(Name = "Start date")]
    [DataType(DataType.Date)]
    public DateOnly StartDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Required(ErrorMessage = "End date is required")]
    [Display(Name = "End date")]
    [DataType(DataType.Date)]
    public DateOnly EndDate { get; set; } = DateOnly.FromDateTime(DateTime.Today.AddMonths(10));
}
