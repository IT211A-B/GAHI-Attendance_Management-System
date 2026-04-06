using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Attendance_Management_System.Backend.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace Attendance_Management_System.Backend.Services;

// Service implementation for managing student attendance
public class AttendanceService : IAttendanceService
{
    private readonly AppDbContext _context;

    public AttendanceService(AppDbContext context)
    {
        _context = context;
    }

    // Marks attendance for a single student after validating all business rules
    public async Task<ApiResponse<AttendanceDto>> MarkAttendanceAsync(MarkAttendanceRequest request, TeacherContext teacherContext)
    {
        // Validate section assignment for non-admin users (teachers must be assigned to the section)
        if (!teacherContext.IsAdmin)
        {
            var sectionValidationId = teacherContext.GetSectionValidationId();
            if (!sectionValidationId.HasValue)
            {
                return ApiResponse<AttendanceDto>.ErrorResponse(
                    "FORBIDDEN",
                    "Teacher profile not found.");
            }

            // Verify teacher is assigned to the section using Teacher.Id (not User.Id)
            var isAssigned = await _context.SectionTeachers
                .AnyAsync(st => st.TeacherId == sectionValidationId.Value && st.SectionId == request.SectionId);

            if (!isAssigned)
            {
                return ApiResponse<AttendanceDto>.ErrorResponse(
                    "FORBIDDEN",
                    "You are not assigned to this section.");
            }
        }

        // Verify schedule belongs to the section
        var schedule = await _context.Schedules
            .Include(s => s.Subject)
            .FirstOrDefaultAsync(s => s.Id == request.ScheduleId && s.SectionId == request.SectionId);

        if (schedule == null)
        {
            return ApiResponse<AttendanceDto>.ErrorResponse(
                "NOT_FOUND",
                "Schedule not found for this section.");
        }

        // Verify student belongs to the section
        var student = await _context.Students
            .FirstOrDefaultAsync(s => s.Id == request.StudentId && s.SectionId == request.SectionId);

        if (student == null)
        {
            return ApiResponse<AttendanceDto>.ErrorResponse(
                "NOT_FOUND",
                "Student not found in this section.");
        }

        // Check for duplicate attendance record for same student/schedule/date
        var existingAttendance = await _context.Attendances
            .AnyAsync(a => a.ScheduleId == request.ScheduleId
                && a.StudentId == request.StudentId
                && a.Date == request.Date);

        if (existingAttendance)
        {
            return ApiResponse<AttendanceDto>.ErrorResponse(
                "DUPLICATE",
                "Attendance already marked for this student on this date.");
        }

        // Get the active academic year for the enrollment period
        var academicYear = await _context.AcademicYears
            .FirstOrDefaultAsync(ay => ay.IsActive);

        if (academicYear == null)
        {
            return ApiResponse<AttendanceDto>.ErrorResponse(
                "NOT_FOUND",
                "No active academic year found.");
        }

        // Get section for response
        var section = await _context.Sections.FindAsync(request.SectionId);

        // Get marker name for response
        var markerName = await GetMarkerNameAsync(teacherContext.UserId);

        // Create attendance record - use UserId for MarkedBy (FK to User table)
        // If no TimeIn provided, student is marked as absent
        var attendance = new Attendance
        {
            ScheduleId = request.ScheduleId,
            StudentId = request.StudentId,
            SectionId = request.SectionId,
            AcademicYearId = academicYear.Id,
            Date = request.Date,
            TimeIn = request.TimeIn,
            Remarks = request.TimeIn.HasValue ? request.Remarks : "Absent",
            MarkedBy = teacherContext.GetMarkerId(),
            MarkedAt = DateTimeOffset.UtcNow
        };

        _context.Attendances.Add(attendance);
        await _context.SaveChangesAsync();

        // Build response DTO with all related data
        var attendanceDto = new AttendanceDto
        {
            Id = attendance.Id,
            ScheduleId = attendance.ScheduleId,
            SubjectName = schedule.Subject?.Name,
            StudentId = attendance.StudentId,
            StudentName = $"{student.FirstName} {student.LastName}",
            SectionId = attendance.SectionId,
            SectionName = section?.Name,
            Date = attendance.Date,
            TimeIn = attendance.TimeIn,
            TimeOut = attendance.TimeOut,
            Remarks = attendance.Remarks,
            MarkedAt = attendance.MarkedAt,
            MarkedBy = attendance.MarkedBy,
            MarkerName = markerName
        };

        return ApiResponse<AttendanceDto>.SuccessResponse(attendanceDto);
    }

