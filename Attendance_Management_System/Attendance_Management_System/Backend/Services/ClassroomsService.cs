using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Attendance_Management_System.Backend.Services;

public class ClassroomsService : IClassroomsService
{
    private readonly AppDbContext _context;

    public ClassroomsService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<ClassroomDto>>> GetAllClassroomsAsync()
    {
        var classrooms = await _context.Classrooms
            .OrderBy(c => c.Name)
            .Select(c => new ClassroomDto
            {
                Id = c.Id,
                Name = c.Name,
                Description = c.Description,
                CreatedAt = c.CreatedAt
            })
            .ToListAsync();

        return ApiResponse<List<ClassroomDto>>.SuccessResponse(classrooms);
    }

    public async Task<ApiResponse<ClassroomDto>> GetClassroomByIdAsync(int id)
    {
        var classroom = await _context.Classrooms.FindAsync(id);

        if (classroom == null)
        {
            return ApiResponse<ClassroomDto>.ErrorResponse("NOT_FOUND", "Classroom not found.");
        }

        var dto = new ClassroomDto
        {
            Id = classroom.Id,
            Name = classroom.Name,
            Description = classroom.Description,
            CreatedAt = classroom.CreatedAt
        };

        return ApiResponse<ClassroomDto>.SuccessResponse(dto);
    }

    public async Task<ApiResponse<ClassroomDto>> CreateClassroomAsync(CreateClassroomRequest request)
    {
        var classroom = new Classroom
        {
            Name = request.Name,
            Description = request.Description
        };

        _context.Classrooms.Add(classroom);
        await _context.SaveChangesAsync();

        var dto = new ClassroomDto
        {
            Id = classroom.Id,
            Name = classroom.Name,
            Description = classroom.Description,
            CreatedAt = classroom.CreatedAt
        };

        return ApiResponse<ClassroomDto>.SuccessResponse(dto);
    }

    public async Task<ApiResponse<ClassroomDto>> UpdateClassroomAsync(int id, UpdateClassroomRequest request)
    {
        var classroom = await _context.Classrooms.FindAsync(id);

        if (classroom == null)
        {
            return ApiResponse<ClassroomDto>.ErrorResponse("NOT_FOUND", "Classroom not found.");
        }

        if (!string.IsNullOrEmpty(request.Name))
            classroom.Name = request.Name;
        if (request.Description != null)
            classroom.Description = request.Description;

        await _context.SaveChangesAsync();

        var dto = new ClassroomDto
        {
            Id = classroom.Id,
            Name = classroom.Name,
            Description = classroom.Description,
            CreatedAt = classroom.CreatedAt
        };

        return ApiResponse<ClassroomDto>.SuccessResponse(dto);
    }

    public async Task<ApiResponse<bool>> DeleteClassroomAsync(int id)
    {
        var classroom = await _context.Classrooms.FindAsync(id);

        if (classroom == null)
        {
            return ApiResponse<bool>.ErrorResponse("NOT_FOUND", "Classroom not found.");
        }

        // Check if classroom is in use by any section
        var isInUse = await _context.Sections.AnyAsync(s => s.ClassroomId == id);
        if (isInUse)
        {
            return ApiResponse<bool>.ErrorResponse("IN_USE", "Cannot delete classroom that is assigned to sections.");
        }

        _context.Classrooms.Remove(classroom);
        await _context.SaveChangesAsync();

        return ApiResponse<bool>.SuccessResponse(true);
    }
}