using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Attendance_Management_System.Backend.Services;

// Service for managing course CRUD operations
public class CoursesService : ICoursesService
{
    private readonly AppDbContext _context;

    public CoursesService(AppDbContext context)
    {
        _context = context;
    }

    // Retrieves all courses ordered alphabetically by name
    public async Task<ApiResponse<List<CourseDto>>> GetAllCoursesAsync()
    {
        // Project directly to DTO to avoid loading full entities into memory
        var courses = await _context.Courses
            .OrderBy(c => c.Name)
            .Select(c => new CourseDto
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
                Description = c.Description,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        return ApiResponse<List<CourseDto>>.SuccessResponse(courses);
    }

    // Retrieves a single course by its ID, returns error if not found
    public async Task<ApiResponse<CourseDto>> GetCourseByIdAsync(int id)
    {
        var course = await _context.Courses.FindAsync(id);

        if (course == null)
        {
            return ApiResponse<CourseDto>.ErrorResponse("NOT_FOUND", "Course not found.");
        }

        // Map entity to response DTO
        var dto = new CourseDto
        {
            Id = course.Id,
            Name = course.Name,
            Code = course.Code,
            Description = course.Description,
            CreatedAt = course.CreatedAt
        };

        return ApiResponse<CourseDto>.SuccessResponse(dto);
    }

    // Creates a new course after validating that the course code is unique
    public async Task<ApiResponse<CourseDto>> CreateCourseAsync(CreateCourseRequest request)
    {
        // Enforce unique course code constraint
        var codeExists = await _context.Courses.AnyAsync(c => c.Code == request.Code);
        if (codeExists)
        {
            return ApiResponse<CourseDto>.ErrorResponse("VALIDATION_ERROR", "A course with this code already exists.");
        }

        var course = new Course
        {
            Name = request.Name,
            Code = request.Code,
            Description = request.Description
        };

        _context.Courses.Add(course);
        await _context.SaveChangesAsync();

        // Map saved entity back to DTO (includes generated Id and CreatedAt)
        var dto = new CourseDto
        {
            Id = course.Id,
            Name = course.Name,
            Code = course.Code,
            Description = course.Description,
            CreatedAt = course.CreatedAt
        };

        return ApiResponse<CourseDto>.SuccessResponse(dto);
    }

    // Updates an existing course, only modifying fields that are provided in the request
    public async Task<ApiResponse<CourseDto>> UpdateCourseAsync(int id, UpdateCourseRequest request)
    {
        var course = await _context.Courses.FindAsync(id);

        if (course == null)
        {
            return ApiResponse<CourseDto>.ErrorResponse("NOT_FOUND", "Course not found.");
        }

        // Validate uniqueness only when the code is actually being changed
        if (!string.IsNullOrEmpty(request.Code) && request.Code != course.Code)
        {
            var codeExists = await _context.Courses.AnyAsync(c => c.Code == request.Code && c.Id != id);
            if (codeExists)
            {
                return ApiResponse<CourseDto>.ErrorResponse("VALIDATION_ERROR", "A course with this code already exists.");
            }
        }

        // Apply partial updates - only non-null/non-empty fields are modified
        if (!string.IsNullOrEmpty(request.Name))
            course.Name = request.Name;
        if (!string.IsNullOrEmpty(request.Code))
            course.Code = request.Code;
        // Description can be explicitly set to empty, so check for null only
        if (request.Description != null)
            course.Description = request.Description;

        await _context.SaveChangesAsync();

        // Return updated course data
        var dto = new CourseDto
        {
            Id = course.Id,
            Name = course.Name,
            Code = course.Code,
            Description = course.Description,
            CreatedAt = course.CreatedAt
        };

        return ApiResponse<CourseDto>.SuccessResponse(dto);
    }

    // Deletes a course only if it has no dependent subjects or students
    public async Task<ApiResponse<bool>> DeleteCourseAsync(int id)
    {
        var course = await _context.Courses.FindAsync(id);

        if (course == null)
        {
            return ApiResponse<bool>.ErrorResponse("NOT_FOUND", "Course not found.");
        }

        // Enforce referential integrity - prevent deletion if subjects exist
        var hasSubjects = await _context.Subjects.AnyAsync(s => s.CourseId == id);
        if (hasSubjects)
        {
            return ApiResponse<bool>.ErrorResponse("IN_USE", "Cannot delete course that has subjects assigned.");
        }

        // Enforce referential integrity - prevent deletion if students are enrolled
        var hasStudents = await _context.Students.AnyAsync(s => s.CourseId == id);
        if (hasStudents)
        {
            return ApiResponse<bool>.ErrorResponse("IN_USE", "Cannot delete course that has students enrolled.");
        }

        _context.Courses.Remove(course);
        await _context.SaveChangesAsync();

        return ApiResponse<bool>.SuccessResponse(true);
    }
}