    // Marks attendance for multiple students in a single transaction

    public async Task<ApiResponse<List<AttendanceDto>>> MarkBulkAttendanceAsync(BulkAttendanceRequest request, TeacherContext teacherContext)
    {
        // Validate section assignment for non-admin users (teachers must be assigned to the section)
        if (!teacherContext.IsAdmin)
        {
            var sectionValidationId = teacherContext.GetSectionValidationId();
            if (!sectionValidationId.HasValue)
            {
                return ApiResponse<List<AttendanceDto>>.ErrorResponse(
                    "FORBIDDEN",
                    "Teacher profile not found.");
            }

            // Verify teacher is assigned to the section using Teacher.Id (not User.Id)
            var isAssigned = await _context.SectionTeachers
                .AnyAsync(st => st.TeacherId == sectionValidationId.Value && st.SectionId == request.SectionId);

            if (!isAssigned)
            {
                return ApiResponse<List<AttendanceDto>>.ErrorResponse(
                    "FORBIDDEN",
                    "You are not assigned to this section.");
            }
        }

        // Verify schedule belongs to the section
        var schedule = await _context.Schedules
            .Include(s => s.Subject)
            .FirstOrDefaultAsync(s => s.Id == request.ScheduleId && s.SectionId == request.SectionId);

        if (schedule == null)
        {
            return ApiResponse<List<AttendanceDto>>.ErrorResponse(
                "NOT_FOUND",
                "Schedule not found for this section.");
        }

        // Get the active academic year for the enrollment period
        var academicYear = await _context.AcademicYears
            .FirstOrDefaultAsync(ay => ay.IsActive);

        if (academicYear == null)
        {
            return ApiResponse<List<AttendanceDto>>.ErrorResponse(
                "NOT_FOUND",
                "No active academic year found.");
        }

        // Pre-load all data needed for bulk operation to avoid multiple database round-trips
        var section = await _context.Sections.FindAsync(request.SectionId);
        var markerName = await GetMarkerNameAsync(teacherContext.UserId);
        var studentIds = request.Entries.Select(e => e.StudentId).ToHashSet();

        // Load all students in one query for efficient validation
        var students = await _context.Students
            .Where(s => studentIds.Contains(s.Id) && s.SectionId == request.SectionId)
            .ToDictionaryAsync(s => s.Id);

        // Check for existing attendances in one query to detect duplicates
        var existingAttendances = await _context.Attendances
            .Where(a => a.ScheduleId == request.ScheduleId
                && a.Date == request.Date
                && studentIds.Contains(a.StudentId))
            .Select(a => a.StudentId)
            .ToHashSetAsync();

        var attendanceDtos = new List<AttendanceDto>();
        var errors = new List<string>();

        // Use transaction to ensure atomic operation - all records succeed or fail together
        await using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var attendanceRecords = new List<Attendance>();

            foreach (var entry in request.Entries)
            {
                // Verify student belongs to the section
                if (!students.TryGetValue(entry.StudentId, out var student))
                {
                    errors.Add($"Student {entry.StudentId} not found in this section.");
                    continue;
                }

                // Skip if attendance already marked for this student
                if (existingAttendances.Contains(entry.StudentId))
                {
                    errors.Add($"Attendance already marked for student {entry.StudentId} on this date.");
                    continue;
                }

                // Create attendance record - if no TimeIn provided, student is marked as absent
                var attendance = new Attendance
                {
                    ScheduleId = request.ScheduleId,
                    StudentId = entry.StudentId,
                    SectionId = request.SectionId,
                    AcademicYearId = academicYear.Id,
                    Date = request.Date,
                    TimeIn = entry.TimeIn,
                    Remarks = entry.TimeIn.HasValue ? entry.Remarks : "Absent",
                    MarkedBy = teacherContext.GetMarkerId(),
                    MarkedAt = DateTimeOffset.UtcNow
                };

                attendanceRecords.Add(attendance);
                _context.Attendances.Add(attendance);
            }

            // Single SaveChanges for all records to improve performance
            if (attendanceRecords.Any())
            {
                await _context.SaveChangesAsync();
            }

            // Commit transaction if all operations succeeded
            await transaction.CommitAsync();

            // Build response DTOs after successful save
            foreach (var attendance in attendanceRecords)
            {
                var student = students[attendance.StudentId];
                attendanceDtos.Add(new AttendanceDto
                {
                    Id = attendance.Id,
                    ScheduleId = attendance.ScheduleId,
                    SubjectName = schedule.Subject?.Name,
                    StudentId = attendance.StudentId,
                    StudentName = $"{student.FirstName} {student.LastName}",
                    SectionId = attendance.SectionId,
                    SectionName = section?.Name,
                    Date = attendance.Date,
                    TimeIn = attendance.TimeIn,
                    TimeOut = attendance.TimeOut,
                    Remarks = attendance.Remarks,
                    MarkedAt = attendance.MarkedAt,
                    MarkedBy = attendance.MarkedBy,
                    MarkerName = markerName
                });
            }
        }
        catch (Exception)
        {
            // Rollback transaction on any error to maintain data consistency
            await transaction.RollbackAsync();
            throw;
        }

