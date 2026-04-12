using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Entities;

namespace Attendance_Management_System.Backend.Interfaces.Services;

// Result of conflict detection checks
public class ConflictResult
{
    // True if no conflicts found
    public bool HasConflict { get; set; }

    // The type of conflict if found
    public string? ConflictType { get; set; }

    // The conflicting schedule if found
    public Schedule? ConflictingSchedule { get; set; }

    // Additional info for building conflict details
    public string? SubjectName { get; set; }
    public string? TeacherName { get; set; }
    public string? SectionName { get; set; }
    public string? ClassroomName { get; set; }
}

// Interface for schedule conflict detection
public interface IConflictService
{
    // Run all 3 conflict checks atomically (Section, Classroom, Teacher)
    Task<ConflictResult> CheckConflictsAsync(
        int sectionId,
        int classroomId,
        int teacherId,
        int dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        int excludeScheduleId = 0);

    // Check if section already has a schedule at this time
    Task<Schedule?> CheckSectionSlotConflictAsync(
        int sectionId,
        int dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        int excludeId = 0);

    // Check if classroom is already booked at this time
    Task<Schedule?> CheckClassroomConflictAsync(
        int classroomId,
        int dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        int excludeId = 0);

    // Check if teacher has overlapping schedule in another section
    Task<Schedule?> CheckTeacherConflictAsync(
        int teacherId,
        int currentSectionId,
        int dayOfWeek,
        TimeOnly startTime,
        TimeOnly endTime,
        int excludeId = 0);

    // Build conflict detail DTO from a conflict result
    ConflictDetailDto BuildConflictDetail(ConflictResult result);
}