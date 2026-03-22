using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Attendance_Management_System.Backend.Services;

public class TeachersService : ITeachersService
{
    private readonly AppDbContext _context;

    public TeachersService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ApiResponse<List<TeacherDto>>> GetAllTeachersAsync()
    {
        var teachers = await _context.Teachers
            .Include(t => t.User)
            .Where(t => t.IsActive)
            .OrderBy(t => t.LastName)
            .Select(t => new TeacherDto
            {
                Id = t.Id,
                UserId = t.UserId,
                Email = t.User != null ? t.User.Email ?? string.Empty : string.Empty,
                EmployeeNumber = t.EmployeeNumber,
                FirstName = t.FirstName,
                LastName = t.LastName,
                MiddleName = t.MiddleName,
                Department = t.Department,
                Specialization = t.Specialization,
                IsActive = t.IsActive,
                CreatedAt = t.CreatedAt
            })
            .ToListAsync();

        return ApiResponse<List<TeacherDto>>.SuccessResponse(teachers);
    }

    public async Task<ApiResponse<TeacherDto>> GetTeacherByIdAsync(int id)
    {
        var teacher = await _context.Teachers
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (teacher == null)
        {
            return ApiResponse<TeacherDto>.ErrorResponse("NOT_FOUND", "Teacher not found.");
        }

        var dto = new TeacherDto
        {
            Id = teacher.Id,
            UserId = teacher.UserId,
            Email = teacher.User?.Email ?? string.Empty,
            EmployeeNumber = teacher.EmployeeNumber,
            FirstName = teacher.FirstName,
            LastName = teacher.LastName,
            MiddleName = teacher.MiddleName,
            Department = teacher.Department,
            Specialization = teacher.Specialization,
            IsActive = teacher.IsActive,
            CreatedAt = teacher.CreatedAt
        };

        return ApiResponse<TeacherDto>.SuccessResponse(dto);
    }

    public async Task<ApiResponse<TeacherDto>> UpdateTeacherAsync(int id, UpdateTeacherRequest request)
    {
        var teacher = await _context.Teachers
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (teacher == null)
        {
            return ApiResponse<TeacherDto>.ErrorResponse("NOT_FOUND", "Teacher not found.");
        }

        // Check if employee number is being changed and if it already exists
        if (!string.IsNullOrEmpty(request.EmployeeNumber) && request.EmployeeNumber != teacher.EmployeeNumber)
        {
            var employeeNumberExists = await _context.Teachers
                .AnyAsync(t => t.EmployeeNumber == request.EmployeeNumber && t.Id != id);
            if (employeeNumberExists)
            {
                return ApiResponse<TeacherDto>.ErrorResponse("VALIDATION_ERROR", "A teacher with this employee number already exists.");
            }
        }

        if (!string.IsNullOrEmpty(request.EmployeeNumber))
            teacher.EmployeeNumber = request.EmployeeNumber;
        if (!string.IsNullOrEmpty(request.FirstName))
            teacher.FirstName = request.FirstName;
        if (!string.IsNullOrEmpty(request.LastName))
            teacher.LastName = request.LastName;
        if (request.MiddleName != null)
            teacher.MiddleName = request.MiddleName;
        if (!string.IsNullOrEmpty(request.Department))
            teacher.Department = request.Department;
        if (request.Specialization != null)
            teacher.Specialization = request.Specialization;

        await _context.SaveChangesAsync();

        var dto = new TeacherDto
        {
            Id = teacher.Id,
            UserId = teacher.UserId,
            Email = teacher.User?.Email ?? string.Empty,
            EmployeeNumber = teacher.EmployeeNumber,
            FirstName = teacher.FirstName,
            LastName = teacher.LastName,
            MiddleName = teacher.MiddleName,
            Department = teacher.Department,
            Specialization = teacher.Specialization,
            IsActive = teacher.IsActive,
            CreatedAt = teacher.CreatedAt
        };

        return ApiResponse<TeacherDto>.SuccessResponse(dto);
    }

    public async Task<ApiResponse<bool>> DeactivateTeacherAsync(int id)
    {
        var teacher = await _context.Teachers.FindAsync(id);

        if (teacher == null)
        {
            return ApiResponse<bool>.ErrorResponse("NOT_FOUND", "Teacher not found.");
        }

        teacher.IsActive = false;
        await _context.SaveChangesAsync();

        return ApiResponse<bool>.SuccessResponse(true);
    }

    public async Task<ApiResponse<bool>> ActivateTeacherAsync(int id)
    {
        var teacher = await _context.Teachers.FindAsync(id);

        if (teacher == null)
        {
            return ApiResponse<bool>.ErrorResponse("NOT_FOUND", "Teacher not found.");
        }

        teacher.IsActive = true;
        await _context.SaveChangesAsync();

        return ApiResponse<bool>.SuccessResponse(true);
    }
}