        // Return error if all entries failed
        if (errors.Any() && !attendanceDtos.Any())
        {
            return ApiResponse<List<AttendanceDto>>.ErrorResponse(
                "BULK_FAILED",
                string.Join(" ", errors));
        }

        return ApiResponse<List<AttendanceDto>>.SuccessResponse(attendanceDtos);
    }

    // Gets attendance summary for all students in a section on a specific date
    // Includes both present and absent students with late calculation
    public async Task<ApiResponse<AttendanceSummaryDto>> GetSectionAttendanceAsync(int sectionId, DateOnly date, int scheduleId)
    {
        // Get section info
        var section = await _context.Sections
            .Include(s => s.Subject)
            .FirstOrDefaultAsync(s => s.Id == sectionId);

        if (section == null)
        {
            return ApiResponse<AttendanceSummaryDto>.ErrorResponse(
                "NOT_FOUND",
                "Section not found.");
        }

        // Get all active students in the section
        var students = await _context.Students
            .Where(s => s.SectionId == sectionId && s.IsActive)
            .ToListAsync();

        // Get attendance records for the specific date and schedule
        var attendances = await _context.Attendances
            .Include(a => a.Schedule)
                .ThenInclude(s => s!.Subject)
            .Include(a => a.Student)
            .Include(a => a.Marker)
            .Where(a => a.SectionId == sectionId && a.Date == date && a.ScheduleId == scheduleId)
            .ToListAsync();

        // Pre-load marker names to avoid N+1 query problem inside loops
        var markerUserIds = attendances
            .Where(a => a.MarkedBy > 0)
            .Select(a => a.MarkedBy)
            .Distinct()
            .ToHashSet();

        var teacherNames = await _context.Teachers
            .Where(t => markerUserIds.Contains(t.UserId))
            .ToDictionaryAsync(t => t.UserId, t => $"{t.FirstName} {t.LastName}");

        // Get schedule to determine late threshold (15 minutes after start time)
        var schedule = await _context.Schedules
            .Include(s => s.Subject)
            .FirstOrDefaultAsync(s => s.Id == scheduleId);
        var lateThreshold = schedule?.StartTime.AddMinutes(15) ?? new TimeOnly(8, 15);

        // Map attendance records to DTOs using pre-loaded data
        var records = attendances.Select(a =>
        {
            string? markerName = null;
            if (a.MarkedBy > 0 && teacherNames.TryGetValue(a.MarkedBy, out var name))
            {
                markerName = name;
            }

            return new AttendanceDto
            {
                Id = a.Id,
                ScheduleId = a.ScheduleId,
                SubjectName = a.Schedule?.Subject?.Name,
                StudentId = a.StudentId,
                StudentName = a.Student != null ? $"{a.Student.FirstName} {a.Student.LastName}" : null,
                SectionId = a.SectionId,
                SectionName = section.Name,
                Date = a.Date,
                TimeIn = a.TimeIn,
                TimeOut = a.TimeOut,
                Remarks = a.Remarks,
                MarkedAt = a.MarkedAt,
                MarkedBy = a.MarkedBy,
                MarkerName = markerName
            };
        }).ToList();

        // Add absent students (students without attendance records for this date/schedule)
        var attendedStudentIds = attendances.Select(a => a.StudentId).ToHashSet();
        var absentStudents = students.Where(s => !attendedStudentIds.Contains(s.Id));

        foreach (var student in absentStudents)
        {
            records.Add(new AttendanceDto
            {
                Id = 0,
                ScheduleId = scheduleId,
                SubjectName = schedule?.Subject?.Name ?? section.Subject?.Name,
                StudentId = student.Id,
                StudentName = $"{student.FirstName} {student.LastName}",
                SectionId = sectionId,
                SectionName = section.Name,
                Date = date,
                TimeIn = null,
                TimeOut = null,
                Remarks = "Absent",
                MarkedAt = DateTimeOffset.UtcNow,
                MarkedBy = 0,
                MarkerName = null
            });
        }

        // Calculate attendance summary statistics
        var presentCount = attendances.Count(a => a.TimeIn.HasValue);
        var lateCount = attendances.Count(a => a.TimeIn.HasValue && a.TimeIn.Value > lateThreshold);
        var absentCount = students.Count - presentCount;

        var summary = new AttendanceSummaryDto
        {
            TotalStudents = students.Count,
            PresentCount = presentCount,
            AbsentCount = absentCount,
            LateCount = lateCount,
            Records = records.OrderBy(r => r.StudentName).ToList()
        };

        return ApiResponse<AttendanceSummaryDto>.SuccessResponse(summary);
    }

    // Gets attendance history for a specific student with optional filtering by section and date range
    public async Task<ApiResponse<List<AttendanceDto>>> GetStudentAttendanceAsync(int studentId, int? sectionId, DateOnly? from, DateOnly? to)
    {
        // Build base query with related data
        var query = _context.Attendances
            .Include(a => a.Schedule)
                .ThenInclude(s => s!.Subject)
            .Include(a => a.Student)
            .Include(a => a.Section)
            .Where(a => a.StudentId == studentId);

        // Apply optional section filter
        if (sectionId.HasValue)
        {
            query = query.Where(a => a.SectionId == sectionId.Value);
        }

        // Apply optional date range filters
        if (from.HasValue)
        {
            query = query.Where(a => a.Date >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(a => a.Date <= to.Value);
        }

        // Get results ordered by most recent first
        var attendances = await query
            .OrderByDescending(a => a.Date)
            .ToListAsync();

        // Pre-load marker names to avoid N+1 query problem
        var markerUserIds = attendances
            .Where(a => a.MarkedBy > 0)
            .Select(a => a.MarkedBy)
            .Distinct()
            .ToHashSet();

        var teacherNames = await _context.Teachers
            .Where(t => markerUserIds.Contains(t.UserId))
            .ToDictionaryAsync(t => t.UserId, t => $"{t.FirstName} {t.LastName}");

        // Map attendance records to DTOs using pre-loaded data
        var records = attendances.Select(a =>
        {
            string? markerName = null;
            if (a.MarkedBy > 0 && teacherNames.TryGetValue(a.MarkedBy, out var name))
            {
                markerName = name;
            }

            return new AttendanceDto
            {
                Id = a.Id,
                ScheduleId = a.ScheduleId,
                SubjectName = a.Schedule?.Subject?.Name,
                StudentId = a.StudentId,
                StudentName = a.Student != null ? $"{a.Student.FirstName} {a.Student.LastName}" : null,
                SectionId = a.SectionId,
                SectionName = a.Section?.Name,
                Date = a.Date,
                TimeIn = a.TimeIn,
                TimeOut = a.TimeOut,
                Remarks = a.Remarks,
                MarkedAt = a.MarkedAt,
                MarkedBy = a.MarkedBy,
                MarkerName = markerName
            };
        }).ToList();

        return ApiResponse<List<AttendanceDto>>.SuccessResponse(records);
    }

    // Gets the marker's name from their UserId
    // Returns teacher's name if found, otherwise null
    private async Task<string?> GetMarkerNameAsync(int userId)
    {
        var teacher = await _context.Teachers
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.UserId == userId);

        return teacher != null ? $"{teacher.FirstName} {teacher.LastName}" : null;
    }
}