using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Interfaces.Services;
using Attendance_Management_System.Backend.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Attendance_Management_System.Backend.Services;

// Service for user management operations: CRUD and profile updates
public class UsersService : IUsersService
{
    private readonly UserManager<User> _userManager;
    private readonly AppDbContext _context;

    public UsersService(UserManager<User> userManager, AppDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    // Retrieves all active users with their role-specific profiles using batch loading
    public async Task<ApiResponse<List<UserDto>>> GetAllUsersAsync()
    {
        // Load all active users first
        var users = await _userManager.Users
            .Where(u => u.IsActive)
            .OrderBy(u => u.Email)
            .ToListAsync();

        // Extract user IDs for batch loading related entities
        var userIds = users.Select(u => u.Id).ToHashSet();

        // Batch load students with courses to avoid N+1 query problem
        var students = await _context.Students
            .Include(s => s.Course)
            .Where(s => userIds.Contains(s.UserId))
            .ToDictionaryAsync(s => s.UserId);

        // Batch load teachers for the same optimization
        var teachers = await _context.Teachers
            .Where(t => userIds.Contains(t.UserId))
            .ToDictionaryAsync(t => t.UserId);

        // Map users to DTOs with role-specific profile data
        var userDtos = users.Select(user =>
        {
            var userDto = new UserDto
            {
                Id = user.Id,
                Email = user.Email ?? string.Empty,
                Role = user.Role,
                IsActive = user.IsActive
            };

            // Attach student profile data if user is a student
            if (user.Role == "student" && students.TryGetValue(user.Id, out var student))
            {
                userDto.StudentNumber = student.StudentNumber;
                userDto.FirstName = student.FirstName;
                userDto.LastName = student.LastName;
                userDto.MiddleName = student.MiddleName;
                userDto.CourseId = student.CourseId;
                userDto.CourseName = student.Course?.Name;
                userDto.YearLevel = student.YearLevel;
            }
            // Attach teacher profile data if user is a teacher
            else if (user.Role == "teacher" && teachers.TryGetValue(user.Id, out var teacher))
            {
                userDto.EmployeeNumber = teacher.EmployeeNumber;
                userDto.FirstName = teacher.FirstName;
                userDto.LastName = teacher.LastName;
                userDto.MiddleName = teacher.MiddleName;
                userDto.Department = teacher.Department;
                userDto.Specialization = teacher.Specialization;
            }

            return userDto;
        }).ToList();

        return ApiResponse<List<UserDto>>.SuccessResponse(userDtos);
    }

    // Retrieves a single user by ID with role-specific profile
    public async Task<ApiResponse<UserDto>> GetUserByIdAsync(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        // Only return active users
        if (user == null || !user.IsActive)
        {
            return ApiResponse<UserDto>.ErrorResponse("NOT_FOUND", "User not found.");
        }

        var userDto = await BuildUserDtoAsync(user);
        return ApiResponse<UserDto>.SuccessResponse(userDto);
    }

    // Creates a new user account with specified role (admin only)
    public async Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserRequest request)
    {
        // Prevent duplicate email registration
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            return ApiResponse<UserDto>.ErrorResponse("VALIDATION_ERROR", "An account with this email already exists.");
        }

        // Validate that role is one of the allowed values
        var validRoles = new[] { "admin", "teacher", "student" };
        if (!validRoles.Contains(request.Role.ToLower()))
        {
            return ApiResponse<UserDto>.ErrorResponse("VALIDATION_ERROR", "Invalid role. Must be admin, teacher, or student.");
        }

