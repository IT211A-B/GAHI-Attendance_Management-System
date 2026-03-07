using SystemManagementSystem.DTOs.Reports;

namespace SystemManagementSystem.Services.Interfaces;

public interface IReportService
{
    Task<DailyReportResponse> GetDailyReportAsync(DateTime date);
    Task<WeeklyReportResponse> GetWeeklyReportAsync(DateTime startDate);
    Task<List<DepartmentAttendanceSummary>> GetDepartmentSummaryAsync(DateTime date);
}
