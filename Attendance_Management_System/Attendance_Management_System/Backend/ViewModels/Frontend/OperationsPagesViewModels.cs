namespace Attendance_Management_System.Backend.ViewModels.Frontend;

public class StaffPageViewModel
{
    public int ActiveTeachers { get; set; }
    public int InactiveTeachers { get; set; }
    public int ActiveAdmins { get; set; }
    public int ActiveStudents { get; set; }
    public IReadOnlyList<StaffTeacherItemViewModel> Teachers { get; set; } = [];
    public IReadOnlyList<StaffAdminItemViewModel> Admins { get; set; } = [];
    public string? ErrorMessage { get; set; }
}

public class StaffTeacherItemViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Department { get; set; } = "-";
    public string SectionsText { get; set; } = "-";
    public bool IsActive { get; set; }
}

public class StaffAdminItemViewModel
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class BusinessRulesPageViewModel
{
    public int WarningThreshold { get; set; }
    public int OverCapacityLimit { get; set; }
    public bool AutoCreateSections { get; set; }
    public int CookieExpirationHours { get; set; }
    public bool SlidingExpiration { get; set; }
    public string SameSite { get; set; } = string.Empty;
    public string SecurePolicy { get; set; } = string.Empty;
}

public class GateTerminalsPageViewModel
{
    public int TodayAttendanceScans { get; set; }
    public int ActiveSections { get; set; }
    public int ActiveSchedulesToday { get; set; }
}

public class AuditLogsPageViewModel
{
    public IReadOnlyList<AuditEventItemViewModel> Events { get; set; } = [];
}

public class AuditEventItemViewModel
{
    public DateTimeOffset Timestamp { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Actor { get; set; } = "System";
    public string Status { get; set; } = string.Empty;
}
