using System.ComponentModel.DataAnnotations;
using Attendance_Management_System.Backend.Enums;

namespace Attendance_Management_System.Backend.ViewModels.Sections;

public class SectionOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class SectionReferenceOptionViewModel
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
    public EducationLevel? EducationLevel { get; set; }
    public string? EducationLevelLabel { get; set; }
    public int? MinYearLevel { get; set; }
    public int? MaxYearLevel { get; set; }
}

public class SectionTeacherOptionViewModel : SectionReferenceOptionViewModel
{
    public string ShortLabel { get; set; } = string.Empty;
}

public class SectionSubjectReferenceOptionViewModel : SectionReferenceOptionViewModel
{
    public int CourseId { get; set; }
}

public class SectionTimetableRowViewModel
{
    public string TimeLabel { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public IReadOnlyList<SectionTimetableCellViewModel> Cells { get; set; } = [];
}

public class SectionTimetableCellViewModel
{
    public int DayOfWeek { get; set; }
    public string DayName { get; set; } = string.Empty;
    public bool IsOccupied { get; set; }
    public bool IsMine { get; set; }
    public bool IsStart { get; set; }
    public int? ScheduleId { get; set; }
    public int SubjectId { get; set; }
    public string SubjectName { get; set; } = string.Empty;
    public string TeacherName { get; set; } = string.Empty;
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string TimeRange { get; set; } = string.Empty;
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
    public IReadOnlyList<SectionTeacherOptionViewModel> AssignedTeachers { get; set; } = [];
    public string AssignedTeacherSummary { get; set; } = "No teacher assigned";
}

public class SectionAttendanceScheduleOptionViewModel
{
    public int Id { get; set; }
    public int SectionId { get; set; }
    public string Label { get; set; } = string.Empty;
}

public class SectionAttendanceStudentRowViewModel
{
    public int StudentId { get; set; }
    public string StudentNumber { get; set; } = "-";
    public string FullName { get; set; } = string.Empty;
    public int YearLevel { get; set; }
    public string CourseText { get; set; } = "-";
    public bool IsMarked { get; set; }
    public string StatusLabel { get; set; } = AttendanceStatusKind.Unmarked.ToString();
    public string StatusClass { get; set; } = "inactive";
    public string ExistingTimeIn { get; set; } = "-";
    public string ExistingTimeInValue { get; set; } = string.Empty;
    public string ExistingRemarks { get; set; } = "-";
    public string EditableRemarksValue { get; set; } = string.Empty;
    public string MarkerName { get; set; } = "-";
    public string ActionLabel { get; set; } = "Mark";
}

public class SectionMarkAttendanceFormViewModel
{
    [Required]
    public int SectionId { get; set; }

    [Required]
    public int ScheduleId { get; set; }

    [Required(ErrorMessage = "Student ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Student ID must be greater than 0")]
    public int StudentId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [DataType(DataType.Time)]
    public TimeOnly? TimeIn { get; set; }

    public string? Remarks { get; set; }
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

    [Required(ErrorMessage = "Academic period is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid academic period")]
    [Display(Name = "Academic Period")]
    public int AcademicYearId { get; set; }

    [Required(ErrorMessage = "Course is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid course")]
    [Display(Name = "Course")]
    public int CourseId { get; set; }

    [Required(ErrorMessage = "Subject is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid subject")]
    [Display(Name = "Subject")]
    public int SubjectId { get; set; }

    [Required(ErrorMessage = "Classroom is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid classroom")]
    [Display(Name = "Classroom")]
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
    [Required(ErrorMessage = "Teacher is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid teacher")]
    [Display(Name = "Teacher")]
    public int TeacherId { get; set; }
}
