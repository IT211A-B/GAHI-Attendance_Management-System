using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Configuration;
using Attendance_Management_System.Backend.Enums;
using Attendance_Management_System.Backend.Helpers;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Attendance_Management_System.Backend.Services;

// Service implementation for teacher history operations
// Provides read-only access to teacher's schedules and attendance history
public class TeacherHistoryService : ITeacherHistoryService
{
    private readonly AppDbContext _context;
    private readonly AttendanceSettings _attendanceSettings;

    public TeacherHistoryService(AppDbContext context, IOptions<AttendanceSettings> attendanceSettings)
    {
        _context = context;
        _attendanceSettings = attendanceSettings.Value?.IsValid() == true
            ? attendanceSettings.Value
            : AttendanceSettings.Default;
    }

    // Get all schedule slots for sections the teacher is assigned to
    public async Task<ApiResponse<List<TeacherScheduleDto>>> GetTeacherSchedulesAsync(int userId)
    {
        // Find the teacher record by UserId
        var teacher = await _context.Teachers
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.UserId == userId);

        if (teacher == null)
        {
            return ApiResponse<List<TeacherScheduleDto>>.ErrorResponse(
                "NOT_FOUND",
                "Teacher profile not found.");
        }

        // Get all schedules owned by this teacher with related data
        var schedules = await _context.Schedules
            .AsNoTracking()
            .Include(s => s.Section)
                .ThenInclude(sec => sec!.Classroom)
            .Include(s => s.Subject)
            .Where(s => s.TeacherId == teacher.Id)
            .OrderBy(s => s.DayOfWeek)
            .ThenBy(s => s.StartTime)
            .ToListAsync();

        // Map to DTOs
        var scheduleDtos = schedules.Select(s => new TeacherScheduleDto
        {
            Id = s.Id,
            SectionId = s.SectionId,
            SectionName = s.Section?.Name ?? string.Empty,
            SubjectId = s.SubjectId,
            SubjectName = s.Subject?.Name ?? string.Empty,
            ClassroomName = s.Section?.Classroom?.Name ?? string.Empty,
            DayOfWeek = s.DayOfWeek,
            DayName = GetDayName(s.DayOfWeek),
            StartTime = s.StartTime.ToString("HH:mm"),
            EndTime = s.EndTime.ToString("HH:mm")
        }).ToList();

