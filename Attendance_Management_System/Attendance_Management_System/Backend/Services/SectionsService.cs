using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Attendance_Management_System.Backend.Services;

public class SectionsService : ISectionsService
{
    private readonly AppDbContext _context;

    public SectionsService(AppDbContext context)
    {
        _context = context;
    }

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

    public async Task<ApiResponse<SectionDto>> GetSectionByIdAsync(int id)
    {
        var section = await _context.Sections
            .Include(s => s.AcademicYear)
            .Include(s => s.Course)
            .Include(s => s.Subject)
            .Include(s => s.Classroom)
            .FirstOrDefaultAsync(s => s.Id == id);

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

    public async Task<ApiResponse<List<SectionDto>>> GetSectionsByAcademicYearIdAsync(int academicYearId)
    {
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

        // Validate subject exists
        var subjectExists = await _context.Subjects.AnyAsync(s => s.Id == request.SubjectId);
        if (!subjectExists)
        {
            return ApiResponse<SectionDto>.ErrorResponse("VALIDATION_ERROR", "Subject not found.");
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
            return ApiResponse<bool>.ErrorResponse("IN_USE", "Cannot delete section that has enrollments.");
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

        return new SectionDto
        {
            Id = section.Id,
            Name = section.Name,
            AcademicYearId = section.AcademicYearId,
            AcademicYearLabel = academicYear?.YearLabel,
            CourseId = section.CourseId,
            CourseName = course?.Name,
            SubjectId = section.SubjectId,
            SubjectName = subject?.Name,
            ClassroomId = section.ClassroomId,
            ClassroomName = classroom?.Name,
            CreatedAt = section.CreatedAt
        };
    }
}