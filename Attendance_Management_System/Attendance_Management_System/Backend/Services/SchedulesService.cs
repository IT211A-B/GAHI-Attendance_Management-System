using Attendance_Management_System.Backend.Constants;
using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Attendance_Management_System.Backend.Services;

// Service for schedule management operations
public class SchedulesService : ISchedulesService
{
    private readonly AppDbContext _context;
    private readonly IConflictService _conflictService;

    // School hours for available slots calculation
    private static readonly TimeOnly SchoolStart = new(7, 0);  // 7:00 AM
    private static readonly TimeOnly SchoolEnd = new(18, 0);   // 6:00 PM

    // Day names for display
    private static readonly string[] DayNames = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };

    // Inject dependencies through constructor
    public SchedulesService(AppDbContext context, IConflictService conflictService)
    {
        _context = context;
        _conflictService = conflictService;
    }

    // Get all schedules (teacher sees their assigned sections, admin sees all)
    public async Task<ApiResponse<List<ScheduleDto>>> GetSchedulesAsync(int userId, string role)
    {
        List<Schedule> schedules;

        if (role == "admin")
        {
            // Admin sees all schedules
            schedules = await _context.Schedules
                .Include(s => s.Section)
                    .ThenInclude(sec => sec!.Classroom)
                .Include(s => s.Teacher)
                .Include(s => s.Subject)
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .ToListAsync();
        }
        else
        {
            // Teacher sees schedules in their assigned sections
            var teacherId = await GetTeacherIdByUserIdAsync(userId);
            if (teacherId == null)
            {
                return ApiResponse<List<ScheduleDto>>.ErrorResponse(ErrorCodes.NotFound, "Teacher profile not found.");
            }

            schedules = await _context.Schedules
                .Include(s => s.Section)
                    .ThenInclude(sec => sec!.Classroom)
                .Include(s => s.Teacher)
                .Include(s => s.Subject)
                .Join(_context.SectionTeachers,
                    s => s.SectionId,
                    st => st.SectionId,
                    (s, st) => new { Schedule = s, SectionTeacher = st })
                .Where(x => x.SectionTeacher.TeacherId == teacherId.Value)
                .Select(x => x.Schedule)
                .OrderBy(s => s.DayOfWeek)
                .ThenBy(s => s.StartTime)
                .ToListAsync();
        }

        var dtos = await BuildScheduleDtosAsync(schedules, userId);
        return ApiResponse<List<ScheduleDto>>.SuccessResponse(dtos);
    }

    // Get a single schedule by ID
    public async Task<ApiResponse<ScheduleDto>> GetScheduleByIdAsync(int id, int userId)
    {
        var schedule = await _context.Schedules
            .Include(s => s.Section)
                .ThenInclude(sec => sec!.Classroom)
            .Include(s => s.Teacher)
            .Include(s => s.Subject)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (schedule == null)
        {
            return ApiResponse<ScheduleDto>.ErrorResponse(ErrorCodes.NotFound, "Schedule not found.");
        }

        var dto = await BuildScheduleDtoAsync(schedule, userId);
        return ApiResponse<ScheduleDto>.SuccessResponse(dto);
    }

    // Create a new schedule slot with conflict validation
    public async Task<ApiResponse<ScheduleDto>> CreateScheduleAsync(CreateScheduleRequest request, int userId)
    {
        // Validate time range
        if (request.EndTime <= request.StartTime)
        {
            return ApiResponse<ScheduleDto>.ErrorResponse(ErrorCodes.ValidationError, "End time must be after start time.");
        }

        // Get teacher ID and validate teacher is assigned to section
        var teacherId = await GetTeacherIdByUserIdAsync(userId);
        if (teacherId == null)
        {
            return ApiResponse<ScheduleDto>.ErrorResponse(ErrorCodes.NotFound, "Teacher profile not found.");
        }

        // Validate section exists and get classroom
        var section = await _context.Sections
            .Include(s => s.Classroom)
            .FirstOrDefaultAsync(s => s.Id == request.SectionId);

        if (section == null)
        {
            return ApiResponse<ScheduleDto>.ErrorResponse(ErrorCodes.NotFound, "Section not found.");
        }

        // Validate subject exists
        var subject = await _context.Subjects.FindAsync(request.SubjectId);
        if (subject == null)
        {
            return ApiResponse<ScheduleDto>.ErrorResponse(ErrorCodes.NotFound, "Subject not found.");
        }

        // Run conflict checks
        var conflictResult = await _conflictService.CheckConflictsAsync(
            request.SectionId,
            section.ClassroomId,
            teacherId.Value,
            request.DayOfWeek,
            request.StartTime,
            request.EndTime);

        if (conflictResult.HasConflict)
        {
            var errorDetails = _conflictService.BuildConflictDetail(conflictResult);
            return ApiResponse<ScheduleDto>.ErrorResponse(
                conflictResult.ConflictType ?? ErrorCodes.Conflict,
                errorDetails.Message,
                errorDetails);
        }

        // Auto-assign teacher to section if not already assigned
        await EnsureTeacherAssignedToSectionAsync(teacherId.Value, request.SectionId);

        // Create the schedule
        var schedule = new Schedule
        {
            SectionId = request.SectionId,
            TeacherId = teacherId.Value,
            SubjectId = request.SubjectId,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo
        };

        _context.Schedules.Add(schedule);
        await _context.SaveChangesAsync();

        var dto = await BuildScheduleDtoAsync(schedule, userId);
        return ApiResponse<ScheduleDto>.SuccessResponse(dto);
    }

    // Create multiple schedule slots for one shared time range across weekdays
    public async Task<ApiResponse<List<ScheduleDto>>> CreateScheduleRangeAsync(CreateScheduleRangeRequest request, int userId)
    {
        // Validate time range
        if (request.EndTime <= request.StartTime)
        {
            return ApiResponse<List<ScheduleDto>>.ErrorResponse(ErrorCodes.ValidationError, "End time must be after start time.");
        }

        var targetDays = request.DaysOfWeek
            .Where(day => day >= 0 && day <= 6)
            .Distinct()
            .OrderBy(day => day)
            .ToList();

        if (targetDays.Count == 0)
        {
            return ApiResponse<List<ScheduleDto>>.ErrorResponse(ErrorCodes.ValidationError, "Select at least one valid day.");
        }

        // Get teacher profile for ownership and conflict checks
        var teacherId = await GetTeacherIdByUserIdAsync(userId);
        if (teacherId == null)
        {
            return ApiResponse<List<ScheduleDto>>.ErrorResponse(ErrorCodes.NotFound, "Teacher profile not found.");
        }

        // Validate section and classroom
        var section = await _context.Sections
            .Include(s => s.Classroom)
            .FirstOrDefaultAsync(s => s.Id == request.SectionId);

        if (section == null)
        {
            return ApiResponse<List<ScheduleDto>>.ErrorResponse(ErrorCodes.NotFound, "Section not found.");
        }

        // Validate subject
        var subject = await _context.Subjects.FindAsync(request.SubjectId);
        if (subject == null)
        {
            return ApiResponse<List<ScheduleDto>>.ErrorResponse(ErrorCodes.NotFound, "Subject not found.");
        }

        // Pre-validate all requested days to avoid partial inserts
        foreach (var dayOfWeek in targetDays)
        {
            var conflictResult = await _conflictService.CheckConflictsAsync(
                request.SectionId,
                section.ClassroomId,
                teacherId.Value,
                dayOfWeek,
                request.StartTime,
                request.EndTime);

            if (conflictResult.HasConflict)
            {
                var errorDetails = _conflictService.BuildConflictDetail(conflictResult);
                var dayName = DayNames[dayOfWeek];
                return ApiResponse<List<ScheduleDto>>.ErrorResponse(
                    conflictResult.ConflictType ?? ErrorCodes.Conflict,
                    $"{dayName}: {errorDetails.Message}",
                    errorDetails);
            }
        }

        await using var transaction = await _context.Database.BeginTransactionAsync();

        // Keep self-assignment behavior for teacher-created schedules
        await EnsureTeacherAssignedToSectionAsync(teacherId.Value, request.SectionId);

        var createdSchedules = targetDays
            .Select(dayOfWeek => new Schedule
            {
                SectionId = request.SectionId,
                TeacherId = teacherId.Value,
                SubjectId = request.SubjectId,
                DayOfWeek = dayOfWeek,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                EffectiveFrom = request.EffectiveFrom,
                EffectiveTo = request.EffectiveTo
            })
            .ToList();

        _context.Schedules.AddRange(createdSchedules);
        await _context.SaveChangesAsync();
        await transaction.CommitAsync();

        var dtos = await BuildScheduleDtosAsync(createdSchedules, userId);
        return ApiResponse<List<ScheduleDto>>.SuccessResponse(dtos);
    }

    // Update an existing schedule slot with re-validation
    public async Task<ApiResponse<ScheduleDto>> UpdateScheduleAsync(int id, UpdateScheduleRequest request, int userId)
    {
        var schedule = await _context.Schedules
            .Include(s => s.Section)
                .ThenInclude(sec => sec!.Classroom)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (schedule == null)
        {
            return ApiResponse<ScheduleDto>.ErrorResponse(ErrorCodes.NotFound, "Schedule not found.");
        }

        // Get teacher ID and validate ownership
        var teacherId = await GetTeacherIdByUserIdAsync(userId);
        if (teacherId == null)
        {
            return ApiResponse<ScheduleDto>.ErrorResponse(ErrorCodes.NotFound, "Teacher profile not found.");
        }

        var isOwner = schedule.TeacherId.HasValue && schedule.TeacherId.Value == teacherId.Value;
        var canClaimUnowned = !schedule.TeacherId.HasValue
            && await _context.SectionTeachers
                .AnyAsync(st => st.SectionId == schedule.SectionId && st.TeacherId == teacherId.Value);

        if (!isOwner && !canClaimUnowned)
        {
            return ApiResponse<ScheduleDto>.ErrorResponse(ErrorCodes.Forbidden, "You can only update your own schedule slots.");
        }

        if (canClaimUnowned)
        {
            schedule.TeacherId = teacherId.Value;
        }

        // Calculate new values (use existing if not provided)
        var newDayOfWeek = request.DayOfWeek ?? schedule.DayOfWeek;
        var newStartTime = request.StartTime ?? schedule.StartTime;
        var newEndTime = request.EndTime ?? schedule.EndTime;
        var newSubjectId = request.SubjectId ?? schedule.SubjectId;

        // Validate time range
        if (newEndTime <= newStartTime)
        {
            return ApiResponse<ScheduleDto>.ErrorResponse(ErrorCodes.ValidationError, "End time must be after start time.");
        }

        // Validate subject if being changed
        Subject? subject = null;
        if (request.SubjectId.HasValue)
        {
            subject = await _context.Subjects.FindAsync(request.SubjectId.Value);
            if (subject == null)
            {
                return ApiResponse<ScheduleDto>.ErrorResponse(ErrorCodes.NotFound, "Subject not found.");
            }
        }

        // Run conflict checks with self-exclusion if time-related fields changed
        if (request.DayOfWeek.HasValue || request.StartTime.HasValue || request.EndTime.HasValue)
        {
            var classroomId = schedule.Section?.ClassroomId ?? 0;
            var conflictResult = await _conflictService.CheckConflictsAsync(
                schedule.SectionId,
                classroomId,
                teacherId.Value,
                newDayOfWeek,
                newStartTime,
                newEndTime,
                id); // Exclude current schedule

            if (conflictResult.HasConflict)
            {
                var errorDetails = _conflictService.BuildConflictDetail(conflictResult);
                return ApiResponse<ScheduleDto>.ErrorResponse(
                    conflictResult.ConflictType ?? ErrorCodes.Conflict,
                    errorDetails.Message,
                    errorDetails);
            }
        }

        // Apply updates
        if (request.SubjectId.HasValue)
            schedule.SubjectId = request.SubjectId.Value;
        if (request.DayOfWeek.HasValue)
            schedule.DayOfWeek = request.DayOfWeek.Value;
        if (request.StartTime.HasValue)
            schedule.StartTime = request.StartTime.Value;
        if (request.EndTime.HasValue)
            schedule.EndTime = request.EndTime.Value;
        if (request.EffectiveFrom.HasValue)
            schedule.EffectiveFrom = request.EffectiveFrom.Value;
        if (request.EffectiveTo.HasValue)
            schedule.EffectiveTo = request.EffectiveTo.Value;

        await _context.SaveChangesAsync();

        var dto = await BuildScheduleDtoAsync(schedule, userId);
        return ApiResponse<ScheduleDto>.SuccessResponse(dto);
    }

    // Delete a schedule slot (owner or admin only)
    public async Task<ApiResponse<bool>> DeleteScheduleAsync(int id, int userId, bool isAdmin)
    {
        var schedule = await _context.Schedules
            .Include(s => s.Section)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (schedule == null)
        {
            return ApiResponse<bool>.ErrorResponse(ErrorCodes.NotFound, "Schedule not found.");
        }

        // Admin can delete any schedule
        if (!isAdmin)
        {
            var teacherId = await GetTeacherIdByUserIdAsync(userId);
            if (teacherId == null)
            {
                return ApiResponse<bool>.ErrorResponse(ErrorCodes.NotFound, "Teacher profile not found.");
            }

            var isOwner = schedule.TeacherId.HasValue && schedule.TeacherId.Value == teacherId.Value;
            var canManageUnowned = !schedule.TeacherId.HasValue
                && await _context.SectionTeachers
                    .AnyAsync(st => st.SectionId == schedule.SectionId && st.TeacherId == teacherId.Value);

            if (!isOwner && !canManageUnowned)
            {
                return ApiResponse<bool>.ErrorResponse(ErrorCodes.Forbidden, "You can only delete your own schedule slots.");
            }
        }

        // Check if attendance records exist for this schedule
        var hasAttendance = await _context.Attendances.AnyAsync(a => a.ScheduleId == id);
        if (hasAttendance)
        {
            return ApiResponse<bool>.ErrorResponse(ErrorCodes.Conflict, "Cannot delete schedule with existing attendance records.");
        }

        _context.Schedules.Remove(schedule);
        await _context.SaveChangesAsync();

        return ApiResponse<bool>.SuccessResponse(true);
    }

    // Get available time slots for a classroom on a specific day
    public async Task<ApiResponse<List<AvailableSlotDto>>> GetAvailableSlotsAsync(int classroomId, int dayOfWeek)
    {
        // Validate classroom exists
        var classroom = await _context.Classrooms.FindAsync(classroomId);
        if (classroom == null)
        {
            return ApiResponse<List<AvailableSlotDto>>.ErrorResponse(ErrorCodes.NotFound, "Classroom not found.");
        }

        // Validate day of week
        if (dayOfWeek < 0 || dayOfWeek > 6)
        {
            return ApiResponse<List<AvailableSlotDto>>.ErrorResponse(ErrorCodes.ValidationError, "Day of week must be between 0 (Sunday) and 6 (Saturday).");
        }

        // Get all schedules for this classroom on this day, sorted by start time
        var schedules = await _context.Schedules
            .Include(s => s.Section)
            .Where(s => s.Section != null && s.Section.ClassroomId == classroomId)
            .Where(s => s.DayOfWeek == dayOfWeek)
            .OrderBy(s => s.StartTime)
            .ToListAsync();

        // Calculate available slots (gaps between schedules)
        var availableSlots = CalculateAvailableSlots(schedules);

        var dto = new AvailableSlotDto
        {
            ClassroomId = classroomId,
            ClassroomName = classroom.Name,
            DayOfWeek = dayOfWeek,
            DayName = DayNames[dayOfWeek],
            AvailableSlots = availableSlots
        };

        return ApiResponse<List<AvailableSlotDto>>.SuccessResponse(new List<AvailableSlotDto> { dto });
    }

    // Helper: Calculate available time slots between schedules
    private List<TimeSlotRange> CalculateAvailableSlots(List<Schedule> schedules)
    {
        var availableSlots = new List<TimeSlotRange>();

        if (schedules.Count == 0)
        {
            // No schedules, entire school day is available
            availableSlots.Add(new TimeSlotRange
            {
                StartTime = SchoolStart.ToString("HH:mm"),
                EndTime = SchoolEnd.ToString("HH:mm")
            });
            return availableSlots;
        }

        // Check for gap before first schedule
        var firstSchedule = schedules.First();
        if (firstSchedule.StartTime > SchoolStart)
        {
            availableSlots.Add(new TimeSlotRange
            {
                StartTime = SchoolStart.ToString("HH:mm"),
                EndTime = firstSchedule.StartTime.ToString("HH:mm")
            });
        }

        // Check for gaps between consecutive schedules
        for (int i = 0; i < schedules.Count - 1; i++)
        {
            var current = schedules[i];
            var next = schedules[i + 1];

            if (current.EndTime < next.StartTime)
            {
                availableSlots.Add(new TimeSlotRange
                {
                    StartTime = current.EndTime.ToString("HH:mm"),
                    EndTime = next.StartTime.ToString("HH:mm")
                });
            }
        }

        // Check for gap after last schedule
        var lastSchedule = schedules.Last();
        if (lastSchedule.EndTime < SchoolEnd)
        {
            availableSlots.Add(new TimeSlotRange
            {
                StartTime = lastSchedule.EndTime.ToString("HH:mm"),
                EndTime = SchoolEnd.ToString("HH:mm")
            });
        }

        return availableSlots;
    }

    // Helper: Get teacher ID from user ID
    private async Task<int?> GetTeacherIdByUserIdAsync(int userId)
    {
        return await _context.Teachers
            .Where(t => t.UserId == userId)
            .Select(t => (int?)t.Id)
            .FirstOrDefaultAsync();
    }

    // Helper: Ensure teacher is assigned to section (auto-assign if not)
    // This enables teachers to self-assign to sections when creating schedules
    private async Task EnsureTeacherAssignedToSectionAsync(int teacherId, int sectionId)
    {
        var isAssigned = await _context.SectionTeachers
            .AnyAsync(st => st.SectionId == sectionId && st.TeacherId == teacherId);

        if (!isAssigned)
        {
            var sectionTeacher = new SectionTeacher
            {
                SectionId = sectionId,
                TeacherId = teacherId,
                AssignedAt = DateTimeOffset.UtcNow
            };

            _context.SectionTeachers.Add(sectionTeacher);
        }
    }

    // Helper: Build a single schedule DTO
    private async Task<ScheduleDto> BuildScheduleDtoAsync(Schedule schedule, int userId)
    {
        var teacherId = await GetTeacherIdByUserIdAsync(userId);
        var section = schedule.Section ?? await _context.Sections
            .Include(s => s.Classroom)
            .FirstOrDefaultAsync(s => s.Id == schedule.SectionId);
        var subject = schedule.Subject ?? await _context.Subjects.FindAsync(schedule.SubjectId);

        // Get teachers assigned to this section
        var teachers = await _context.SectionTeachers
            .Include(st => st.Teacher)
            .Where(st => st.SectionId == schedule.SectionId)
            .Select(st => st.Teacher)
            .Where(t => t != null)
            .ToListAsync();

        // Check if current teacher owns this schedule slot
        var isMine = teacherId.HasValue
            && schedule.TeacherId.HasValue
            && schedule.TeacherId.Value == teacherId.Value;

        return new ScheduleDto
        {
            Id = schedule.Id,
            SectionId = schedule.SectionId,
            SectionName = section?.Name ?? string.Empty,
            SubjectId = schedule.SubjectId,
            SubjectName = subject?.Name ?? string.Empty,
            ClassroomId = section?.ClassroomId ?? 0,
            ClassroomName = section?.Classroom?.Name ?? string.Empty,
            DayOfWeek = schedule.DayOfWeek,
            DayName = DayNames[schedule.DayOfWeek],
            StartTime = schedule.StartTime.ToString("HH:mm"),
            EndTime = schedule.EndTime.ToString("HH:mm"),
            EffectiveFrom = schedule.EffectiveFrom,
            EffectiveTo = schedule.EffectiveTo,
            CreatedAt = schedule.CreatedAt,
            IsMine = isMine,
            Teachers = teachers.Select(t => new TeacherInfo
            {
                Id = t!.Id,
                FirstName = t.FirstName,
                LastName = t.LastName,
                Department = t.Department
            }).ToList()
        };
    }

    // Helper: Build multiple schedule DTOs
    private async Task<List<ScheduleDto>> BuildScheduleDtosAsync(List<Schedule> schedules, int userId)
    {
        var dtos = new List<ScheduleDto>();
        foreach (var schedule in schedules)
        {
            dtos.Add(await BuildScheduleDtoAsync(schedule, userId));
        }
        return dtos;
    }
}