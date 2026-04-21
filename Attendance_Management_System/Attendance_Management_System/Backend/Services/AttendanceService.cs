using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Configuration;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Enums;
using Attendance_Management_System.Backend.Helpers;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Attendance_Management_System.Backend.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Attendance_Management_System.Backend.Services;

// Service implementation for managing student attendance
public class AttendanceService : IAttendanceService
{
    private readonly AppDbContext _context;
    private readonly AttendanceSettings _attendanceSettings;

    public AttendanceService(AppDbContext context, IOptions<AttendanceSettings> attendanceSettings)
    {
        _context = context;
        // Fall back to defaults when config is missing so attendance checks still work.
        _attendanceSettings = attendanceSettings.Value?.IsValid() == true
            ? attendanceSettings.Value
            : AttendanceSettings.Default;
    }

    public async Task<ApiResponse<AttendanceDto>> MarkAttendanceAsync(MarkAttendanceRequest request, TeacherContext teacherContext)
    {
        // Validate ownership, weekday, and allowed marking window before any mutation.
        var scheduleValidation = await ValidateScheduleForMarkingAsync(
            request.SectionId,
            request.ScheduleId,
            request.Date,
            teacherContext);
        if (!scheduleValidation.Success || scheduleValidation.Schedule is null)
        {
            return ApiResponse<AttendanceDto>.ErrorResponse(
                scheduleValidation.ErrorCode!,
                scheduleValidation.ErrorMessage!);
        }

        var schedule = scheduleValidation.Schedule;

        var student = await _context.Students
            .FirstOrDefaultAsync(s =>
                s.Id == request.StudentId
                && s.SectionId == request.SectionId
                && s.IsActive);

        if (student == null)
        {
            return ApiResponse<AttendanceDto>.ErrorResponse(
                "NOT_FOUND",
                "Active student not found in this section.");
        }

        var existingAttendance = await _context.Attendances
            .FirstOrDefaultAsync(a => a.ScheduleId == request.ScheduleId
                && a.StudentId == request.StudentId
                && a.Date == request.Date);

        var academicYear = await GetActiveAcademicYearAsync();
        if (academicYear == null)
        {
            return ApiResponse<AttendanceDto>.ErrorResponse(
                "NOT_FOUND",
                "No active academic year found.");
        }

        var section = await _context.Sections.FindAsync(request.SectionId);
        var markerName = await GetMarkerNameAsync(teacherContext.UserId);
        var now = DateTimeOffset.UtcNow;
        var normalizedRemarks = NormalizeRemarks(request.TimeIn, request.Remarks);
        var afterStatus = AttendancePolicy.GetMarkedStatus(request.TimeIn, schedule.StartTime, _attendanceSettings);

        Attendance attendance;
        AttendanceAudit audit;

        if (existingAttendance == null)
        {
            // First mark for this student/date/schedule creates a brand-new attendance row.
            attendance = new Attendance
            {
                ScheduleId = request.ScheduleId,
                StudentId = request.StudentId,
                SectionId = request.SectionId,
                AcademicYearId = academicYear.Id,
                Date = request.Date,
                TimeIn = request.TimeIn,
                Remarks = normalizedRemarks,
                MarkedBy = teacherContext.GetMarkerId(),
                MarkedAt = now
            };

            _context.Attendances.Add(attendance);
            // Save first so the generated AttendanceId can be referenced by the audit row.
            await _context.SaveChangesAsync();

            audit = BuildAttendanceAudit(
                attendance,
                "created",
                beforeTimeIn: null,
                beforeRemarks: null,
                beforeStatus: null,
                afterTimeIn: attendance.TimeIn,
                afterRemarks: attendance.Remarks,
                afterStatus: afterStatus,
                actorUserId: teacherContext.GetMarkerId(),
                actionAt: now);
        }
        else
        {
            // Re-marking updates the existing row while preserving an audit trail of before/after values.
            var beforeTimeIn = existingAttendance.TimeIn;
            var beforeRemarks = existingAttendance.Remarks;
            var beforeStatus = AttendancePolicy.GetMarkedStatus(beforeTimeIn, schedule.StartTime, _attendanceSettings);

            existingAttendance.TimeIn = request.TimeIn;
            existingAttendance.Remarks = normalizedRemarks;
            existingAttendance.MarkedBy = teacherContext.GetMarkerId();
            existingAttendance.MarkedAt = now;

            await _context.SaveChangesAsync();

            attendance = existingAttendance;
            audit = BuildAttendanceAudit(
                attendance,
                "updated",
                beforeTimeIn: beforeTimeIn,
                beforeRemarks: beforeRemarks,
                beforeStatus: beforeStatus,
                afterTimeIn: attendance.TimeIn,
                afterRemarks: attendance.Remarks,
                afterStatus: afterStatus,
                actorUserId: teacherContext.GetMarkerId(),
                actionAt: now);
        }

        // Audit is persisted separately to keep the main attendance write path straightforward.
        _context.AttendanceAudits.Add(audit);
        await _context.SaveChangesAsync();

        var attendanceDto = BuildAttendanceDto(
            attendance,
            schedule,
            student,
            section?.Name,
            markerName,
            afterStatus,
            isMarked: true);

        return ApiResponse<AttendanceDto>.SuccessResponse(attendanceDto);
    }

