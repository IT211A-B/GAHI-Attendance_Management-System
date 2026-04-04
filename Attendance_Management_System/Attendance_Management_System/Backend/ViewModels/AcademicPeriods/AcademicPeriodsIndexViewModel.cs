namespace Attendance_Management_System.Backend.ViewModels.AcademicPeriods;

public class AcademicPeriodsIndexViewModel
{
    public IReadOnlyList<AcademicPeriodListItemViewModel> Periods { get; set; } = [];
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
