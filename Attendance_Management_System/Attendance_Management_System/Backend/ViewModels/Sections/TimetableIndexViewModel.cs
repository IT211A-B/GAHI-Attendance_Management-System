namespace Attendance_Management_System.Backend.ViewModels.Sections;

public class TimetableIndexViewModel
{
    public IReadOnlyList<SectionOptionViewModel> SectionOptions { get; set; } = [];
    public IReadOnlyList<SectionReferenceOptionViewModel> TimetableSubjects { get; set; } = [];
    public IReadOnlyList<SectionTimetableRowViewModel> TimetableRows { get; set; } = [];
    public int? SelectedSectionId { get; set; }
    public string SelectedSectionName { get; set; } = string.Empty;
    public int SelectedSectionSubjectId { get; set; }
    public string SelectedSectionSubjectName { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool IsTeacher { get; set; }
    public bool IsCurrentTeacherAssignedToSelectedSection { get; set; }
    public string? ErrorMessage { get; set; }
    public string? TimetableErrorMessage { get; set; }
    public string? TimetableSubjectsErrorMessage { get; set; }
}
