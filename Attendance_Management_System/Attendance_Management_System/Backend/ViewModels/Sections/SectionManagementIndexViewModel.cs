namespace Attendance_Management_System.Backend.ViewModels.Sections;

public class SectionManagementIndexViewModel
{
    public IReadOnlyList<SectionListItemViewModel> Sections { get; set; } = [];
    public IReadOnlyList<SectionTeacherOptionViewModel> TeacherOptions { get; set; } = [];
    public IReadOnlyList<SectionReferenceOptionViewModel> AcademicPeriods { get; set; } = [];
    public IReadOnlyList<SectionReferenceOptionViewModel> Courses { get; set; } = [];
    public IReadOnlyList<SectionSubjectReferenceOptionViewModel> Subjects { get; set; } = [];
    public IReadOnlyList<SectionReferenceOptionViewModel> Classrooms { get; set; } = [];
    public CreateSectionFormViewModel CreateForm { get; set; } = new();
    public bool IsAdmin { get; set; }
    public bool IsTeacher { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CreateSectionOptionsErrorMessage { get; set; }
    public string? TeacherOptionsErrorMessage { get; set; }
}
