namespace Attendance_Management_System.Backend.ViewModels.Students;

public class StudentsIndexViewModel
{
    public IReadOnlyList<StudentsSectionOptionViewModel> Sections { get; set; } = [];
    public IReadOnlyList<StudentListItemViewModel> Students { get; set; } = [];
    public int? SelectedSectionId { get; set; }
    public string? ErrorMessage { get; set; }
}

public class StudentsSectionOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class StudentListItemViewModel
{
    public int Id { get; set; }
    public string StudentNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int YearLevel { get; set; }
    public string CourseText { get; set; } = "-";
    public string SectionName { get; set; } = "-";
}
