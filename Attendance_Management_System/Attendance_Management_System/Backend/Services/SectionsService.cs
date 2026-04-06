using Attendance_Management_System.Backend.Constants;
using Attendance_Management_System.Backend.Configuration;
using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Enums;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Attendance_Management_System.Backend.Services;

// Service handling all section-related business logic
public class SectionsService : ISectionsService
{
    private readonly AppDbContext _context;
    private readonly EnrollmentSettings _enrollmentSettings;

    // Inject database context and enrollment settings through constructor
    public SectionsService(AppDbContext context, IOptions<EnrollmentSettings> enrollmentSettings)
    {
        _context = context;
        _enrollmentSettings = enrollmentSettings.Value?.IsValid() == true
            ? enrollmentSettings.Value
            : EnrollmentSettings.Default;
    }

    // Retrieves all sections with related entity names (academic year, course, subject, classroom)
    public async Task<ApiResponse<List<SectionDto>>> GetAllSectionsAsync()
    {
        var sections = await _context.Sections
            .Include(s => s.AcademicYear)
            .Include(s => s.Course)
            .Include(s => s.Subject)
            .Include(s => s.Classroom)
            .OrderBy(s => s.Name)
            .Select(s => new SectionDto
            {
                Id = s.Id,
                Name = s.Name,
                AcademicYearId = s.AcademicYearId,
                AcademicYearLabel = s.AcademicYear != null ? s.AcademicYear.YearLabel : null,
                CourseId = s.CourseId,
                CourseName = s.Course != null ? s.Course.Name : null,
                SubjectId = s.SubjectId,
                SubjectName = s.Subject != null ? s.Subject.Name : null,
                ClassroomId = s.ClassroomId,
                ClassroomName = s.Classroom != null ? s.Classroom.Name : null,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();

        return ApiResponse<List<SectionDto>>.SuccessResponse(sections);
    }

    public async Task<ApiResponse<List<SectionDto>>> GetSectionsByTeacherUserIdAsync(int teacherUserId)
    {
        var teacherId = await _context.Teachers
            .Where(teacher => teacher.UserId == teacherUserId)
            .Select(teacher => (int?)teacher.Id)
            .FirstOrDefaultAsync();

        if (!teacherId.HasValue)
        {
            return ApiResponse<List<SectionDto>>.ErrorResponse(ErrorCodes.NotFound, "Teacher profile not found.");
        }

        // Include both explicit section assignments and legacy/owned schedules.
        // This keeps /student(s) in sync even if section links were not created historically.
        var sectionIds = await _context.SectionTeachers
            .Where(assignment => assignment.TeacherId == teacherId.Value)
            .Select(assignment => assignment.SectionId)
            .Union(_context.Schedules
                .Where(schedule => schedule.TeacherId == teacherId.Value)
                .Select(schedule => schedule.SectionId))
            .Distinct()
            .ToListAsync();

        var sections = await _context.Sections
            .Include(section => section.AcademicYear)
            .Include(section => section.Course)
            .Include(section => section.Subject)
            .Include(section => section.Classroom)
            .Where(section => sectionIds.Contains(section.Id))
            .OrderBy(section => section.Name)
            .Select(section => new SectionDto
            {
                Id = section.Id,
                Name = section.Name,
                YearLevel = section.YearLevel,
                AcademicYearId = section.AcademicYearId,
                AcademicYearLabel = section.AcademicYear != null ? section.AcademicYear.YearLabel : null,
                CourseId = section.CourseId,
                CourseName = section.Course != null ? section.Course.Name : null,
                SubjectId = section.SubjectId,
                SubjectName = section.Subject != null ? section.Subject.Name : null,
                ClassroomId = section.ClassroomId,
                ClassroomName = section.Classroom != null ? section.Classroom.Name : null,
                CreatedAt = section.CreatedAt
            })
            .ToListAsync();

        return ApiResponse<List<SectionDto>>.SuccessResponse(sections);
    }

    // Retrieves a single section by ID with related entity details
    public async Task<ApiResponse<SectionDto>> GetSectionByIdAsync(int id)
    {
        var section = await _context.Sections
            .Include(s => s.AcademicYear)
            .Include(s => s.Course)
            .Include(s => s.Subject)
            .Include(s => s.Classroom)
            .FirstOrDefaultAsync(s => s.Id == id);

        // Return error if section doesn't exist
        if (section == null)
        {
            return ApiResponse<SectionDto>.ErrorResponse("NOT_FOUND", "Section not found.");
        }

        var dto = new SectionDto
        {
            Id = section.Id,
            Name = section.Name,
            AcademicYearId = section.AcademicYearId,
            AcademicYearLabel = section.AcademicYear?.YearLabel,
            CourseId = section.CourseId,
            CourseName = section.Course?.Name,
            SubjectId = section.SubjectId,
            SubjectName = section.Subject?.Name,
            ClassroomId = section.ClassroomId,
            ClassroomName = section.Classroom?.Name,
            CreatedAt = section.CreatedAt
        };

        return ApiResponse<SectionDto>.SuccessResponse(dto);
    }

    // Retrieves all sections for a specific academic year
    public async Task<ApiResponse<List<SectionDto>>> GetSectionsByAcademicYearIdAsync(int academicYearId)
    {
        // Validate academic year exists before querying sections
        var academicYearExists = await _context.AcademicYears.AnyAsync(ay => ay.Id == academicYearId);
        if (!academicYearExists)
        {
            return ApiResponse<List<SectionDto>>.ErrorResponse("NOT_FOUND", "Academic year not found.");
        }

        var sections = await _context.Sections
            .Include(s => s.AcademicYear)
            .Include(s => s.Course)
            .Include(s => s.Subject)
            .Include(s => s.Classroom)
            .Where(s => s.AcademicYearId == academicYearId)
            .OrderBy(s => s.Name)
            .Select(s => new SectionDto
            {
                Id = s.Id,
                Name = s.Name,
                AcademicYearId = s.AcademicYearId,
                AcademicYearLabel = s.AcademicYear != null ? s.AcademicYear.YearLabel : null,
                CourseId = s.CourseId,
                CourseName = s.Course != null ? s.Course.Name : null,
                SubjectId = s.SubjectId,
                SubjectName = s.Subject != null ? s.Subject.Name : null,
                ClassroomId = s.ClassroomId,
                ClassroomName = s.Classroom != null ? s.Classroom.Name : null,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();

        return ApiResponse<List<SectionDto>>.SuccessResponse(sections);
    }

    public async Task<ApiResponse<SectionDto>> CreateSectionAsync(CreateSectionRequest request)
    {
        // Validate academic year exists
        var academicYearExists = await _context.AcademicYears.AnyAsync(ay => ay.Id == request.AcademicYearId);
        if (!academicYearExists)
        {
            return ApiResponse<SectionDto>.ErrorResponse("VALIDATION_ERROR", "Academic year not found.");
        }

        // Validate course exists
        var courseExists = await _context.Courses.AnyAsync(c => c.Id == request.CourseId);
        if (!courseExists)
        {
            return ApiResponse<SectionDto>.ErrorResponse("VALIDATION_ERROR", "Course not found.");
        }

        // Validate subject exists and belongs to the selected course
        var subjectCourseId = await _context.Subjects
            .Where(subject => subject.Id == request.SubjectId)
            .Select(subject => (int?)subject.CourseId)
            .FirstOrDefaultAsync();

        if (!subjectCourseId.HasValue)
        {
            return ApiResponse<SectionDto>.ErrorResponse("VALIDATION_ERROR", "Subject not found.");
        }

        if (subjectCourseId.Value != request.CourseId)
        {
            return ApiResponse<SectionDto>.ErrorResponse(
                "VALIDATION_ERROR",
                "Selected subject does not belong to the selected course.");
        }

        // Validate classroom exists
        var classroomExists = await _context.Classrooms.AnyAsync(c => c.Id == request.ClassroomId);
        if (!classroomExists)
        {
            return ApiResponse<SectionDto>.ErrorResponse("VALIDATION_ERROR", "Classroom not found.");
        }

        var section = new Section
        {
            Name = request.Name,
            YearLevel = request.YearLevel,
            AcademicYearId = request.AcademicYearId,
            CourseId = request.CourseId,
            SubjectId = request.SubjectId,
            ClassroomId = request.ClassroomId
        };

        _context.Sections.Add(section);
        await _context.SaveChangesAsync();

        var dto = await BuildSectionDtoAsync(section);

        return ApiResponse<SectionDto>.SuccessResponse(dto);
    }

    public async Task<ApiResponse<SectionDto>> UpdateSectionAsync(int id, UpdateSectionRequest request)
    {
        var section = await _context.Sections.FindAsync(id);

        if (section == null)
        {
            return ApiResponse<SectionDto>.ErrorResponse("NOT_FOUND", "Section not found.");
        }

        // Validate academic year exists if being changed
        if (request.AcademicYearId.HasValue && request.AcademicYearId != section.AcademicYearId)
        {
            var academicYearExists = await _context.AcademicYears.AnyAsync(ay => ay.Id == request.AcademicYearId.Value);
            if (!academicYearExists)
            {
                return ApiResponse<SectionDto>.ErrorResponse("VALIDATION_ERROR", "Academic year not found.");
            }
        }

        // Validate course exists if being changed
        if (request.CourseId.HasValue && request.CourseId != section.CourseId)
        {
            var courseExists = await _context.Courses.AnyAsync(c => c.Id == request.CourseId.Value);
            if (!courseExists)
            {
                return ApiResponse<SectionDto>.ErrorResponse("VALIDATION_ERROR", "Course not found.");
            }
        }

        // Validate subject exists if being changed
        if (request.SubjectId.HasValue && request.SubjectId != section.SubjectId)
        {
            var subjectExists = await _context.Subjects.AnyAsync(s => s.Id == request.SubjectId.Value);
            if (!subjectExists)
            {
                return ApiResponse<SectionDto>.ErrorResponse("VALIDATION_ERROR", "Subject not found.");
            }
        }

        // Validate classroom exists if being changed
        if (request.ClassroomId.HasValue && request.ClassroomId != section.ClassroomId)
        {
            var classroomExists = await _context.Classrooms.AnyAsync(c => c.Id == request.ClassroomId.Value);
            if (!classroomExists)
            {
                return ApiResponse<SectionDto>.ErrorResponse("VALIDATION_ERROR", "Classroom not found.");
            }
        }

        if (!string.IsNullOrEmpty(request.Name))
            section.Name = request.Name;
        if (request.YearLevel.HasValue)
            section.YearLevel = request.YearLevel.Value;
        if (request.AcademicYearId.HasValue)
            section.AcademicYearId = request.AcademicYearId.Value;
        if (request.CourseId.HasValue)
            section.CourseId = request.CourseId.Value;
        if (request.SubjectId.HasValue)
            section.SubjectId = request.SubjectId.Value;
        if (request.ClassroomId.HasValue)
            section.ClassroomId = request.ClassroomId.Value;

        await _context.SaveChangesAsync();

        var dto = await BuildSectionDtoAsync(section);

        return ApiResponse<SectionDto>.SuccessResponse(dto);
    }

    public async Task<ApiResponse<bool>> DeleteSectionAsync(int id)
    {
        var section = await _context.Sections.FindAsync(id);

        if (section == null)
        {
            return ApiResponse<bool>.ErrorResponse("NOT_FOUND", "Section not found.");
        }

        // Check if section has enrollments
        var hasEnrollments = await _context.Enrollments.AnyAsync(e => e.SectionId == id);
        if (hasEnrollments)
        {
            return ApiResponse<bool>.ErrorResponse(ErrorCodes.InUse, "Cannot delete section that has enrollments.");
        }

        _context.Sections.Remove(section);
        await _context.SaveChangesAsync();

        return ApiResponse<bool>.SuccessResponse(true);
    }

    private async Task<SectionDto> BuildSectionDtoAsync(Section section)
    {
        var academicYear = await _context.AcademicYears.FindAsync(section.AcademicYearId);
        var course = await _context.Courses.FindAsync(section.CourseId);
        var subject = await _context.Subjects.FindAsync(section.SubjectId);
        var classroom = await _context.Classrooms.FindAsync(section.ClassroomId);

        // Get enrollment count for capacity status
        var enrollmentCount = await _context.Enrollments
            .CountAsync(e => e.SectionId == section.Id && e.Status == "approved");

        var capacityStatus = CalculateCapacityStatus(enrollmentCount);

        return new SectionDto
        {
            Id = section.Id,
            Name = section.Name,
            YearLevel = section.YearLevel,
            AcademicYearId = section.AcademicYearId,
            AcademicYearLabel = academicYear?.YearLabel,
            CourseId = section.CourseId,
            CourseName = course?.Name,
            SubjectId = section.SubjectId,
            SubjectName = subject?.Name,
            ClassroomId = section.ClassroomId,
            ClassroomName = classroom?.Name,
            CurrentEnrollmentCount = enrollmentCount,
            CapacityStatus = capacityStatus.ToString(),
            CreatedAt = section.CreatedAt
        };
    }

    private static readonly string[] DayNames = { "Sun", "Mon", "Tue", "Wed", "Thu", "Fri", "Sat" };

    public async Task<ApiResponse<TimetableResponse>> GetTimetableAsync(int sectionId, int? currentUserId = null)
    {
        // Check if section exists
        var section = await _context.Sections
            .Include(s => s.Classroom)
            .FirstOrDefaultAsync(s => s.Id == sectionId);

        if (section == null)
        {
            return ApiResponse<TimetableResponse>.ErrorResponse(ErrorCodes.NotFound, "Section not found.");
        }

        // Determine current teacher ID if user is a teacher
        int? currentTeacherId = null;
        if (currentUserId.HasValue)
        {
            currentTeacherId = await _context.Teachers
                .Where(t => t.UserId == currentUserId.Value)
                .Select(t => (int?)t.Id)
                .FirstOrDefaultAsync();
        }

        // Get all schedules for this section
        var schedules = await _context.Schedules
            .Include(s => s.Subject)
            .Include(s => s.Teacher)
            .Where(s => s.SectionId == sectionId)
            .ToListAsync();

        // Build timetable dictionary with all 7 days
        var timetable = new Dictionary<string, List<ScheduleSlotDto>>();
        foreach (var dayName in DayNames)
        {
            timetable[dayName] = new List<ScheduleSlotDto>();
        }

        // Populate with schedules
        foreach (var schedule in schedules)
        {
            var dayIndex = schedule.DayOfWeek;
            if (dayIndex >= 0 && dayIndex < 7)
            {
                var dayName = DayNames[dayIndex];
                var slot = new ScheduleSlotDto
                {
                    ScheduleId = schedule.Id,
                    SubjectId = schedule.SubjectId,
                    DayOfWeek = schedule.DayOfWeek,
                    TeacherId = schedule.TeacherId,
                    SubjectName = schedule.Subject?.Name ?? string.Empty,
                    TeacherName = schedule.Teacher != null
                        ? $"{schedule.Teacher.FirstName} {schedule.Teacher.LastName}".Trim()
                        : "Unassigned",
                    Classroom = section.Classroom?.Name ?? string.Empty,
                    StartTime = schedule.StartTime.ToString("HH:mm"),
                    EndTime = schedule.EndTime.ToString("HH:mm"),
                    IsMine = currentTeacherId.HasValue
                        && schedule.TeacherId.HasValue
                        && schedule.TeacherId.Value == currentTeacherId.Value
                };
                timetable[dayName].Add(slot);
            }
        }

        // Sort each day's slots by start time
        foreach (var dayName in DayNames)
        {
            timetable[dayName] = timetable[dayName].OrderBy(s => s.StartTime).ToList();
        }

        var response = new TimetableResponse
        {
            SectionId = sectionId,
            SectionName = section.Name,
            Timetable = timetable
        };

        return ApiResponse<TimetableResponse>.SuccessResponse(response);
    }

    public async Task<ApiResponse<List<SectionTeacherDto>>> GetSectionTeachersAsync(int sectionId)
    {
        // Check if section exists
        var sectionExists = await _context.Sections.AnyAsync(s => s.Id == sectionId);
        if (!sectionExists)
        {
            return ApiResponse<List<SectionTeacherDto>>.ErrorResponse(ErrorCodes.NotFound, "Section not found.");
        }

        var teachers = await _context.SectionTeachers
            .Include(st => st.Teacher)
            .Where(st => st.SectionId == sectionId)
            .Select(st => new SectionTeacherDto
            {
                TeacherId = st.TeacherId,
                EmployeeNumber = st.Teacher != null ? st.Teacher.EmployeeNumber : string.Empty,
                FirstName = st.Teacher != null ? st.Teacher.FirstName : string.Empty,
                LastName = st.Teacher != null ? st.Teacher.LastName : string.Empty,
                MiddleName = st.Teacher != null ? st.Teacher.MiddleName : null,
                Department = st.Teacher != null ? st.Teacher.Department : string.Empty,
                AssignedAt = st.AssignedAt
            })
            .ToListAsync();

        return ApiResponse<List<SectionTeacherDto>>.SuccessResponse(teachers);
    }

    public async Task<ApiResponse<SectionTeacherDto>> AssignTeacherToSectionAsync(int sectionId, AssignTeacherRequest request)
    {
        // Check if section exists
        var section = await _context.Sections.FindAsync(sectionId);
        if (section == null)
        {
            return ApiResponse<SectionTeacherDto>.ErrorResponse(ErrorCodes.NotFound, "Section not found.");
        }

        // Check if teacher exists and is active
        var teacher = await _context.Teachers.FindAsync(request.TeacherId);
        if (teacher == null)
        {
            return ApiResponse<SectionTeacherDto>.ErrorResponse(ErrorCodes.NotFound, "Teacher not found.");
        }

        if (!teacher.IsActive)
        {
            return ApiResponse<SectionTeacherDto>.ErrorResponse(ErrorCodes.ValidationError, "Cannot assign an inactive teacher.");
        }

        // Check if assignment already exists
        var existingAssignment = await _context.SectionTeachers
            .AnyAsync(st => st.SectionId == sectionId && st.TeacherId == request.TeacherId);

        if (existingAssignment)
        {
            return ApiResponse<SectionTeacherDto>.ErrorResponse(ErrorCodes.DuplicateAssignment, "Teacher is already assigned to this section.");
        }

        var sectionTeacher = new SectionTeacher
        {
            SectionId = sectionId,
            TeacherId = request.TeacherId,
            AssignedAt = DateTimeOffset.UtcNow
        };

        _context.SectionTeachers.Add(sectionTeacher);
        await _context.SaveChangesAsync();

        var dto = new SectionTeacherDto
        {
            TeacherId = teacher.Id,
            EmployeeNumber = teacher.EmployeeNumber,
            FirstName = teacher.FirstName,
            LastName = teacher.LastName,
            MiddleName = teacher.MiddleName,
            Department = teacher.Department,
            AssignedAt = sectionTeacher.AssignedAt
        };

        return ApiResponse<SectionTeacherDto>.SuccessResponse(dto);
    }

    public async Task<ApiResponse<bool>> RemoveTeacherFromSectionAsync(int sectionId, int teacherId, bool isAdmin = false, bool removeOwnedSchedules = false)
    {
        var sectionTeacher = await _context.SectionTeachers
            .FirstOrDefaultAsync(st => st.SectionId == sectionId && st.TeacherId == teacherId);

        if (sectionTeacher == null)
        {
            return ApiResponse<bool>.ErrorResponse(ErrorCodes.NotFound, "Teacher assignment not found.");
        }

        var teacherSchedulesQuery = _context.Schedules
            .Where(schedule => schedule.SectionId == sectionId && schedule.TeacherId == teacherId);

        if (removeOwnedSchedules)
        {
            var ownedScheduleIds = await teacherSchedulesQuery
                .Select(schedule => schedule.Id)
                .ToListAsync();

            if (ownedScheduleIds.Count > 0)
            {
                var hasAttendance = await _context.Attendances
                    .AnyAsync(attendance => ownedScheduleIds.Contains(attendance.ScheduleId));

                if (hasAttendance)
                {
                    return ApiResponse<bool>.ErrorResponse(
                        ErrorCodes.Conflict,
                        "Cannot unassign because one or more of your schedules already have attendance records.");
                }

                var ownedSchedules = await teacherSchedulesQuery.ToListAsync();
                _context.Schedules.RemoveRange(ownedSchedules);
            }
        }

        // Non-admin users cannot remove teachers from sections that have schedules
        if (!isAdmin && !removeOwnedSchedules)
        {
            var hasSchedules = await _context.Schedules.AnyAsync(s => s.SectionId == sectionId);
            if (hasSchedules)
            {
                return ApiResponse<bool>.ErrorResponse(ErrorCodes.TeacherHasSchedules, "Cannot remove teacher from section with existing schedules.");
            }
        }

        _context.SectionTeachers.Remove(sectionTeacher);
        await _context.SaveChangesAsync();

        return ApiResponse<bool>.SuccessResponse(true);
    }

    // Retrieves sections filtered by course and year level for enrollment purposes
    public async Task<ApiResponse<List<SectionDto>>> GetSectionsByCourseAndYearLevelAsync(int courseId, int yearLevel, int? academicYearId = null)
    {
        var query = _context.Sections
            .Include(s => s.AcademicYear)
            .Include(s => s.Course)
            .Include(s => s.Subject)
            .Include(s => s.Classroom)
            .Where(s => s.CourseId == courseId && s.YearLevel == yearLevel);

        if (academicYearId.HasValue)
        {
            query = query.Where(s => s.AcademicYearId == academicYearId.Value);
        }

        var sections = await query
            .OrderBy(s => s.Name)
            .ToListAsync();

        var dtos = new List<SectionDto>();
        foreach (var section in sections)
        {
            var dto = await BuildSectionDtoAsync(section);
            dtos.Add(dto);
        }

        return ApiResponse<List<SectionDto>>.SuccessResponse(dtos);
    }

    // Calculates the capacity status based on enrollment count
    private SectionCapacityStatus CalculateCapacityStatus(int currentCount)
    {
        if (currentCount >= _enrollmentSettings.OverCapacityLimit)
        {
            return SectionCapacityStatus.OverCapacity;
        }
        else if (currentCount >= _enrollmentSettings.WarningThreshold)
        {
            return SectionCapacityStatus.AtWarning;
        }
        return SectionCapacityStatus.Available;
    }
}
