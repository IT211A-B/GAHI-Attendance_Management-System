using Microsoft.EntityFrameworkCore;
using SystemManagementSystem.Data;
using SystemManagementSystem.DTOs.Reports;
using SystemManagementSystem.Models.Enums;
using SystemManagementSystem.Services.Interfaces;

namespace SystemManagementSystem.Services.Implementations;

public class ReportService : IReportService
{
    private readonly ApplicationDbContext _context;

    public ReportService(ApplicationDbContext context) => _context = context;

    public async Task<DailyReportResponse> GetDailyReportAsync(DateTime date)
    {
        var targetDate = date.Date;

        var logs = await _context.AttendanceLogs
            .Include(a => a.Student)
            .Include(a => a.Staff)
            .Where(a => a.ScannedAt.Date == targetDate)
            .ToListAsync();

        var entryLogs = logs.Where(l => l.ScanType == ScanType.Entry).ToList();

        return new DailyReportResponse
        {
            Date = targetDate,
            TotalScans = logs.Count,
            OnTimeCount = entryLogs.Count(l => l.Status == AttendanceStatus.OnTime),
            LateCount = entryLogs.Count(l => l.Status == AttendanceStatus.Late),
            AbsentCount = entryLogs.Count(l => l.Status == AttendanceStatus.Absent),
            UniqueStudents = logs.Where(l => l.StudentId.HasValue).Select(l => l.StudentId).Distinct().Count(),
            UniqueStaff = logs.Where(l => l.StaffId.HasValue).Select(l => l.StaffId).Distinct().Count(),
            ByDepartment = await GetDepartmentSummaryAsync(targetDate)
        };
    }

    public async Task<WeeklyReportResponse> GetWeeklyReportAsync(DateTime startDate)
    {
        var start = startDate.Date;
        var end = start.AddDays(7);

        var dailyReports = new List<DailyReportResponse>();
        for (var d = start; d < end; d = d.AddDays(1))
        {
            dailyReports.Add(await GetDailyReportAsync(d));
        }

        var reportsWithScans = dailyReports.Where(r => r.TotalScans > 0).ToList();

        return new WeeklyReportResponse
        {
            StartDate = start,
            EndDate = end.AddDays(-1),
            DailyBreakdown = dailyReports,
            TotalScans = dailyReports.Sum(r => r.TotalScans),
            AverageOnTimeRate = reportsWithScans.Any()
                ? Math.Round(reportsWithScans.Average(r =>
                    (double)r.OnTimeCount / Math.Max(r.OnTimeCount + r.LateCount, 1) * 100), 2)
                : 0,
            AverageLateRate = reportsWithScans.Any()
                ? Math.Round(reportsWithScans.Average(r =>
                    (double)r.LateCount / Math.Max(r.OnTimeCount + r.LateCount, 1) * 100), 2)
                : 0
        };
    }

    public async Task<List<DepartmentAttendanceSummary>> GetDepartmentSummaryAsync(DateTime date)
    {
        var targetDate = date.Date;

        var departments = await _context.Departments
            .Include(d => d.Staff)
            .Include(d => d.AcademicPrograms)
                .ThenInclude(p => p.Sections)
                    .ThenInclude(s => s.Students)
            .ToListAsync();

        var entryLogs = await _context.AttendanceLogs
            .Include(a => a.Student).ThenInclude(s => s!.Section).ThenInclude(sec => sec.AcademicProgram)
            .Include(a => a.Staff)
            .Where(a => a.ScannedAt.Date == targetDate && a.ScanType == ScanType.Entry)
            .ToListAsync();

        var summaries = new List<DepartmentAttendanceSummary>();

        foreach (var dept in departments)
        {
            var totalStudents = dept.AcademicPrograms
                .SelectMany(p => p.Sections)
                .SelectMany(s => s.Students)
                .Count(s => s.EnrollmentStatus == EnrollmentStatus.Active);

            var totalStaff = dept.Staff.Count;
            var totalPersonnel = totalStudents + totalStaff;

            var deptEntryLogs = entryLogs.Where(l =>
                (l.Student != null && l.Student.Section.AcademicProgram.DepartmentId == dept.Id) ||
                (l.Staff != null && l.Staff.DepartmentId == dept.Id)).ToList();

            var uniquePresent = deptEntryLogs
                .Select(l => l.StudentId ?? l.StaffId)
                .Distinct()
                .Count();

            summaries.Add(new DepartmentAttendanceSummary
            {
                DepartmentId = dept.Id,
                DepartmentName = dept.Name,
                TotalPersonnel = totalPersonnel,
                PresentCount = uniquePresent,
                LateCount = deptEntryLogs.Count(l => l.Status == AttendanceStatus.Late),
                AbsentCount = Math.Max(0, totalPersonnel - uniquePresent)
            });
        }

        return summaries;
    }
}
