using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Attendance_Management_System.Backend.Services;

// Service implementation for teacher history operations
// Provides read-only access to teacher's schedules and attendance history
public class TeacherHistoryService : ITeacherHistoryService
{
    private readonly AppDbContext _context;

    public TeacherHistoryService(AppDbContext context)
    {
        _context = context;
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

        // Get section IDs that the teacher is assigned to via SectionTeachers bridge table
        var assignedSectionIds = await _context.SectionTeachers
            .AsNoTracking()
            .Where(st => st.TeacherId == teacher.Id)
            .Select(st => st.SectionId)
            .ToListAsync();

        if (!assignedSectionIds.Any())
        {
            // Teacher has no section assignments - return empty list
            return ApiResponse<List<TeacherScheduleDto>>.SuccessResponse(new List<TeacherScheduleDto>());
        }

        // Get all schedules for the assigned sections with related data
        var schedules = await _context.Schedules
            .AsNoTracking()
            .Include(s => s.Section)
                .ThenInclude(sec => sec!.Classroom)
            .Include(s => s.Subject)
            .Where(s => assignedSectionIds.Contains(s.SectionId))
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

        // Verify teacher is assigned to the schedule's section
        var isAssigned = await _context.SectionTeachers
            .AsNoTracking()
            .AnyAsync(st => st.TeacherId == teacher.Id && st.SectionId == schedule.SectionId);

        if (!isAssigned)
        {
            return ApiResponse<ScheduleHistoryDto>.ErrorResponse(
                "FORBIDDEN",
                "You are not assigned to this section.");
        }

        // Use provided date or default to today
        var filterDate = date ?? DateOnly.FromDateTime(DateTime.Today);

        // Get attendance records for the schedule and date
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

        // Build ScheduleInfoDto
        var scheduleInfo = new ScheduleInfoDto
        {
            SubjectName = schedule.Subject?.Name ?? string.Empty,
            Section = schedule.Section?.Name ?? string.Empty,
            Classroom = schedule.Section?.Classroom?.Name ?? string.Empty,
            Day = GetDayName(schedule.DayOfWeek),
            StartTime = schedule.StartTime.ToString("HH:mm"),
            EndTime = schedule.EndTime.ToString("HH:mm")
        };

        // Calculate late threshold (15 minutes after start time)
        var lateThreshold = schedule.StartTime.AddMinutes(15);

        // Map attendance records to AttendanceDto
        var records = attendances.Select(a => new AttendanceDto
        {
            Id = a.Id,
            ScheduleId = a.ScheduleId,
            SubjectName = schedule.Subject?.Name,
            StudentId = a.StudentId,
            StudentName = a.Student != null ? $"{a.Student.FirstName} {a.Student.LastName}" : null,
            SectionId = a.SectionId,
            SectionName = schedule.Section?.Name,
            Date = a.Date,
            TimeIn = a.TimeIn,
            TimeOut = a.TimeOut,
            Remarks = a.Remarks,
            MarkedAt = a.MarkedAt,
            MarkedBy = a.MarkedBy
        }).ToList();

        // Add absent students (students without attendance records)
        var attendedStudentIds = attendances.Select(a => a.StudentId).ToHashSet();
        var absentStudents = sectionStudents.Where(s => !attendedStudentIds.Contains(s.Id));

        foreach (var student in absentStudents)
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
                Remarks = "Absent",
                MarkedAt = DateTimeOffset.UtcNow,
                MarkedBy = 0
            });
        }

        // Calculate summary statistics
        var presentCount = attendances.Count(a => a.TimeIn.HasValue);
        var lateCount = attendances.Count(a => a.TimeIn.HasValue && a.TimeIn.Value > lateThreshold);
        var absentCount = sectionStudents.Count - presentCount;

        var summary = new AttendanceSummaryDto
        {
            TotalStudents = sectionStudents.Count,
            PresentCount = presentCount,
            AbsentCount = absentCount,
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