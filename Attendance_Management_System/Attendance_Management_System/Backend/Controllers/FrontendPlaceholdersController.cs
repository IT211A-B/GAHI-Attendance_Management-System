using Attendance_Management_System.Backend.Configuration;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Attendance_Management_System.Backend.ViewModels.Frontend;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Attendance_Management_System.Backend.Controllers;

[Authorize]
public class FrontendPlaceholdersController : Controller
{
    private readonly ITeachersService _teachersService;
    private readonly IUsersService _usersService;
    private readonly AppDbContext _context;
    private readonly IOptions<EnrollmentSettings> _enrollmentSettings;
    private readonly IOptions<CookieSettings> _cookieSettings;

    public FrontendPlaceholdersController(
        ITeachersService teachersService,
        IUsersService usersService,
        AppDbContext context,
        IOptions<EnrollmentSettings> enrollmentSettings,
        IOptions<CookieSettings> cookieSettings)
    {
        _teachersService = teachersService;
        _usersService = usersService;
        _context = context;
        _enrollmentSettings = enrollmentSettings;
        _cookieSettings = cookieSettings;
    }

    [HttpGet("audit-logs")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> AuditLogs()
    {
        var enrollmentEvents = await _context.Enrollments
            .AsNoTracking()
            .Include(e => e.Student)
            .Include(e => e.Section)
            .Include(e => e.Processor)
            .OrderByDescending(e => e.ProcessedAt ?? e.CreatedAt)
            .Take(40)
            .ToListAsync();

        var attendanceEvents = await _context.Attendances
            .AsNoTracking()
            .Include(a => a.Student)
            .Include(a => a.Schedule)
                .ThenInclude(s => s!.Subject)
            .Include(a => a.Marker)
            .OrderByDescending(a => a.MarkedAt)
            .Take(40)
            .ToListAsync();

        var events = new List<AuditEventItemViewModel>();

        events.AddRange(enrollmentEvents.Select(enrollment => new AuditEventItemViewModel
        {
            Timestamp = enrollment.ProcessedAt ?? enrollment.CreatedAt,
            Category = "Enrollment",
            Status = string.IsNullOrWhiteSpace(enrollment.Status) ? "pending" : enrollment.Status,
            Actor = enrollment.Processor?.Email ?? "Student",
            Description = $"{(enrollment.Student != null ? enrollment.Student.FirstName + " " + enrollment.Student.LastName : "Student")} -> {enrollment.Section?.Name ?? "Section"} ({enrollment.Status})"
        }));

        events.AddRange(attendanceEvents.Select(attendance => new AuditEventItemViewModel
        {
            Timestamp = attendance.MarkedAt,
            Category = "Attendance",
            Status = attendance.Remarks ?? (attendance.TimeIn.HasValue ? "present" : "absent"),
            Actor = attendance.Marker?.Email ?? "Teacher",
            Description = $"{attendance.Student?.FirstName} {attendance.Student?.LastName} | {attendance.Schedule?.Subject?.Name ?? "Subject"} | {attendance.Date:yyyy-MM-dd}"
        }));

        var model = new AuditLogsPageViewModel
        {
            Events = events
                .OrderByDescending(evt => evt.Timestamp)
                .Take(60)
                .ToList()
        };

        return View(model);
    }

    [HttpGet("business-rules")]
    [Authorize(Policy = "AdminOnly")]
    public IActionResult BusinessRules()
    {
        var enrollment = _enrollmentSettings.Value?.IsValid() == true
            ? _enrollmentSettings.Value
            : EnrollmentSettings.Default;

        var cookie = _cookieSettings.Value;

        var model = new BusinessRulesPageViewModel
        {
            WarningThreshold = enrollment.WarningThreshold,
            OverCapacityLimit = enrollment.OverCapacityLimit,
            AutoCreateSections = enrollment.AutoCreateSections,
            CookieExpirationHours = cookie.ExpirationHours,
            SlidingExpiration = cookie.SlidingExpiration,
            SameSite = cookie.SameSite,
            SecurePolicy = cookie.SecurePolicy
        };

        return View(model);
    }

    [HttpGet("gate-terminals")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> GateTerminals()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var currentDay = (int)DateTime.Today.DayOfWeek;

        var model = new GateTerminalsPageViewModel
        {
            TodayAttendanceScans = await _context.Attendances
                .AsNoTracking()
                .CountAsync(a => a.Date == today),
            ActiveSections = await _context.Sections
                .AsNoTracking()
                .CountAsync(),
            ActiveSchedulesToday = await _context.Schedules
                .AsNoTracking()
                .CountAsync(s => s.DayOfWeek == currentDay)
        };

        return View(model);
    }

    [HttpGet("staff")]
    [Authorize(Policy = "AdminOnly")]
    public async Task<IActionResult> Staff()
    {
        var model = new StaffPageViewModel();

        var teachersResult = await _teachersService.GetAllTeachersWithSectionsAsync();
        if (!teachersResult.Success || teachersResult.Data is null)
        {
            model.ErrorMessage = teachersResult.Error?.Message ?? "Unable to load teachers right now.";
            return View(model);
        }

        var usersResult = await _usersService.GetAllUsersAsync();
        if (!usersResult.Success || usersResult.Data is null)
        {
            model.ErrorMessage = usersResult.Error?.Message ?? "Unable to load users right now.";
            return View(model);
        }

        model.Teachers = teachersResult.Data
            .OrderBy(t => t.LastName)
            .ThenBy(t => t.FirstName)
            .Select(teacher => new StaffTeacherItemViewModel
            {
                Id = teacher.Id,
                Name = string.Join(" ", new[] { teacher.FirstName, teacher.MiddleName, teacher.LastName }
                    .Where(part => !string.IsNullOrWhiteSpace(part))),
                Email = teacher.Email,
                Department = teacher.Department,
                SectionsText = teacher.Sections.Count == 0
                    ? "-"
                    : string.Join(", ", teacher.Sections.OrderBy(section => section.SectionName).Select(section => section.SectionName)),
                IsActive = teacher.IsActive
            })
            .ToList();

        var admins = usersResult.Data
            .Where(user => user.Role == "admin")
            .OrderBy(user => user.Email)
            .ToList();

        model.Admins = admins
            .Select(admin => new StaffAdminItemViewModel
            {
                Id = admin.Id,
                Email = admin.Email,
                IsActive = admin.IsActive
            })
            .ToList();

        model.ActiveTeachers = model.Teachers.Count(t => t.IsActive);
        model.InactiveTeachers = model.Teachers.Count(t => !t.IsActive);
        model.ActiveAdmins = admins.Count(u => u.IsActive);
        model.ActiveStudents = usersResult.Data.Count(u => u.Role == "student" && u.IsActive);

        return View(model);
    }
}
