using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Attendance_Management_System.Backend.Services;

public class CoursesService : ICoursesService
{
    private readonly AppDbContext _context;

    public CoursesService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<CourseDto>>> GetAllCoursesAsync()
    {
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

    public async Task<ApiResponse<CourseDto>> GetCourseByIdAsync(int id)
    {
        var course = await _context.Courses.FindAsync(id);

        if (course == null)
        {
            return ApiResponse<CourseDto>.ErrorResponse("NOT_FOUND", "Course not found.");
        }

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

    public async Task<ApiResponse<CourseDto>> CreateCourseAsync(CreateCourseRequest request)
    {
        // Check if code already exists
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

    public async Task<ApiResponse<CourseDto>> UpdateCourseAsync(int id, UpdateCourseRequest request)
    {
        var course = await _context.Courses.FindAsync(id);

        if (course == null)
        {
            return ApiResponse<CourseDto>.ErrorResponse("NOT_FOUND", "Course not found.");
        }

        // Check if new code already exists (if code is being changed)
        if (!string.IsNullOrEmpty(request.Code) && request.Code != course.Code)
        {
            var codeExists = await _context.Courses.AnyAsync(c => c.Code == request.Code && c.Id != id);
            if (codeExists)
            {
                return ApiResponse<CourseDto>.ErrorResponse("VALIDATION_ERROR", "A course with this code already exists.");
            }
        }

        if (!string.IsNullOrEmpty(request.Name))
            course.Name = request.Name;
        if (!string.IsNullOrEmpty(request.Code))
            course.Code = request.Code;
        if (request.Description != null)
            course.Description = request.Description;

        await _context.SaveChangesAsync();

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

    public async Task<ApiResponse<bool>> DeleteCourseAsync(int id)
    {
        var course = await _context.Courses.FindAsync(id);

        if (course == null)
        {
            return ApiResponse<bool>.ErrorResponse("NOT_FOUND", "Course not found.");
        }

        // Check if course has subjects
        var hasSubjects = await _context.Subjects.AnyAsync(s => s.CourseId == id);
        if (hasSubjects)
        {
            return ApiResponse<bool>.ErrorResponse("IN_USE", "Cannot delete course that has subjects assigned.");
        }

        // Check if course has students
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