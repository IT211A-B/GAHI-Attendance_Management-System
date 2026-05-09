using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;
using Attendance_Management_System.Backend.Entities;
using Attendance_Management_System.Backend.Enums;
using Attendance_Management_System.Backend.Helpers;
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
    public async Task<List<UserDto>> GetAllUsersAsync()
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
            if (user.Role.IsRole(UserRole.Student) && students.TryGetValue(user.Id, out var student))
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
            else if (user.Role.IsRole(UserRole.Teacher) && teachers.TryGetValue(user.Id, out var teacher))
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

        return userDtos;
    }

    // Retrieves a single user by ID with role-specific profile
    public async Task<UserDto> GetUserByIdAsync(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        // Only return active users
        if (user == null || !user.IsActive)
        {
            throw new KeyNotFoundException("User not found.");
        }

        var userDto = await BuildUserDtoAsync(user);
        return userDto;
    }

    // Creates a new user account with specified role (admin only)
    public async Task<UserDto> CreateUserAsync(CreateUserRequest request)
    {
        // Prevent duplicate email registration
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
        {
            throw new InvalidOperationException("An account with this email already exists.");
        }

        var normalizedRole = NormalizeRoleOrThrow(request.Role);

        // Create user with Identity for secure password hashing
        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            Role = normalizedRole,
            IsActive = true,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);

        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create user: {errors}");
        }

        var userDto = await BuildUserDtoAsync(user);
        return userDto;
    }

    // Updates user email, role, or active status
    public async Task<UserDto> UpdateUserAsync(int id, UpdateUserRequest request)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        // Update email if provided and not already in use
        if (!string.IsNullOrEmpty(request.Email) && request.Email != user.Email)
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null && existingUser.Id != user.Id)
            {
                throw new InvalidOperationException("Email is already in use.");
            }
            user.UserName = request.Email;
            user.Email = request.Email;
        }

        // Update role if provided and valid
        if (!string.IsNullOrEmpty(request.Role))
        {
            user.Role = NormalizeRoleOrThrow(request.Role);
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
            throw new InvalidOperationException($"Failed to update user: {errors}");
        }

        var userDto = await BuildUserDtoAsync(user);
        return userDto;
    }

    // Performs soft delete by setting IsActive to false
    public async Task DeleteUserAsync(int id)
    {
        var user = await _userManager.FindByIdAsync(id.ToString());

        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        // Soft delete preserves data integrity for historical records
        user.IsActive = false;
        var result = await _userManager.UpdateAsync(user);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException("Failed to delete user.");
        }

        return;
    }

    // Updates user's own profile (password and personal info) - users can only update their own profile
    public async Task<UserDto> UpdateProfileAsync(int userId, UpdateProfileRequest request)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());

        if (user == null || !user.IsActive)
        {
            throw new KeyNotFoundException("User not found.");
        }

        // Handle password change with current password verification
        if (!string.IsNullOrEmpty(request.NewPassword))
        {
            // Require current password to prevent unauthorized password changes
            if (string.IsNullOrEmpty(request.CurrentPassword))
            {
                throw new InvalidOperationException("Current password is required to change password.");
            }

            // Verify current password before allowing change
            var passwordValid = await _userManager.CheckPasswordAsync(user, request.CurrentPassword);
            if (!passwordValid)
            {
                throw new InvalidOperationException("Current password is incorrect.");
            }

            var changeResult = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!changeResult.Succeeded)
            {
                var errors = string.Join(", ", changeResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to change password: {errors}");
            }
        }

        // Update personal information in the appropriate profile table based on role
        if (user.Role.IsRole(UserRole.Student))
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
        else if (user.Role.IsRole(UserRole.Teacher))
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
        return userDto;
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
        if (user.Role.IsRole(UserRole.Student))
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
        else if (user.Role.IsRole(UserRole.Teacher))
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

    private static string NormalizeRoleOrThrow(string? role)
    {
        if (EnumStorage.TryParseRole(role, out var parsedRole))
        {
            return parsedRole.ToStorageValue();
        }

        throw new InvalidOperationException("Invalid role. Must be admin, teacher, or student.");
    }
}


