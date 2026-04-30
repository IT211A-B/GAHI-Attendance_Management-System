using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Configuration;
using Attendance_Management_System.Backend.Enums;
using Attendance_Management_System.Backend.Helpers;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Attendance_Management_System.Backend.ViewModels.Dashboard;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Attendance_Management_System.Backend.Services;

public class DashboardService : IDashboardService
{
    // Shared limits keep dashboard queries bounded for fast initial page loads.
    private const int RecentRecordsLimit = 10;
    private const int UpcomingDaysRange = 7;
    private const int RiskRowsLimit = 10;
    private const decimal AtRiskAbsentRateThreshold = 20m;
    // Window keys are persisted to the UI filter state, so keep them stable.
    private const string WindowAcademic = "academic";
    private const string WindowLast7 = "last7";
    private const string WindowLast30 = "last30";
    private const string WindowThisMonth = "thismonth";
    private const string WindowCustom = "custom";

    private readonly AppDbContext _context;
    private readonly AttendanceSettings _attendanceSettings;

    public DashboardService(AppDbContext context, IOptions<AttendanceSettings> attendanceSettings)
    {
        _context = context;
        // Fall back to defaults when configuration is missing or invalid.
        _attendanceSettings = attendanceSettings.Value?.IsValid() == true
            ? attendanceSettings.Value
            : AttendanceSettings.Default;
    }

    public async Task<DashboardIndexViewModel> BuildIndexViewModelAsync(int userId, string role, string? window, DateOnly? from, DateOnly? to)
    {
        var normalizedRole = role.Trim().ToLowerInvariant();
        var currentAcademicYear = await GetCurrentAcademicYearAsync();
        // Date filtering is centralized so all role dashboards use the same window rules.
        var filter = BuildDateFilter(currentAcademicYear, window, from, to);

        var viewModel = new DashboardIndexViewModel
        {
            IsAdmin = normalizedRole == "admin",
            IsTeacher = normalizedRole == "teacher",
            IsStudent = normalizedRole == "student",
            AcademicPeriodLabel = currentAcademicYear?.YearLabel,
            Filters = filter
        };

        // Populate only the section needed for the caller's role.
        switch (normalizedRole)
        {
            case "student":
                viewModel.Student = await BuildStudentSectionAsync(userId, filter);
                viewModel.ErrorMessage = viewModel.Student is null
                    ? "Student profile is not available for this account."
                    : null;
                break;

            case "teacher":
                viewModel.Teacher = await BuildTeacherSectionAsync(userId, filter);
                viewModel.ErrorMessage = viewModel.Teacher is null
                    ? "Teacher profile is not available for this account."
                    : null;
                break;

            case "admin":
                viewModel.Admin = await BuildAdminSectionAsync();
                break;

            default:
                viewModel.ErrorMessage = "Your role is not allowed to access this dashboard.";
                break;
        }

        return viewModel;
    }

    private async Task<StudentDashboardSectionViewModel?> BuildStudentSectionAsync(int userId, DashboardDateFilterViewModel filter)
    {
        var student = await _context.Students
            .AsNoTracking()
            .Include(s => s.Course)
            .Include(s => s.Section)
            .FirstOrDefaultAsync(s => s.UserId == userId);

        if (student is null)
        {
            return null;
        }

        var attendanceBaseQuery = _context.Attendances
            .AsNoTracking()
            .Where(a => a.StudentId == student.Id)
            .Where(a => a.Date >= filter.EffectiveFrom && a.Date <= filter.EffectiveTo);

        var summaryRows = await attendanceBaseQuery
            .Select(a => new
            {
                a.TimeIn,
                StartTime = a.Schedule != null ? a.Schedule.StartTime : new TimeOnly(0, 0)
            })
            .ToListAsync();

        // Status values always come from policy so late/present rules stay consistent app-wide.
        var summaryStatuses = summaryRows
            .Select(row => AttendancePolicy.GetMarkedStatus(row.TimeIn, row.StartTime, _attendanceSettings))
            .ToList();

        var presentCount = summaryStatuses.Count(AttendancePolicy.CountsAsPresent);
        var lateCount = summaryStatuses.Count(status => status == AttendanceStatusKind.Late);
        var absentCount = summaryStatuses.Count(status => status == AttendanceStatusKind.Absent);
        var attendanceRate = summaryRows.Count == 0
            ? 0m
            : decimal.Round((decimal)presentCount / summaryRows.Count * 100m, 1);

        var recentAttendanceRows = await attendanceBaseQuery
            .Include(a => a.Schedule)
                .ThenInclude(s => s!.Subject)
            .Include(a => a.Section)
            .OrderByDescending(a => a.Date)
            .ThenByDescending(a => a.MarkedAt)
            .Take(RecentRecordsLimit)
            .ToListAsync();

        var recentRecords = recentAttendanceRows
            .Select(MapStudentAttendanceRecord)
            .ToList();

        return new StudentDashboardSectionViewModel
        {
            StudentName = BuildName(student.FirstName, student.MiddleName, student.LastName),
            StudentNumber = student.StudentNumber,
            CourseText = string.IsNullOrWhiteSpace(student.Course?.Code)
                ? student.Course?.Name ?? "-"
                : $"{student.Course.Code} {student.Course?.Name}".Trim(),
            SectionName = student.Section?.Name ?? "-",
            PresentCount = presentCount,
            LateCount = lateCount,
            AbsentCount = absentCount,
            AttendanceRate = attendanceRate,
            RecentRecords = recentRecords
        };
    }

