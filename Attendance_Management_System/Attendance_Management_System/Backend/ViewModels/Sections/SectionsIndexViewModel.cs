namespace Attendance_Management_System.Backend.ViewModels.Sections;

public class SectionsIndexViewModel
{
    public IReadOnlyList<SectionListItemViewModel> Sections { get; set; } = [];
    public string? ErrorMessage { get; set; }
}

public class SectionListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int YearLevel { get; set; }
    public string CourseName { get; set; } = "-";
    public string SubjectName { get; set; } = "-";
    public string ClassroomName { get; set; } = "-";
    public int CurrentEnrollmentCount { get; set; }
}