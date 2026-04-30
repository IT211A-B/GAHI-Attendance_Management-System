using System.ComponentModel.DataAnnotations;

namespace Attendance_Management_System.Backend.ViewModels.Subjects;

public class SubjectsIndexViewModel
{
    public IReadOnlyList<SubjectListItemViewModel> Subjects { get; set; } = [];
    public IReadOnlyList<SubjectCourseOptionViewModel> Courses { get; set; } = [];
    public CreateSubjectFormViewModel CreateForm { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

public class SubjectCourseOptionViewModel
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class SubjectListItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public string CourseLabel { get; set; } = "-";
    public int Units { get; set; }
}

public class CreateSubjectFormViewModel
{
    [Required(ErrorMessage = "Name is required")]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Code is required")]
    [Display(Name = "Code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Course is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid course")]
    [Display(Name = "Course")]
    public int CourseId { get; set; }

    [Required(ErrorMessage = "Units is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Units must be at least 1")]
    [Display(Name = "Units")]
    public int Units { get; set; } = 3;
}

public class UpdateSubjectFormViewModel
{
    [Required(ErrorMessage = "Name is required")]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Code is required")]
    [Display(Name = "Code")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Course is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid course")]
    [Display(Name = "Course")]
    public int CourseId { get; set; }

    [Required(ErrorMessage = "Units is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Units must be at least 1")]
    [Display(Name = "Units")]
    public int Units { get; set; }
}
