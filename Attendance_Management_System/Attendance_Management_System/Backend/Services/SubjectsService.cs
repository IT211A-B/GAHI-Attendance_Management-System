using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Attendance_Management_System.Backend.Services;

public class SubjectsService : ISubjectsService
{
    private readonly AppDbContext _context;

    public SubjectsService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<SubjectDto>> GetAllSubjectsAsync()
    {
        var subjects = await _context.Subjects
            .Include(s => s.Course)
            .OrderBy(s => s.Name)
            .Select(s => new SubjectDto
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                CourseId = s.CourseId,
                CourseName = s.Course != null ? s.Course.Name : null,
                Units = s.Units,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();

        return subjects;
    }

    public async Task<SubjectDto> GetSubjectByIdAsync(int id)
    {
        var subject = await _context.Subjects
            .Include(s => s.Course)
            .FirstOrDefaultAsync(s => s.Id == id);

        if (subject == null)
        {
            throw new KeyNotFoundException("Subject not found.");
        }

        var dto = new SubjectDto
        {
            Id = subject.Id,
            Name = subject.Name,
            Code = subject.Code,
            CourseId = subject.CourseId,
            CourseName = subject.Course?.Name,
            Units = subject.Units,
            CreatedAt = subject.CreatedAt
        };

        return dto;
    }

    public async Task<List<SubjectDto>> GetSubjectsByCourseIdAsync(int courseId)
    {
        var courseExists = await _context.Courses.AnyAsync(c => c.Id == courseId);
        if (!courseExists)
        {
            throw new KeyNotFoundException("Course not found.");
        }

        var subjects = await _context.Subjects
            .Include(s => s.Course)
            .Where(s => s.CourseId == courseId)
            .OrderBy(s => s.Name)
            .Select(s => new SubjectDto
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                CourseId = s.CourseId,
                CourseName = s.Course != null ? s.Course.Name : null,
                Units = s.Units,
                CreatedAt = s.CreatedAt
            })
            .ToListAsync();

        return subjects;
    }

    public async Task<SubjectDto> CreateSubjectAsync(CreateSubjectRequest request)
    {
        // Validate course exists
        var courseExists = await _context.Courses.AnyAsync(c => c.Id == request.CourseId);
        if (!courseExists)
        {
            throw new InvalidOperationException("Course not found.");
        }

        // Check if code already exists
        var codeExists = await _context.Subjects.AnyAsync(s => s.Code == request.Code);
        if (codeExists)
        {
            throw new InvalidOperationException("A subject with this code already exists.");
        }

        var subject = new Subject
        {
            Name = request.Name,
            Code = request.Code,
            CourseId = request.CourseId,
            Units = request.Units
        };

        _context.Subjects.Add(subject);
        await _context.SaveChangesAsync();

        var course = await _context.Courses.FindAsync(request.CourseId);

        var dto = new SubjectDto
        {
            Id = subject.Id,
            Name = subject.Name,
            Code = subject.Code,
            CourseId = subject.CourseId,
            CourseName = course?.Name,
            Units = subject.Units,
            CreatedAt = subject.CreatedAt
        };

        return dto;
    }

    public async Task<SubjectDto> UpdateSubjectAsync(int id, UpdateSubjectRequest request)
    {
        var subject = await _context.Subjects.FindAsync(id);

        if (subject == null)
        {
            throw new KeyNotFoundException("Subject not found.");
        }

        // Validate course exists if being changed
        if (request.CourseId.HasValue && request.CourseId != subject.CourseId)
        {
            var courseExists = await _context.Courses.AnyAsync(c => c.Id == request.CourseId.Value);
            if (!courseExists)
            {
                throw new InvalidOperationException("Course not found.");
            }
        }

        // Check if new code already exists (if code is being changed)
        if (!string.IsNullOrEmpty(request.Code) && request.Code != subject.Code)
        {
            var codeExists = await _context.Subjects.AnyAsync(s => s.Code == request.Code && s.Id != id);
            if (codeExists)
            {
                throw new InvalidOperationException("A subject with this code already exists.");
            }
        }

        if (!string.IsNullOrEmpty(request.Name))
            subject.Name = request.Name;
        if (!string.IsNullOrEmpty(request.Code))
            subject.Code = request.Code;
        if (request.CourseId.HasValue)
            subject.CourseId = request.CourseId.Value;
        if (request.Units.HasValue)
            subject.Units = request.Units.Value;

        await _context.SaveChangesAsync();

        var course = await _context.Courses.FindAsync(subject.CourseId);

        var dto = new SubjectDto
        {
            Id = subject.Id,
            Name = subject.Name,
            Code = subject.Code,
            CourseId = subject.CourseId,
            CourseName = course?.Name,
            Units = subject.Units,
            CreatedAt = subject.CreatedAt
        };

        return dto;
    }

    public async Task DeleteSubjectAsync(int id, int? replacementSubjectId = null)
    {
        var subject = await _context.Subjects.FindAsync(id);

        if (subject == null)
        {
            throw new KeyNotFoundException("Subject not found.");
        }

        var sectionsInUseCount = await _context.Sections.CountAsync(s => s.SubjectId == id);
        var schedulesInUseCount = await _context.Schedules.CountAsync(s => s.SubjectId == id);
        var hasDependencies = sectionsInUseCount > 0 || schedulesInUseCount > 0;

        if (hasDependencies)
        {
            Subject? resolvedReplacementSubject;

            if (replacementSubjectId.HasValue && replacementSubjectId.Value > 0)
            {
                if (replacementSubjectId.Value == id)
                {
                    throw new InvalidOperationException("Replacement subject must be different from the subject being deleted.");
                }

                resolvedReplacementSubject = await _context.Subjects.FindAsync(replacementSubjectId.Value);
                if (resolvedReplacementSubject == null)
                {
                    throw new KeyNotFoundException("Replacement subject not found.");
                }
            }
            else
            {
                resolvedReplacementSubject = await _context.Subjects
                    .Where(s => s.Id != id && s.CourseId == subject.CourseId)
                    .OrderBy(s => s.Name)
                    .ThenBy(s => s.Code)
                    .FirstOrDefaultAsync();

                if (resolvedReplacementSubject == null)
                {
                    resolvedReplacementSubject = await _context.Subjects
                        .Where(s => s.Id != id)
                        .OrderBy(s => s.Name)
                        .ThenBy(s => s.Code)
                        .FirstOrDefaultAsync();
                }

                if (resolvedReplacementSubject == null)
                {
                    throw new InvalidOperationException($"Cannot delete subject because it is used by {sectionsInUseCount} section(s) and {schedulesInUseCount} schedule slot(s), and no fallback subject is available. Create another subject first or select a replacement.");
                }
            }

            var sectionsToUpdate = await _context.Sections
                .Where(section => section.SubjectId == id)
                .ToListAsync();

            foreach (var section in sectionsToUpdate)
            {
                section.SubjectId = resolvedReplacementSubject.Id;
            }

            var schedulesToUpdate = await _context.Schedules
                .Where(schedule => schedule.SubjectId == id)
                .ToListAsync();

            foreach (var schedule in schedulesToUpdate)
            {
                schedule.SubjectId = resolvedReplacementSubject.Id;
            }
        }

        _context.Subjects.Remove(subject);
        await _context.SaveChangesAsync();

        return;
    }
}