    private async Task<TeacherDashboardSectionViewModel?> BuildTeacherSectionAsync(int userId, DashboardDateFilterViewModel filter)
    {
        var teacher = await _context.Teachers
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.UserId == userId);

        if (teacher is null)
        {
            return null;
        }

        var assignedSectionIds = await _context.SectionTeachers
            .AsNoTracking()
            .Where(st => st.TeacherId == teacher.Id)
            .Select(st => st.SectionId)
            .Distinct()
            .ToListAsync();

        var schedules = await _context.Schedules
            .AsNoTracking()
            .Include(s => s.Section)
                .ThenInclude(sec => sec!.Classroom)
            .Include(s => s.Subject)
            .Where(s => s.TeacherId == teacher.Id)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ToListAsync();

        var todayDate = DateOnly.FromDateTime(DateTime.Today);
        // Teacher counters use the same selected date window as the rest of the dashboard.
        var (presentCount, lateCount, absentCount) = await BuildTeacherWindowTotalsAsync(teacher.Id, filter);
        var upcomingClasses = BuildUpcomingClasses(schedules, todayDate, UpcomingDaysRange);
        var atRiskStudents = await BuildTeacherAtRiskStudentsAsync(teacher.Id, filter);

        return new TeacherDashboardSectionViewModel
        {
            TeacherName = BuildName(teacher.FirstName, teacher.MiddleName, teacher.LastName),
            AssignedSectionsCount = assignedSectionIds.Count,
            AssignedSchedulesCount = schedules.Count,
            TodayPresentCount = presentCount,
            TodayLateCount = lateCount,
            TodayAbsentCount = absentCount,
            UpcomingClasses = upcomingClasses,
            AtRiskStudents = atRiskStudents
        };
    }

    private async Task<AdminDashboardSectionViewModel> BuildAdminSectionAsync()
    {
        var statusCounts = await _context.Enrollments
            .AsNoTracking()
            .GroupBy(e => e.Status)
            .Select(group => new
            {
                Status = group.Key,
                Count = group.Count()
            })
            .ToListAsync();

        var pendingCount = statusCounts.FirstOrDefault(s => s.Status == "pending")?.Count ?? 0;
        var approvedCount = statusCounts.FirstOrDefault(s => s.Status == "approved")?.Count ?? 0;
        var rejectedCount = statusCounts.FirstOrDefault(s => s.Status == "rejected")?.Count ?? 0;

        var activeStudentsCount = await _context.Students
            .AsNoTracking()
            .CountAsync(s => s.IsActive);

        var activeTeachersCount = await _context.Teachers
            .AsNoTracking()
            .CountAsync(t => t.IsActive);

        var totalSectionsCount = await _context.Sections
            .AsNoTracking()
            .CountAsync();

        return new AdminDashboardSectionViewModel
        {
            PendingEnrollmentsCount = pendingCount,
            ApprovedEnrollmentsCount = approvedCount,
            RejectedEnrollmentsCount = rejectedCount,
            ActiveStudentsCount = activeStudentsCount,
            ActiveTeachersCount = activeTeachersCount,
            TotalSectionsCount = totalSectionsCount
        };
    }

    private async Task<(int PresentCount, int LateCount, int AbsentCount)> BuildTeacherWindowTotalsAsync(
        int teacherId,
        DashboardDateFilterViewModel filter)
    {
        var rows = await _context.Attendances
            .AsNoTracking()
            .Where(attendance => attendance.Schedule != null && attendance.Schedule.TeacherId == teacherId)
            .Where(attendance => attendance.Date >= filter.EffectiveFrom && attendance.Date <= filter.EffectiveTo)
            .Select(attendance => new
            {
                attendance.TimeIn,
                StartTime = attendance.Schedule != null ? attendance.Schedule.StartTime : new TimeOnly(0, 0)
            })
            .ToListAsync();

        var statuses = rows
            .Select(row => AttendancePolicy.GetMarkedStatus(row.TimeIn, row.StartTime, _attendanceSettings))
            .ToList();

        var presentCount = statuses.Count(AttendancePolicy.CountsAsPresent);
        var lateCount = statuses.Count(status => status == AttendanceStatusKind.Late);
        var absentCount = statuses.Count(status => status == AttendanceStatusKind.Absent);

        return (presentCount, lateCount, absentCount);
    }

    private async Task<IReadOnlyList<TeacherAtRiskStudentViewModel>> BuildTeacherAtRiskStudentsAsync(
        int teacherId,
        DashboardDateFilterViewModel filter)
    {
        // Group by student + section to compute risk metrics per class context.
        var groupedRows = await _context.Attendances
            .AsNoTracking()
            .Where(a => a.Schedule != null && a.Schedule.TeacherId == teacherId)
            .Where(a => a.Date >= filter.EffectiveFrom && a.Date <= filter.EffectiveTo)
            .GroupBy(a => new { a.StudentId, a.SectionId })
            .Select(group => new
            {
                group.Key.StudentId,
                group.Key.SectionId,
                TotalRecords = group.Count(),
                AbsentCount = group.Count(item => !item.TimeIn.HasValue)
            })
            .ToListAsync();

        if (!groupedRows.Any())
        {
            return [];
        }

        var studentIds = groupedRows.Select(row => row.StudentId).Distinct().ToList();
        var sectionIds = groupedRows.Select(row => row.SectionId).Distinct().ToList();

        var studentNames = await _context.Students
            .AsNoTracking()
            .Where(student => studentIds.Contains(student.Id))
            .Select(student => new
            {
                student.Id,
                student.FirstName,
                student.MiddleName,
                student.LastName
            })
            .ToDictionaryAsync(student => student.Id, student => BuildName(student.FirstName, student.MiddleName, student.LastName));

        var sectionNames = await _context.Sections
            .AsNoTracking()
            .Where(section => sectionIds.Contains(section.Id))
            .Select(section => new { section.Id, section.Name })
            .ToDictionaryAsync(section => section.Id, section => section.Name);

        var candidates = groupedRows
            .Select(row =>
            {
                var absentRate = row.TotalRecords == 0
                    ? 0m
                    : decimal.Round((decimal)row.AbsentCount / row.TotalRecords * 100m, 1);

                studentNames.TryGetValue(row.StudentId, out var studentName);
                sectionNames.TryGetValue(row.SectionId, out var sectionName);

                return new TeacherAtRiskStudentViewModel
                {
                    StudentId = row.StudentId,
                    StudentName = string.IsNullOrWhiteSpace(studentName) ? "-" : studentName,
                    SectionName = string.IsNullOrWhiteSpace(sectionName) ? "-" : sectionName,
                    AbsentCount = row.AbsentCount,
                    TotalRecords = row.TotalRecords,
                    AbsentRate = absentRate
                };
            })
            // "At risk" means sustained absence rate at or above the configured threshold.
            .Where(item => item.TotalRecords > 0 && item.AbsentRate >= AtRiskAbsentRateThreshold)
            .OrderByDescending(item => item.AbsentRate)
            .ThenByDescending(item => item.AbsentCount)
            .ThenBy(item => item.StudentName)
            .Take(RiskRowsLimit)
            .ToList();

        return candidates;
    }

    private DashboardDateFilterViewModel BuildDateFilter(
        AcademicYear? currentAcademicYear,
        string? window,
        DateOnly? from,
        DateOnly? to)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var selectedWindow = NormalizeWindow(window);

        var filter = new DashboardDateFilterViewModel
        {
            SelectedWindow = selectedWindow,
            CustomFrom = from,
            CustomTo = to
        };

        // Resolve the effective date range once, then reuse it in all downstream queries.
        switch (selectedWindow)
        {
            case WindowLast7:
                filter.EffectiveFrom = today.AddDays(-6);
                filter.EffectiveTo = today;
                filter.EffectiveLabel = "Last 7 days";
                break;

            case WindowLast30:
                filter.EffectiveFrom = today.AddDays(-29);
                filter.EffectiveTo = today;
                filter.EffectiveLabel = "Last 30 days";
                break;

            case WindowThisMonth:
                filter.EffectiveFrom = new DateOnly(today.Year, today.Month, 1);
                filter.EffectiveTo = today;
                filter.EffectiveLabel = "This month";
                break;

            case WindowCustom:
                if (from.HasValue && to.HasValue && from.Value <= to.Value)
                {
                    filter.EffectiveFrom = from.Value;
                    filter.EffectiveTo = to.Value;
                    filter.EffectiveLabel = $"Custom: {from.Value:yyyy-MM-dd} to {to.Value:yyyy-MM-dd}";
                }
                else
                {
                    filter.SelectedWindow = WindowAcademic;
                    // Invalid custom ranges gracefully fall back instead of throwing.
                    ApplyAcademicFallback(filter, currentAcademicYear, today);
                    filter.Message = "Custom range is invalid. Falling back to current academic period.";
                }
                break;

            case WindowAcademic:
            default:
                ApplyAcademicFallback(filter, currentAcademicYear, today);
                break;
        }

        return filter;
    }

    private static string NormalizeWindow(string? window)
    {
        var normalized = (window ?? string.Empty).Trim().ToLowerInvariant();

        return normalized switch
        {
            WindowAcademic => WindowAcademic,
            WindowLast7 => WindowLast7,
            WindowLast30 => WindowLast30,
            WindowThisMonth => WindowThisMonth,
            WindowCustom => WindowCustom,
            _ => WindowAcademic
        };
    }

    private static void ApplyAcademicFallback(
        DashboardDateFilterViewModel filter,
        AcademicYear? currentAcademicYear,
        DateOnly today)
    {
        if (currentAcademicYear is null)
        {
            // Keep the dashboard usable even when academic years are not configured yet.
            filter.EffectiveFrom = today.AddDays(-29);
            filter.EffectiveTo = today;
            filter.EffectiveLabel = "Last 30 days";
            return;
        }

        var effectiveTo = currentAcademicYear.EndDate < today
            ? currentAcademicYear.EndDate
            : today;

        // Guard against malformed academic periods where end date can precede start date.
        if (effectiveTo < currentAcademicYear.StartDate)
        {
            effectiveTo = currentAcademicYear.StartDate;
        }

        filter.EffectiveFrom = currentAcademicYear.StartDate;
        filter.EffectiveTo = effectiveTo;
        filter.EffectiveLabel = currentAcademicYear.YearLabel;
    }

    private static List<TeacherUpcomingClassViewModel> BuildUpcomingClasses(
        List<Schedule> schedules,
        DateOnly startDate,
        int daysRange)
    {
        var upcoming = new List<TeacherUpcomingClassViewModel>();

        // Expand the next N calendar days, then match schedules that are active on each day.
        for (var offset = 0; offset < daysRange; offset++)
        {
            var targetDate = startDate.AddDays(offset);
            var targetDayOfWeek = (int)targetDate.DayOfWeek;

            foreach (var schedule in schedules)
            {
                if (schedule.DayOfWeek != targetDayOfWeek)
                {
                    continue;
                }

                if (schedule.EffectiveFrom > targetDate || (schedule.EffectiveTo is not null && schedule.EffectiveTo < targetDate))
                {
                    continue;
                }

                upcoming.Add(new TeacherUpcomingClassViewModel
                {
                    Date = targetDate,
                    SectionName = schedule.Section?.Name ?? "-",
                    SubjectName = schedule.Subject?.Name ?? "-",
                    ClassroomName = schedule.Section?.Classroom?.Name ?? "-",
                    StartTime = schedule.StartTime.ToString("HH:mm"),
                    EndTime = schedule.EndTime.ToString("HH:mm")
                });
            }
        }

        return upcoming
            .OrderBy(item => item.Date)
            .ThenBy(item => item.StartTime)
            // Keep cards compact; full schedules are available elsewhere.
            .Take(12)
            .ToList();
    }

    private async Task<AcademicYear?> GetCurrentAcademicYearAsync()
    {
        // Prefer the explicitly active year; otherwise use the most recent configured period.
        var activeAcademicYear = await _context.AcademicYears
            .AsNoTracking()
            .FirstOrDefaultAsync(academicYear => academicYear.IsActive);

        if (activeAcademicYear is not null)
        {
            return activeAcademicYear;
        }

        return await _context.AcademicYears
            .AsNoTracking()
            .OrderByDescending(academicYear => academicYear.StartDate)
            .FirstOrDefaultAsync();
    }

    private StudentAttendanceRecordViewModel MapStudentAttendanceRecord(Attendance attendance)
    {
        var scheduleStart = attendance.Schedule?.StartTime ?? new TimeOnly(0, 0);
        // Status mapping uses shared policy to keep labels and CSS classes in sync everywhere.
        var status = AttendancePolicy.GetMarkedStatus(attendance.TimeIn, scheduleStart, _attendanceSettings);

        return new StudentAttendanceRecordViewModel
        {
            Date = attendance.Date,
            SubjectName = attendance.Schedule?.Subject?.Name ?? "-",
            SectionName = attendance.Section?.Name ?? "-",
            TimeInText = attendance.TimeIn?.ToString("HH:mm") ?? "-",
            StatusLabel = AttendancePolicy.ToLabel(status),
            StatusClass = AttendancePolicy.ToCssClass(status)
        };
    }

    private static string BuildName(string firstName, string? middleName, string lastName)
    {
        return string.Join(" ", new[] { firstName, middleName, lastName }
            .Where(part => !string.IsNullOrWhiteSpace(part)));
    }
}
