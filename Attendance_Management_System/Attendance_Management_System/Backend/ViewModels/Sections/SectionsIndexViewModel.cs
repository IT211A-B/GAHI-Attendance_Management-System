using System.ComponentModel.DataAnnotations;

namespace Attendance_Management_System.Backend.ViewModels.Sections;

public class SectionsIndexViewModel
{
    public IReadOnlyList<SectionListItemViewModel> Sections { get; set; } = [];
    public IReadOnlyList<SectionOptionViewModel> SectionOptions { get; set; } = [];
    public IReadOnlyList<SectionReferenceOptionViewModel> AcademicPeriods { get; set; } = [];
    public IReadOnlyList<SectionReferenceOptionViewModel> Courses { get; set; } = [];
    public IReadOnlyList<SectionSubjectReferenceOptionViewModel> Subjects { get; set; } = [];
    public IReadOnlyList<SectionReferenceOptionViewModel> Classrooms { get; set; } = [];
    public IReadOnlyList<SectionTimetableRowViewModel> TimetableRows { get; set; } = [];
    public IReadOnlyList<SectionAttendanceScheduleOptionViewModel> AttendanceSchedules { get; set; } = [];
    public IReadOnlyList<SectionAttendanceStudentRowViewModel> AttendanceStudents { get; set; } = [];
    public CreateSectionFormViewModel CreateForm { get; set; } = new();
    public int? SelectedSectionId { get; set; }
    public int? SelectedAttendanceScheduleId { get; set; }
    public DateOnly SelectedAttendanceDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string SelectedSectionName { get; set; } = string.Empty;
    public int SelectedSectionSubjectId { get; set; }
    public string SelectedSectionSubjectName { get; set; } = string.Empty;
    public bool IsAdmin { get; set; }
    public bool IsTeacher { get; set; }
    public int AttendanceTotalStudents { get; set; }
    public int AttendancePresentCount { get; set; }
    public int AttendanceLateCount { get; set; }
    public int AttendanceAbsentCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string? CreateSectionOptionsErrorMessage { get; set; }
    public string? TimetableErrorMessage { get; set; }
    public string? AttendanceErrorMessage { get; set; }
}

public class SectionOptionViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class SectionReferenceOptionViewModel
{
    public int Id { get; set; }
    public string Label { get; set; } = string.Empty;
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
    public string StatusLabel { get; set; } = "Not Marked";
    public string StatusClass { get; set; } = "muted";
    public string ExistingTimeIn { get; set; } = "-";
    public string ExistingRemarks { get; set; } = "-";
    public string MarkerName { get; set; } = "-";
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
    [Required(ErrorMessage = "Teacher ID is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Teacher ID must be greater than 0")]
    [Display(Name = "Teacher ID")]
    public int TeacherId { get; set; }
}