    public async Task<ApiResponse<List<AttendanceDto>>> MarkBulkAttendanceAsync(BulkAttendanceRequest request, TeacherContext teacherContext)
    {
        // Reuse single-mark validation so bulk and manual flows enforce identical rules.
        var scheduleValidation = await ValidateScheduleForMarkingAsync(
            request.SectionId,
            request.ScheduleId,
            request.Date,
            teacherContext);
        if (!scheduleValidation.Success || scheduleValidation.Schedule is null)
        {
            return ApiResponse<List<AttendanceDto>>.ErrorResponse(
                scheduleValidation.ErrorCode!,
                scheduleValidation.ErrorMessage!);
        }

        var schedule = scheduleValidation.Schedule;

        var academicYear = await GetActiveAcademicYearAsync();
        if (academicYear == null)
        {
            return ApiResponse<List<AttendanceDto>>.ErrorResponse(
                "NOT_FOUND",
                "No active academic year found.");
        }

        var section = await _context.Sections.FindAsync(request.SectionId);
        var markerName = await GetMarkerNameAsync(teacherContext.UserId);
        var now = DateTimeOffset.UtcNow;

        var normalizedEntries = request.Entries
            .GroupBy(entry => entry.StudentId)
            // Last entry wins when a client sends duplicate student rows in one payload.
            .Select(group => group.Last())
            .ToList();

        var studentIds = normalizedEntries.Select(entry => entry.StudentId).ToHashSet();
        var students = await _context.Students
            .Where(student => studentIds.Contains(student.Id)
                && student.SectionId == request.SectionId
                && student.IsActive)
            .ToDictionaryAsync(student => student.Id);

        var existingAttendances = await _context.Attendances
            .Where(attendance => attendance.ScheduleId == request.ScheduleId
                && attendance.Date == request.Date
                && studentIds.Contains(attendance.StudentId))
            .ToDictionaryAsync(attendance => attendance.StudentId);

        var processedRows = new List<BulkAttendanceMutation>();
        var errors = new List<string>();

        foreach (var entry in normalizedEntries)
        {
            // Skip invalid rows but continue processing valid ones for partial-success behavior.
            if (!students.TryGetValue(entry.StudentId, out var student))
            {
                errors.Add($"Student {entry.StudentId} is not an active member of this section.");
                continue;
            }

            existingAttendances.TryGetValue(entry.StudentId, out var existingAttendance);

            var normalizedRemarks = NormalizeRemarks(entry.TimeIn, entry.Remarks);
            var afterStatus = AttendancePolicy.GetMarkedStatus(entry.TimeIn, schedule.StartTime, _attendanceSettings);

            if (existingAttendance == null)
            {
                var attendance = new Attendance
                {
                    ScheduleId = request.ScheduleId,
                    StudentId = entry.StudentId,
                    SectionId = request.SectionId,
                    AcademicYearId = academicYear.Id,
                    Date = request.Date,
                    TimeIn = entry.TimeIn,
                    Remarks = normalizedRemarks,
                    MarkedBy = teacherContext.GetMarkerId(),
                    MarkedAt = now
                };

                _context.Attendances.Add(attendance);

                processedRows.Add(new BulkAttendanceMutation
                {
                    Attendance = attendance,
                    Student = student,
                    Action = "created",
                    BeforeTimeIn = null,
                    BeforeRemarks = null,
                    BeforeStatus = null,
                    AfterStatus = afterStatus
                });

                continue;
            }

            var beforeTimeIn = existingAttendance.TimeIn;
            var beforeRemarks = existingAttendance.Remarks;
            var beforeStatus = AttendancePolicy.GetMarkedStatus(beforeTimeIn, schedule.StartTime, _attendanceSettings);

            existingAttendance.TimeIn = entry.TimeIn;
            existingAttendance.Remarks = normalizedRemarks;
            existingAttendance.MarkedBy = teacherContext.GetMarkerId();
            existingAttendance.MarkedAt = now;

            processedRows.Add(new BulkAttendanceMutation
            {
                Attendance = existingAttendance,
                Student = student,
                Action = "updated",
                BeforeTimeIn = beforeTimeIn,
                BeforeRemarks = beforeRemarks,
                BeforeStatus = beforeStatus,
                AfterStatus = afterStatus
            });
        }

        if (!processedRows.Any())
        {
            var errorMessage = errors.Any()
                ? string.Join(" ", errors)
                : "No attendance entries were processed.";

            return ApiResponse<List<AttendanceDto>>.ErrorResponse("BULK_FAILED", errorMessage);
        }

        await _context.SaveChangesAsync();

        // Build one audit row per processed mutation to preserve detailed history.
        var audits = processedRows
            .Select(row => BuildAttendanceAudit(
                row.Attendance,
                row.Action,
                row.BeforeTimeIn,
                row.BeforeRemarks,
                row.BeforeStatus,
                row.Attendance.TimeIn,
                row.Attendance.Remarks,
                row.AfterStatus,
                teacherContext.GetMarkerId(),
                now))
            .ToList();

        _context.AttendanceAudits.AddRange(audits);
        await _context.SaveChangesAsync();

        var attendanceDtos = processedRows
            .Select(row => BuildAttendanceDto(
                row.Attendance,
                schedule,
                row.Student,
                section?.Name,
                markerName,
                row.AfterStatus,
                isMarked: true))
            .OrderBy(row => row.StudentName)
            .ToList();

        return ApiResponse<List<AttendanceDto>>.SuccessResponse(attendanceDtos);
    }

