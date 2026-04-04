using System.ComponentModel.DataAnnotations;

namespace Attendance_Management_System.Backend.ViewModels.Sections;

public class SectionsIndexViewModel
{
    public IReadOnlyList<SectionListItemViewModel> Sections { get; set; } = [];
    public CreateSectionFormViewModel CreateForm { get; set; } = new();
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

public class CreateSectionFormViewModel
{
    [Required(ErrorMessage = "Name is required")]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Year level is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Year level must be at least 1")]
    [Display(Name = "Year level")]
    public int YearLevel { get; set; } = 1;

    [Required(ErrorMessage = "Academic year ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Academic year ID must be greater than 0")]
    [Display(Name = "Academic year ID")]
    public int AcademicYearId { get; set; }

    [Required(ErrorMessage = "Course ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Course ID must be greater than 0")]
    [Display(Name = "Course ID")]
    public int CourseId { get; set; }

    [Required(ErrorMessage = "Subject ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Subject ID must be greater than 0")]
    [Display(Name = "Subject ID")]
    public int SubjectId { get; set; }

    [Required(ErrorMessage = "Classroom ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Classroom ID must be greater than 0")]
    [Display(Name = "Classroom ID")]
    public int ClassroomId { get; set; }
}

public class UpdateSectionFormViewModel
{
    [Required(ErrorMessage = "Name is required")]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Year level is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Year level must be at least 1")]
    [Display(Name = "Year level")]
    public int YearLevel { get; set; } = 1;
}

public class AssignSectionTeacherFormViewModel
{
    [Required(ErrorMessage = "Teacher ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Teacher ID must be greater than 0")]
    [Display(Name = "Teacher ID")]
    public int TeacherId { get; set; }
}