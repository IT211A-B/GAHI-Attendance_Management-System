namespace Attendance_Management_System.Backend.ViewModels.Dashboard;

public class DashboardIndexViewModel
{
    public bool IsAdmin { get; set; }
    public bool IsTeacher { get; set; }
    public bool IsStudent { get; set; }

    public string? AcademicPeriodLabel { get; set; }
    public string? ErrorMessage { get; set; }
    public DashboardDateFilterViewModel Filters { get; set; } = new();

    public StudentDashboardSectionViewModel? Student { get; set; }
    public TeacherDashboardSectionViewModel? Teacher { get; set; }
    public AdminDashboardSectionViewModel? Admin { get; set; }
}

public class DashboardDateFilterViewModel
{
    public string SelectedWindow { get; set; } = "academic";
    public DateOnly? CustomFrom { get; set; }
    public DateOnly? CustomTo { get; set; }
    public DateOnly EffectiveFrom { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public DateOnly EffectiveTo { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public string EffectiveLabel { get; set; } = "Current academic period";
    public string? Message { get; set; }
}

public class StudentDashboardSectionViewModel
{
    public string StudentName { get; set; } = "-";
    public string StudentNumber { get; set; } = "-";
    public string CourseText { get; set; } = "-";
    public string SectionName { get; set; } = "-";

    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int AbsentCount { get; set; }
    public decimal AttendanceRate { get; set; }

    public IReadOnlyList<StudentAttendanceRecordViewModel> RecentRecords { get; set; } = [];
}

public class StudentAttendanceRecordViewModel
{
    public DateOnly Date { get; set; }
    public string SubjectName { get; set; } = "-";
    public string SectionName { get; set; } = "-";
    public string TimeInText { get; set; } = "-";
    public string StatusLabel { get; set; } = "-";
    public string StatusClass { get; set; } = "muted";
}

public class TeacherDashboardSectionViewModel
{
    public string TeacherName { get; set; } = "-";

    public int AssignedSectionsCount { get; set; }
    public int AssignedSchedulesCount { get; set; }

    public int TodayPresentCount { get; set; }
    public int TodayLateCount { get; set; }
    public int TodayAbsentCount { get; set; }

    public IReadOnlyList<TeacherUpcomingClassViewModel> UpcomingClasses { get; set; } = [];
    public IReadOnlyList<TeacherAtRiskStudentViewModel> AtRiskStudents { get; set; } = [];
}

public class TeacherUpcomingClassViewModel
{
    public DateOnly Date { get; set; }
    public string SectionName { get; set; } = "-";
    public string SubjectName { get; set; } = "-";
    public string ClassroomName { get; set; } = "-";
    public string StartTime { get; set; } = "-";
    public string EndTime { get; set; } = "-";
}

public class TeacherAtRiskStudentViewModel
{
    public int StudentId { get; set; }
    public string StudentName { get; set; } = "-";
    public string SectionName { get; set; } = "-";
    public int AbsentCount { get; set; }
    public int TotalRecords { get; set; }
    public decimal AbsentRate { get; set; }
}

public class AdminDashboardSectionViewModel
{
    public int PendingEnrollmentsCount { get; set; }
    public int ApprovedEnrollmentsCount { get; set; }
    public int RejectedEnrollmentsCount { get; set; }

    public int ActiveStudentsCount { get; set; }
    public int ActiveTeachersCount { get; set; }
    public int TotalSectionsCount { get; set; }
}
