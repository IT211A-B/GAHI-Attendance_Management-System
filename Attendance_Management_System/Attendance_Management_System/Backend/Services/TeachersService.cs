using Attendance_Management_System.Backend.Constants;
using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Attendance_Management_System.Backend.Services;

// Service handling all teacher-related business logic
public class TeachersService : ITeachersService
{
    private readonly AppDbContext _context;

    // Inject database context through constructor
    public TeachersService(AppDbContext context)
    {
        _context = context;
    }

    // Retrieves all active teachers from the database
    public async Task<ApiResponse<List<TeacherDto>>> GetAllTeachersAsync()
    {
        // Query only active teachers, include related user data for email
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

    public async Task<ApiResponse<TeacherDto>> GetTeacherByUserIdAsync(int userId)
    {
        var teacher = await _context.Teachers
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.UserId == userId && t.IsActive);

        if (teacher == null)
        {
            return ApiResponse<TeacherDto>.ErrorResponse(ErrorCodes.NotFound, "Teacher profile not found.");
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

    // Retrieves a single teacher by their ID
    public async Task<ApiResponse<TeacherDto>> GetTeacherByIdAsync(int id)
    {
        var teacher = await _context.Teachers
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == id);

        // Return error if teacher doesn't exist
        if (teacher == null)
        {
            return ApiResponse<TeacherDto>.ErrorResponse("NOT_FOUND", "Teacher not found.");
        }

        // Map entity to DTO for response
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

    // Updates an existing teacher's information
    public async Task<ApiResponse<TeacherDto>> UpdateTeacherAsync(int id, UpdateTeacherRequest request)
    {
        var teacher = await _context.Teachers
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (teacher == null)
        {
            return ApiResponse<TeacherDto>.ErrorResponse("NOT_FOUND", "Teacher not found.");
        }

        // Ensure employee number uniqueness if it's being changed
        if (!string.IsNullOrEmpty(request.EmployeeNumber) && request.EmployeeNumber != teacher.EmployeeNumber)
        {
            var employeeNumberExists = await _context.Teachers
                .AnyAsync(t => t.EmployeeNumber == request.EmployeeNumber && t.Id != id);
            if (employeeNumberExists)
            {
                return ApiResponse<TeacherDto>.ErrorResponse("VALIDATION_ERROR", "A teacher with this employee number already exists.");
            }
        }

        // Update only provided fields (partial update support)
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

    // Soft deletes a teacher by marking them as inactive
    public async Task<ApiResponse<bool>> DeactivateTeacherAsync(int id)
    {
        var teacher = await _context.Teachers.FindAsync(id);

        if (teacher == null)
        {
            return ApiResponse<bool>.ErrorResponse("NOT_FOUND", "Teacher not found.");
        }

        // Mark as inactive instead of hard delete
        teacher.IsActive = false;
        await _context.SaveChangesAsync();

        return ApiResponse<bool>.SuccessResponse(true);
    }

    // Reactivates a previously deactivated teacher
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

    // Retrieves all teachers with their assigned sections
    public async Task<ApiResponse<List<TeacherListDto>>> GetAllTeachersWithSectionsAsync()
    {
        // Include section assignments through the bridge table
        var teachers = await _context.Teachers
            .Include(t => t.User)
            .Include(t => t.SectionTeachers)
                .ThenInclude(st => st.Section)
            .OrderBy(t => t.LastName)
            .Select(t => new TeacherListDto
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
                Sections = t.SectionTeachers.Select(st => new SectionSummaryDto
                {
                    SectionId = st.SectionId,
                    SectionName = st.Section != null ? st.Section.Name : string.Empty
                }).ToList()
            })
            .ToListAsync();

        return ApiResponse<List<TeacherListDto>>.SuccessResponse(teachers);
    }

    // Creates a new teacher profile for an existing user
    public async Task<ApiResponse<TeacherDto>> CreateTeacherAsync(CreateTeacherRequest request)
    {
        // Validate that the user exists
        var user = await _context.Users.FindAsync(request.UserId);
        if (user == null)
        {
            return ApiResponse<TeacherDto>.ErrorResponse(ErrorCodes.NotFound, "User not found.");
        }

        // Ensure user has the teacher role
        if (user.Role != "teacher")
        {
            return ApiResponse<TeacherDto>.ErrorResponse(ErrorCodes.ValidationError, "User does not have the teacher role.");
        }

        // Prevent duplicate teacher profiles for the same user
        var existingTeacher = await _context.Teachers.AnyAsync(t => t.UserId == request.UserId);
        if (existingTeacher)
        {
            return ApiResponse<TeacherDto>.ErrorResponse(ErrorCodes.AlreadyExists, "Teacher profile already exists for this user.");
        }

        // Ensure employee number is unique
        var employeeNumberExists = await _context.Teachers.AnyAsync(t => t.EmployeeNumber == request.EmployeeNumber);
        if (employeeNumberExists)
        {
            return ApiResponse<TeacherDto>.ErrorResponse(ErrorCodes.Conflict, "A teacher with this employee number already exists.");
        }

        // Create and persist the new teacher entity
        var teacher = new Teacher
        {
            UserId = request.UserId,
            EmployeeNumber = request.EmployeeNumber,
            FirstName = request.FirstName,
            LastName = request.LastName,
            MiddleName = request.MiddleName,
            Department = request.Department,
            Specialization = request.Specialization,
            IsActive = true
        };

        _context.Teachers.Add(teacher);
        await _context.SaveChangesAsync();

        var dto = new TeacherDto
        {
            Id = teacher.Id,
            UserId = teacher.UserId,
            Email = user.Email ?? string.Empty,
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
}
