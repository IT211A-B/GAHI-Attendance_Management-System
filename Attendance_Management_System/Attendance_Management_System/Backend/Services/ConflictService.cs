using Attendance_Management_System.Backend.Constants;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Attendance_Management_System.Backend.Services;

// Service for detecting schedule conflicts (Section, Classroom, Teacher)
public class ConflictService : IConflictService
{
    private readonly AppDbContext _context;

    // Inject database context through constructor
    public ConflictService(AppDbContext context)
    {
        _context = context;
    }

    // Run all 3 conflict checks atomically in priority order: Section → Classroom → Teacher
    public async Task<ConflictResult> CheckConflictsAsync(
        int sectionId,
        int classroomId,
        int teacherId,
        int dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        int excludeScheduleId = 0)
    {
        // Check 1: Section slot conflict (same section, overlapping time)
        var sectionConflict = await CheckSectionSlotConflictAsync(sectionId, dayOfWeek, startTime, endTime, excludeScheduleId);
        if (sectionConflict != null)
        {
            return await BuildConflictResultAsync(sectionConflict, ErrorCodes.ConflictSectionSlot, sectionId);
        }

        // Check 2: Classroom conflict (same classroom, overlapping time)
        var classroomConflict = await CheckClassroomConflictAsync(classroomId, dayOfWeek, startTime, endTime, excludeScheduleId);
        if (classroomConflict != null)
        {
            return await BuildConflictResultAsync(classroomConflict, ErrorCodes.ConflictClassroom, sectionId);
        }

        // Check 3: Teacher conflict (same teacher, different section, overlapping time)
        var teacherConflict = await CheckTeacherConflictAsync(teacherId, sectionId, dayOfWeek, startTime, endTime, excludeScheduleId);
        if (teacherConflict != null)
        {
            return await BuildConflictResultAsync(teacherConflict, ErrorCodes.ConflictTeacher, sectionId);
        }

        // No conflicts found
        return new ConflictResult { HasConflict = false };
    }

    // Check if section already has a schedule at this time
    public async Task<Schedule?> CheckSectionSlotConflictAsync(
        int sectionId,
        int dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        int excludeId = 0)
    {
        // Time overlap logic: existingStart < newEnd AND existingEnd > newStart
        return await _context.Schedules
            .Where(s => s.SectionId == sectionId)
            .Where(s => s.DayOfWeek == dayOfWeek)
            .Where(s => s.StartTime < endTime)
            .Where(s => s.EndTime > startTime)
            .Where(s => s.Id != excludeId)
            .FirstOrDefaultAsync();
    }

    // Check if classroom is already booked at this time
    public async Task<Schedule?> CheckClassroomConflictAsync(
        int classroomId,
        int dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        int excludeId = 0)
    {
        // Join schedules with sections to get classroom, then check overlap
        return await _context.Schedules
            .Include(s => s.Section)
            .Where(s => s.Section != null && s.Section.ClassroomId == classroomId)
            .Where(s => s.DayOfWeek == dayOfWeek)
            .Where(s => s.StartTime < endTime)
            .Where(s => s.EndTime > startTime)
            .Where(s => s.Id != excludeId)
            .FirstOrDefaultAsync();
    }

    // Check if teacher has overlapping schedule in another section
    public async Task<Schedule?> CheckTeacherConflictAsync(
        int teacherId,
        int currentSectionId,
        int dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        int excludeId = 0)
    {
        // Find overlapping schedules owned by the same teacher, excluding the current section
        return await _context.Schedules
            .Include(s => s.Section)
            .Where(s => s.TeacherId == teacherId)
            .Where(s => s.SectionId != currentSectionId)
            .Where(s => s.DayOfWeek == dayOfWeek)
            .Where(s => s.StartTime < endTime)
            .Where(s => s.EndTime > startTime)
            .Where(s => s.Id != excludeId)
            .FirstOrDefaultAsync();
    }

    // Build conflict detail DTO from a conflict result
    public ConflictDetailDto BuildConflictDetail(ConflictResult result)
    {
        var resolvedTeacherName = string.IsNullOrWhiteSpace(result.TeacherName)
            ? "Selected teacher"
            : result.TeacherName;

        var message = result.ConflictType switch
        {
            ErrorCodes.ConflictSectionSlot => $"This section already has a schedule at {result.ConflictingSchedule?.StartTime.ToString("HH:mm")} - {result.ConflictingSchedule?.EndTime.ToString("HH:mm")}",
            ErrorCodes.ConflictClassroom => $"{result.ClassroomName} is already booked at this time",
            ErrorCodes.ConflictTeacher => $"{resolvedTeacherName} has an overlapping schedule in another section",
            _ => "Schedule conflict detected"
        };

        return new ConflictDetailDto
        {
            ConflictType = result.ConflictType ?? ErrorCodes.Conflict,
            Message = message,
            Info = result.ConflictingSchedule != null ? new ConflictInfo
            {
                SubjectName = result.SubjectName,
                TeacherName = result.TeacherName,
                SectionName = result.SectionName,
                ClassroomName = result.ClassroomName,
                StartTime = result.ConflictingSchedule.StartTime.ToString("HH:mm"),
                EndTime = result.ConflictingSchedule.EndTime.ToString("HH:mm")
            } : null
        };
    }

    // Helper to build conflict result with additional info
    private async Task<ConflictResult> BuildConflictResultAsync(Schedule schedule, string conflictType, int sectionId)
    {
        // Get subject name
        var subject = await _context.Subjects.FindAsync(schedule.SubjectId);
        var subjectName = subject?.Name;

        // Get section and classroom info
        var section = await _context.Sections
            .Include(s => s.Classroom)
            .FirstOrDefaultAsync(s => s.Id == schedule.SectionId);
        var sectionName = section?.Name;
        var classroomName = section?.Classroom?.Name;

        var teacherName = string.Empty;
        if (schedule.TeacherId.HasValue)
        {
            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.Id == schedule.TeacherId.Value);
            if (teacher != null)
            {
                teacherName = $"{teacher.FirstName} {teacher.LastName}".Trim();
            }
        }

        return new ConflictResult
        {
            HasConflict = true,
            ConflictType = conflictType,
            ConflictingSchedule = schedule,
            SubjectName = subjectName,
            TeacherName = teacherName,
            SectionName = sectionName,
            ClassroomName = classroomName
        };
    }
}