    public async Task<ApiResponse<AttendanceSummaryDto>> GetSectionAttendanceAsync(int sectionId, DateOnly date, int scheduleId)
    {
        var section = await _context.Sections
            .Include(s => s.Subject)
            .FirstOrDefaultAsync(s => s.Id == sectionId);

        if (section == null)
        {
            return ApiResponse<AttendanceSummaryDto>.ErrorResponse(
                "NOT_FOUND",
                "Section not found.");
        }

        var schedule = await _context.Schedules
            .Include(s => s.Subject)
            .FirstOrDefaultAsync(s => s.Id == scheduleId && s.SectionId == sectionId);

        if (schedule == null)
        {
            return ApiResponse<AttendanceSummaryDto>.ErrorResponse(
                "NOT_FOUND",
                "Schedule not found for this section.");
        }

        var students = await _context.Students
            .Where(student => student.SectionId == sectionId && student.IsActive)
            .ToListAsync();

        var attendances = await _context.Attendances
            .Include(a => a.Student)
            .Where(a => a.SectionId == sectionId && a.Date == date && a.ScheduleId == scheduleId)
            .ToListAsync();

        var markerUserIds = attendances
            .Where(attendance => attendance.MarkedBy > 0)
            .Select(attendance => attendance.MarkedBy)
            .Distinct()
            .ToHashSet();

        var teacherNames = await _context.Teachers
            .Where(teacher => markerUserIds.Contains(teacher.UserId))
            .ToDictionaryAsync(teacher => teacher.UserId, teacher => $"{teacher.FirstName} {teacher.LastName}");

        var markedRecords = new List<(AttendanceDto Record, AttendanceStatusKind Status)>();
        foreach (var attendance in attendances)
        {
            teacherNames.TryGetValue(attendance.MarkedBy, out var markerName);

            var status = AttendancePolicy.GetMarkedStatus(attendance.TimeIn, schedule.StartTime, _attendanceSettings);
            var record = BuildAttendanceDto(
                attendance,
                schedule,
                attendance.Student,
                section.Name,
                markerName,
                status,
                isMarked: true);

            markedRecords.Add((record, status));
        }

        var attendedStudentIds = attendances.Select(attendance => attendance.StudentId).ToHashSet();
        var unmarkedStudents = students.Where(student => !attendedStudentIds.Contains(student.Id));

        var records = markedRecords.Select(row => row.Record).ToList();
        foreach (var student in unmarkedStudents)
        {
            // Include explicit "Unmarked" rows so the UI can show every student in the class list.
            records.Add(new AttendanceDto
            {
                Id = 0,
                ScheduleId = scheduleId,
                SubjectName = schedule.Subject?.Name ?? section.Subject?.Name,
                StudentId = student.Id,
                StudentName = $"{student.FirstName} {student.LastName}",
                SectionId = sectionId,
                SectionName = section.Name,
                Date = date,
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

        var presentCount = markedRecords.Count(row => AttendancePolicy.CountsAsPresent(row.Status));
        var lateCount = markedRecords.Count(row => row.Status == AttendanceStatusKind.Late);
        var absentCount = markedRecords.Count(row => row.Status == AttendanceStatusKind.Absent);
        var unmarkedCount = students.Count - markedRecords.Count;

        var summary = new AttendanceSummaryDto
        {
            TotalStudents = students.Count,
            PresentCount = presentCount,
            AbsentCount = absentCount,
            UnmarkedCount = unmarkedCount,
            LateCount = lateCount,
            Records = records.OrderBy(record => record.StudentName).ToList()
        };

        return ApiResponse<AttendanceSummaryDto>.SuccessResponse(summary);
    }

    public async Task<ApiResponse<List<AttendanceDto>>> GetStudentAttendanceAsync(int studentId, int? sectionId, DateOnly? from, DateOnly? to)
    {
        var query = _context.Attendances
            .Include(attendance => attendance.Schedule)
                .ThenInclude(schedule => schedule!.Subject)
            .Include(attendance => attendance.Student)
            .Include(attendance => attendance.Section)
            .Where(attendance => attendance.StudentId == studentId);

        if (sectionId.HasValue)
        {
            query = query.Where(attendance => attendance.SectionId == sectionId.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(attendance => attendance.Date >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(attendance => attendance.Date <= to.Value);
        }

        var attendances = await query
            .OrderByDescending(attendance => attendance.Date)
            .ToListAsync();

        var markerUserIds = attendances
            .Where(attendance => attendance.MarkedBy > 0)
            .Select(attendance => attendance.MarkedBy)
            .Distinct()
            .ToHashSet();

        var teacherNames = await _context.Teachers
            .Where(teacher => markerUserIds.Contains(teacher.UserId))
            .ToDictionaryAsync(teacher => teacher.UserId, teacher => $"{teacher.FirstName} {teacher.LastName}");

        var records = attendances.Select(attendance =>
        {
            teacherNames.TryGetValue(attendance.MarkedBy, out var markerName);

            var scheduleStart = attendance.Schedule?.StartTime ?? new TimeOnly(0, 0);
            var status = AttendancePolicy.GetMarkedStatus(attendance.TimeIn, scheduleStart, _attendanceSettings);

            return new AttendanceDto
            {
                Id = attendance.Id,
                ScheduleId = attendance.ScheduleId,
                SubjectName = attendance.Schedule?.Subject?.Name,
                StudentId = attendance.StudentId,
                StudentName = attendance.Student != null ? $"{attendance.Student.FirstName} {attendance.Student.LastName}" : null,
                SectionId = attendance.SectionId,
                SectionName = attendance.Section?.Name,
                Date = attendance.Date,
                TimeIn = attendance.TimeIn,
                TimeOut = attendance.TimeOut,
                Remarks = attendance.Remarks,
                MarkedAt = attendance.MarkedAt,
                MarkedBy = attendance.MarkedBy,
                MarkerName = markerName,
                IsMarked = true,
                IsLate = status == AttendanceStatusKind.Late,
                StatusLabel = AttendancePolicy.ToLabel(status),
                StatusClass = AttendancePolicy.ToCssClass(status)
            };
        }).ToList();

        return ApiResponse<List<AttendanceDto>>.SuccessResponse(records);
    }

    private async Task<ScheduleValidationResult> ValidateScheduleForMarkingAsync(
        int sectionId,
        int scheduleId,
        DateOnly date,
        TeacherContext teacherContext)
    {
        // Attendance must always target a real schedule tied to the requested section.
        var schedule = await _context.Schedules
            .Include(s => s.Subject)
            .FirstOrDefaultAsync(s => s.Id == scheduleId && s.SectionId == sectionId);

        if (schedule == null)
        {
            return ScheduleValidationResult.Fail("NOT_FOUND", "Schedule not found for this section.");
        }

        if ((int)date.DayOfWeek != schedule.DayOfWeek)
        {
            return ScheduleValidationResult.Fail("VALIDATION_ERROR", "Attendance date must match the schedule weekday.");
        }

        if (date < schedule.EffectiveFrom || (schedule.EffectiveTo.HasValue && date > schedule.EffectiveTo.Value))
        {
            return ScheduleValidationResult.Fail(
                "VALIDATION_ERROR",
                "Attendance date is outside the schedule effective date range.");
        }

        if (teacherContext.IsAdmin)
        {
            // Admin bypasses teacher ownership and backfill limits by design.
            return ScheduleValidationResult.Pass(schedule);
        }

        var teacherId = teacherContext.GetSectionValidationId();
        if (!teacherId.HasValue)
        {
            return ScheduleValidationResult.Fail("FORBIDDEN", "Teacher profile not found.");
        }

        if (!schedule.TeacherId.HasValue || schedule.TeacherId.Value != teacherId.Value)
        {
            return ScheduleValidationResult.Fail(
                "FORBIDDEN",
                "Only the schedule owner teacher can mark or correct attendance for this schedule.");
        }

        var schoolToday = AttendancePolicy.GetSchoolDate(_attendanceSettings, DateTimeOffset.UtcNow);
        if (date > schoolToday)
        {
            return ScheduleValidationResult.Fail(
                "VALIDATION_ERROR",
                "Teachers cannot mark attendance for future dates.");
        }

        if (!AttendancePolicy.IsWithinTeacherWindow(_attendanceSettings, date, schoolToday))
        {
            return ScheduleValidationResult.Fail(
                "VALIDATION_ERROR",
                $"Teachers can only mark attendance from today back to {_attendanceSettings.TeacherBackfillDays} days.");
        }

        return ScheduleValidationResult.Pass(schedule);
    }

    private async Task<AcademicYear?> GetActiveAcademicYearAsync()
    {
        return await _context.AcademicYears.FirstOrDefaultAsync(academicYear => academicYear.IsActive);
    }

    private static string? NormalizeRemarks(TimeOnly? timeIn, string? remarks)
    {
        var trimmed = remarks?.Trim();

        // Missing time-in is treated as absent; auto-fill that remark when caller leaves it blank.
        if (!timeIn.HasValue)
        {
            return string.IsNullOrWhiteSpace(trimmed) ? "Absent" : trimmed;
        }

        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static AttendanceAudit BuildAttendanceAudit(
        Attendance attendance,
        string action,
        TimeOnly? beforeTimeIn,
        string? beforeRemarks,
        AttendanceStatusKind? beforeStatus,
        TimeOnly? afterTimeIn,
        string? afterRemarks,
        AttendanceStatusKind afterStatus,
        int actorUserId,
        DateTimeOffset actionAt)
    {
        // Persisting labels keeps audit snapshots human-readable even if status rules evolve later.
        return new AttendanceAudit
        {
            AttendanceId = attendance.Id,
            Action = action,
            BeforeTimeIn = beforeTimeIn,
            BeforeRemarks = beforeRemarks,
            BeforeStatus = beforeStatus.HasValue ? AttendancePolicy.ToLabel(beforeStatus.Value) : null,
            AfterTimeIn = afterTimeIn,
            AfterRemarks = afterRemarks,
            AfterStatus = AttendancePolicy.ToLabel(afterStatus),
            ActorUserId = actorUserId,
            ActionAt = actionAt
        };
    }

    private static AttendanceDto BuildAttendanceDto(
        Attendance attendance,
        Schedule schedule,
        Student? student,
        string? sectionName,
        string? markerName,
        AttendanceStatusKind status,
        bool isMarked)
    {
        return new AttendanceDto
        {
            Id = attendance.Id,
            ScheduleId = attendance.ScheduleId,
            SubjectName = schedule.Subject?.Name,
            StudentId = attendance.StudentId,
            StudentName = student != null ? $"{student.FirstName} {student.LastName}" : null,
            SectionId = attendance.SectionId,
            SectionName = sectionName,
            Date = attendance.Date,
            TimeIn = attendance.TimeIn,
            TimeOut = attendance.TimeOut,
            Remarks = attendance.Remarks,
            MarkedAt = attendance.MarkedAt,
            MarkedBy = attendance.MarkedBy,
            MarkerName = markerName,
            IsMarked = isMarked,
            IsLate = status == AttendanceStatusKind.Late,
            StatusLabel = AttendancePolicy.ToLabel(status),
            StatusClass = AttendancePolicy.ToCssClass(status)
        };
    }

    private async Task<string?> GetMarkerNameAsync(int userId)
    {
        // Marker names are optional display metadata and should not block attendance writes.
        var teacher = await _context.Teachers
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.UserId == userId);

        return teacher != null ? $"{teacher.FirstName} {teacher.LastName}" : null;
    }

    private sealed class BulkAttendanceMutation
    {
        public Attendance Attendance { get; set; } = null!;
        public Student Student { get; set; } = null!;
        public string Action { get; set; } = string.Empty;
        public TimeOnly? BeforeTimeIn { get; set; }
        public string? BeforeRemarks { get; set; }
        public AttendanceStatusKind? BeforeStatus { get; set; }
        public AttendanceStatusKind AfterStatus { get; set; }
    }

    private readonly record struct ScheduleValidationResult(
        bool Success,
        Schedule? Schedule,
        string? ErrorCode,
        string? ErrorMessage)
    {
        // Small helper factory methods keep call sites terse and consistent.
        public static ScheduleValidationResult Pass(Schedule schedule) => new(true, schedule, null, null);

        public static ScheduleValidationResult Fail(string errorCode, string errorMessage)
            => new(false, null, errorCode, errorMessage);
    }
}