        return ApiResponse<List<TeacherScheduleDto>>.SuccessResponse(scheduleDtos);
    }

    // Get attendance history for a specific schedule with optional date filter
    public async Task<ApiResponse<ScheduleHistoryDto>> GetScheduleHistoryAsync(int scheduleId, int userId, DateOnly? date)
    {
        // Find the teacher record by UserId
        var teacher = await _context.Teachers
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.UserId == userId);

        if (teacher == null)
        {
            return ApiResponse<ScheduleHistoryDto>.ErrorResponse(
                "NOT_FOUND",
                "Teacher profile not found.");
        }

        // Get the schedule with related data
        var schedule = await _context.Schedules
            .AsNoTracking()
            .Include(s => s.Section)
                .ThenInclude(sec => sec!.Classroom)
            .Include(s => s.Subject)
            .FirstOrDefaultAsync(s => s.Id == scheduleId);

        if (schedule == null)
        {
            return ApiResponse<ScheduleHistoryDto>.ErrorResponse(
                "NOT_FOUND",
                "Schedule not found.");
        }

        // Verify teacher owns the target schedule slot
        if (schedule.TeacherId != teacher.Id)
        {
            return ApiResponse<ScheduleHistoryDto>.ErrorResponse(
                "FORBIDDEN",
                "You can only view history for your own schedule slots.");
        }

        // Use provided date or default to today
        var filterDate = date ?? DateOnly.FromDateTime(DateTime.Today);

        // Get attendance records for the schedule and date.
        var attendances = await _context.Attendances
            .AsNoTracking()
            .Include(a => a.Student)
            .Include(a => a.Schedule)
                .ThenInclude(s => s!.Subject)
            .Where(a => a.ScheduleId == scheduleId && a.Date == filterDate)
            .ToListAsync();

        // Get all active students in the section for summary calculation
        var sectionStudents = await _context.Students
            .AsNoTracking()
            .Where(s => s.SectionId == schedule.SectionId && s.IsActive)
            .ToListAsync();

        var markerUserIds = attendances
            .Where(attendance => attendance.MarkedBy > 0)
            .Select(attendance => attendance.MarkedBy)
            .Distinct()
            .ToHashSet();

        var teacherNames = await _context.Teachers
            .AsNoTracking()
            .Where(teacher => markerUserIds.Contains(teacher.UserId))
            .ToDictionaryAsync(teacher => teacher.UserId, teacher => $"{teacher.FirstName} {teacher.LastName}");

        // Build ScheduleInfoDto.
        var scheduleInfo = new ScheduleInfoDto
        {
            SubjectName = schedule.Subject?.Name ?? string.Empty,
            Section = schedule.Section?.Name ?? string.Empty,
            Classroom = schedule.Section?.Classroom?.Name ?? string.Empty,
            Day = GetDayName(schedule.DayOfWeek),
            StartTime = schedule.StartTime.ToString("HH:mm"),
            EndTime = schedule.EndTime.ToString("HH:mm")
        };

        var recordsWithStatus = new List<(AttendanceDto Record, AttendanceStatusKind Status)>();
        foreach (var attendance in attendances)
        {
            teacherNames.TryGetValue(attendance.MarkedBy, out var markerName);
            var status = AttendancePolicy.GetMarkedStatus(attendance.TimeIn, schedule.StartTime, _attendanceSettings);

            recordsWithStatus.Add((new AttendanceDto
            {
                Id = attendance.Id,
                ScheduleId = attendance.ScheduleId,
                SubjectName = schedule.Subject?.Name,
                StudentId = attendance.StudentId,
                StudentName = attendance.Student != null ? $"{attendance.Student.FirstName} {attendance.Student.LastName}" : null,
                SectionId = attendance.SectionId,
                SectionName = schedule.Section?.Name,
                Date = attendance.Date,
                TimeIn = attendance.TimeIn,
                TimeOut = attendance.TimeOut,
                Remarks = string.IsNullOrWhiteSpace(attendance.Remarks)
                    ? AttendancePolicy.ToLabel(status)
                    : attendance.Remarks,
                MarkedAt = attendance.MarkedAt,
                MarkedBy = attendance.MarkedBy,
                MarkerName = markerName,
                IsMarked = true,
                IsLate = status == AttendanceStatusKind.Late,
                StatusLabel = AttendancePolicy.ToLabel(status),
                StatusClass = AttendancePolicy.ToCssClass(status)
            }, status));
        }

        var records = recordsWithStatus.Select(row => row.Record).ToList();

        // Add unmarked students (students without attendance records).
        var attendedStudentIds = attendances.Select(attendance => attendance.StudentId).ToHashSet();
        var unmarkedStudents = sectionStudents.Where(student => !attendedStudentIds.Contains(student.Id));

        foreach (var student in unmarkedStudents)
        {
            records.Add(new AttendanceDto
            {
                Id = 0,
                ScheduleId = scheduleId,
                SubjectName = schedule.Subject?.Name,
                StudentId = student.Id,
                StudentName = $"{student.FirstName} {student.LastName}",
                SectionId = schedule.SectionId,
                SectionName = schedule.Section?.Name,
                Date = filterDate,
                TimeIn = null,
                TimeOut = null,
                Remarks = "Unmarked",
                MarkedAt = DateTimeOffset.MinValue,
                MarkedBy = 0,
                MarkerName = null,
                IsMarked = false,
                IsLate = false,
                StatusLabel = AttendancePolicy.ToLabel(AttendanceStatusKind.Unmarked),
                StatusClass = AttendancePolicy.ToCssClass(AttendanceStatusKind.Unmarked)
            });
        }

        var presentCount = recordsWithStatus.Count(row => AttendancePolicy.CountsAsPresent(row.Status));
        var lateCount = recordsWithStatus.Count(row => row.Status == AttendanceStatusKind.Late);
        var absentCount = recordsWithStatus.Count(row => row.Status == AttendanceStatusKind.Absent);
        var unmarkedCount = sectionStudents.Count - recordsWithStatus.Count;

        var summary = new AttendanceSummaryDto
        {
            TotalStudents = sectionStudents.Count,
            PresentCount = presentCount,
            AbsentCount = absentCount,
            UnmarkedCount = unmarkedCount,
            LateCount = lateCount,
            Records = new List<AttendanceDto>() // Not used in this context
        };

        // Build the response
        var historyDto = new ScheduleHistoryDto
        {
            Schedule = scheduleInfo,
            Date = filterDate,
            Records = records.OrderBy(r => r.StudentName).ToList(),
            Summary = summary
        };

        return ApiResponse<ScheduleHistoryDto>.SuccessResponse(historyDto);
    }

    // Helper method to convert DayOfWeek integer to day name
    private static string GetDayName(int dayOfWeek)
    {
        return dayOfWeek switch
        {
            0 => "Sunday",
            1 => "Monday",
            2 => "Tuesday",
            3 => "Wednesday",
            4 => "Thursday",
            5 => "Friday",
            6 => "Saturday",
            _ => "Unknown"
        };
    }
}