        // Create user with Identity for secure password hashing
        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            Role = request.Role.ToLower(),
            IsActive = true,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return ApiResponse<UserDto>.ErrorResponse("CREATE_FAILED", $"Failed to create user: {errors}");
        }

        var userDto = await BuildUserDtoAsync(user);
        return ApiResponse<UserDto>.SuccessResponse(userDto);
    }

    // Updates user email, role, or active status
    public async Task<ApiResponse<UserDto>> UpdateUserAsync(int id, UpdateUserRequest request)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        if (user == null)
        {
            return ApiResponse<UserDto>.ErrorResponse("NOT_FOUND", "User not found.");
        }

        // Update email if provided and not already in use
        if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                return ApiResponse<UserDto>.ErrorResponse("VALIDATION_ERROR", "Email is already in use.");
            }
            user.UserName = request.Email;
            user.Email = request.Email;
        }

        // Update role if provided and valid
        if (!string.IsNullOrEmpty(request.Role))
        {
            var validRoles = new[] { "admin", "teacher", "student" };
            if (!validRoles.Contains(request.Role.ToLower()))
            {
                return ApiResponse<UserDto>.ErrorResponse("VALIDATION_ERROR", "Invalid role. Must be admin, teacher, or student.");
            }
            user.Role = request.Role.ToLower();
        }

        // Update active status if provided (for enable/disable account)
        if (request.IsActive.HasValue)
        {
            user.IsActive = request.IsActive.Value;
        }

        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return ApiResponse<UserDto>.ErrorResponse("UPDATE_FAILED", $"Failed to update user: {errors}");
        }

        var userDto = await BuildUserDtoAsync(user);
        return ApiResponse<UserDto>.SuccessResponse(userDto);
    }

    // Performs soft delete by setting IsActive to false
    public async Task<ApiResponse<bool>> DeleteUserAsync(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        if (user == null)
        {
            return ApiResponse<bool>.ErrorResponse("NOT_FOUND", "User not found.");
        }

        // Soft delete preserves data integrity for historical records
        user.IsActive = false;
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            return ApiResponse<bool>.ErrorResponse("DELETE_FAILED", "Failed to delete user.");
        }

        return ApiResponse<bool>.SuccessResponse(true);
    }

    // Updates user's own profile (password and personal info) - users can only update their own profile
    public async Task<ApiResponse<UserDto>> UpdateProfileAsync(int userId, UpdateProfileRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user == null || !user.IsActive)
        {
            return ApiResponse<UserDto>.ErrorResponse("NOT_FOUND", "User not found.");
        }

        // Handle password change with current password verification
        if (!string.IsNullOrEmpty(request.NewPassword))
        {
            // Require current password to prevent unauthorized password changes
            if (string.IsNullOrEmpty(request.CurrentPassword))
            {
                return ApiResponse<UserDto>.ErrorResponse("VALIDATION_ERROR", "Current password is required to change password.");
            }

            // Verify current password before allowing change
            var passwordValid = await _userManager.CheckPasswordAsync(user, request.CurrentPassword);
            if (!passwordValid)
            {
                return ApiResponse<UserDto>.ErrorResponse("VALIDATION_ERROR", "Current password is incorrect.");
            }

            var changeResult = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!changeResult.Succeeded)
            {
                var errors = string.Join(", ", changeResult.Errors.Select(e => e.Description));
                return ApiResponse<UserDto>.ErrorResponse("PASSWORD_CHANGE_FAILED", $"Failed to change password: {errors}");
            }
        }

        // Update personal information in the appropriate profile table based on role
        if (user.Role == "student")
        {
            var student = await _context.Students.FirstOrDefaultAsync(s => s.UserId == user.Id);
            if (student != null)
            {
                if (!string.IsNullOrEmpty(request.FirstName))
                    student.FirstName = request.FirstName;
                if (!string.IsNullOrEmpty(request.LastName))
                    student.LastName = request.LastName;
                if (request.MiddleName != null)
                    student.MiddleName = request.MiddleName;
            }
        }
        else if (user.Role == "teacher")
        {
            var teacher = await _context.Teachers.FirstOrDefaultAsync(t => t.UserId == user.Id);
            if (teacher != null)
            {
                if (!string.IsNullOrEmpty(request.FirstName))
                    teacher.FirstName = request.FirstName;
                if (!string.IsNullOrEmpty(request.LastName))
                    teacher.LastName = request.LastName;
                if (request.MiddleName != null)
                    teacher.MiddleName = request.MiddleName;
            }
        }

        await _context.SaveChangesAsync();

        var userDto = await BuildUserDtoAsync(user);
        return ApiResponse<UserDto>.SuccessResponse(userDto);
    }

    // Builds user DTO with role-specific profile information
    private async Task<UserDto> BuildUserDtoAsync(User user)
    {
        // Start with base user information common to all roles
        var userDto = new UserDto
        {
            Id = user.Id,
            Email = user.Email ?? string.Empty,
            Role = user.Role,
            IsActive = user.IsActive
        };

        // Enrich with student-specific data for student role
        if (user.Role == "student")
        {
            var student = await _context.Students
                .Include(s => s.Course)
                .FirstOrDefaultAsync(s => s.UserId == user.Id);

            if (student != null)
            {
                userDto.StudentNumber = student.StudentNumber;
                userDto.FirstName = student.FirstName;
                userDto.LastName = student.LastName;
                userDto.MiddleName = student.MiddleName;
                userDto.CourseId = student.CourseId;
                userDto.CourseName = student.Course?.Name;
                userDto.YearLevel = student.YearLevel;
            }
        }
        // Enrich with teacher-specific data for teacher role
        else if (user.Role == "teacher")
        {
            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (teacher != null)
            {
                userDto.EmployeeNumber = teacher.EmployeeNumber;
                userDto.FirstName = teacher.FirstName;
                userDto.LastName = teacher.LastName;
                userDto.MiddleName = teacher.MiddleName;
                userDto.Department = teacher.Department;
                userDto.Specialization = teacher.Specialization;
            }
        }

        return userDto;
    }
}