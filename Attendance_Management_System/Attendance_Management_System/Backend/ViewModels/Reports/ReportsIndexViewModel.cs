namespace Attendance_Management_System.Backend.ViewModels.Reports;

public class ReportsIndexViewModel
{
    public int? SelectedSectionId { get; set; }
    public int? SelectedScheduleId { get; set; }
    public DateOnly SelectedDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public IReadOnlyList<ReportsSectionOptionViewModel> Sections { get; set; } = [];
    public IReadOnlyList<ReportsScheduleOptionViewModel> Schedules { get; set; } = [];
    public int TotalStudents { get; set; }
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int AbsentCount { get; set; }
    public decimal PresentRate { get; set; }
    public decimal LateRate { get; set; }
    public decimal AbsentRate { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ReportsSectionOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class ReportsScheduleOptionViewModel
{
    public int Id { get; set; }
    public int SectionId { get; set; }
    public string Label { get; set; } = string.Empty;
}
