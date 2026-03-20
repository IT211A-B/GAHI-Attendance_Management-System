using Microsoft.EntityFrameworkCore;
using Donbosco_Attendance_Management_System.Data;
using Donbosco_Attendance_Management_System.DTOs.Requests;
using Donbosco_Attendance_Management_System.DTOs.Responses;
using Donbosco_Attendance_Management_System.Models;

namespace Donbosco_Attendance_Management_System.Services;

public interface IUsersService
{
    Task<UserListResponse> GetAllUsersAsync();
    Task<UserProfileResponse?> GetUserByIdAsync(Guid id);
    Task<(UserProfileResponse? User, string? ErrorCode, string? ErrorMessage)> CreateUserAsync(CreateUserRequest request);
    Task<(UserProfileResponse? User, string? ErrorCode, string? ErrorMessage)> UpdateUserAsync(Guid id, UpdateUserRequest request);
    Task<(bool Success, string? ErrorCode, string? ErrorMessage)> DeleteUserAsync(Guid id);
    Task<(UserProfileResponse? User, string? ErrorCode, string? ErrorMessage)> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
}

public class UsersService : IUsersService
{
    private readonly AppDbContext _context;
    private readonly ILogger<UsersService> _logger;

    public UsersService(AppDbContext context, ILogger<UsersService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<UserListResponse> GetAllUsersAsync()
    {
        var users = await _context.Users
            .AsNoTracking()
            .OrderBy(u => u.CreatedAt)
            .ToListAsync();

        return new UserListResponse
        {
            Items = users.Select(MapToUserProfile).ToList(),
            Total = users.Count
        };
    }

    public async Task<UserProfileResponse?> GetUserByIdAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        return user != null ? MapToUserProfile(user) : null;
    }

    public async Task<(UserProfileResponse? User, string? ErrorCode, string? ErrorMessage)> CreateUserAsync(CreateUserRequest request)
    {
        // Check for duplicate email
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (existingUser != null)
        {
            return (null, ErrorCodes.VALIDATION_ERROR, "A user with this email already exists");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = request.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role.ToLower(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User created: {Email} with role {Role}", user.Email, user.Role);
        return (MapToUserProfile(user), null, null);
    }

    public async Task<(UserProfileResponse? User, string? ErrorCode, string? ErrorMessage)> UpdateUserAsync(Guid id, UpdateUserRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return (null, ErrorCodes.NOT_FOUND, "User not found");
        }

        // Check for duplicate email if email is being changed
        if (!string.IsNullOrEmpty(request.Email) && request.Email.ToLower() != user.Email)
        {
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower() && u.Id != id);

            if (existingUser != null)
            {
                return (null, ErrorCodes.VALIDATION_ERROR, "A user with this email already exists");
            }
            user.Email = request.Email.ToLower().Trim();
        }

        // Update other fields if provided
        if (!string.IsNullOrEmpty(request.Name))
        {
            user.Name = request.Name.Trim();
        }

        if (!string.IsNullOrEmpty(request.Role))
        {
            user.Role = request.Role.ToLower();
        }

        if (!string.IsNullOrEmpty(request.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        if (request.IsActive.HasValue)
        {
            user.IsActive = request.IsActive.Value;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("User updated: {Email}", user.Email);
        return (MapToUserProfile(user), null, null);
    }

    public async Task<(bool Success, string? ErrorCode, string? ErrorMessage)> DeleteUserAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return (false, ErrorCodes.NOT_FOUND, "User not found");
        }

        // Soft delete - just set is_active to false
        user.IsActive = false;
        await _context.SaveChangesAsync();

        _logger.LogInformation("User soft-deleted: {Email}", user.Email);
        return (true, null, null);
    }

    public async Task<(UserProfileResponse? User, string? ErrorCode, string? ErrorMessage)> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return (null, ErrorCodes.NOT_FOUND, "User not found");
        }

        if (!string.IsNullOrEmpty(request.Name))
        {
            user.Name = request.Name.Trim();
        }

        if (!string.IsNullOrEmpty(request.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("User profile updated: {Email}", user.Email);
        return (MapToUserProfile(user), null, null);
    }

    private static UserProfileResponse MapToUserProfile(User user)
    {
        return new UserProfileResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }
}