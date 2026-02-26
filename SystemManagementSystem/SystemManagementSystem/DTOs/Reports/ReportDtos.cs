namespace SystemManagementSystem.DTOs.Reports;

public class DailyReportResponse
{
    public DateTime Date { get; set; }
    public int TotalScans { get; set; }
    public int OnTimeCount { get; set; }
    public int LateCount { get; set; }
    public int AbsentCount { get; set; }
    public int UniqueStudents { get; set; }
    public int UniqueStaff { get; set; }
    public List<DepartmentAttendanceSummary> ByDepartment { get; set; } = new();
}

public class DepartmentAttendanceSummary
{
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int TotalPersonnel { get; set; }
    public int PresentCount { get; set; }
    public int LateCount { get; set; }
    public int AbsentCount { get; set; }
    public double AttendanceRate => TotalPersonnel > 0
        ? Math.Round((double)PresentCount / TotalPersonnel * 100, 2)
        : 0;
}

public class WeeklyReportResponse
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<DailyReportResponse> DailyBreakdown { get; set; } = new();
    public int TotalScans { get; set; }
    public double AverageOnTimeRate { get; set; }
    public double AverageLateRate { get; set; }
}
