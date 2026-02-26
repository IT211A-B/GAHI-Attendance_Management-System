using System.Security.Claims;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SystemManagementSystem.Data;
using SystemManagementSystem.DTOs.Common;
using SystemManagementSystem.DTOs.Users;
using SystemManagementSystem.Models.Entities;
using SystemManagementSystem.Services.Interfaces;

namespace SystemManagementSystem.Services.Implementations;

public class UserService : IUserService
{
    private readonly ApplicationDbContext _context;
    private readonly IAuditLogService _auditLog;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserService(ApplicationDbContext context, IAuditLogService auditLog, IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _auditLog = auditLog;
        _httpContextAccessor = httpContextAccessor;
    }

    private Guid? GetCurrentUserId()
    {
        var claim = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier);
        return claim != null ? Guid.Parse(claim.Value) : null;
    }

    public async Task<PagedResult<UserResponse>> GetAllAsync(int page, int pageSize)
    {
        var query = _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .OrderByDescending(u => u.CreatedAt);

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResult<UserResponse>
        {
            Items = data.Select(MapToResponse).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount
        };
    }

    public async Task<UserResponse> GetByIdAsync(Guid id)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new KeyNotFoundException($"User with ID {id} not found.");

        return MapToResponse(user);
    }

    public async Task<UserResponse> CreateAsync(CreateUserRequest request)
    {
        if (await _context.Users.AnyAsync(u => u.Username == request.Username))
            throw new InvalidOperationException($"Username '{request.Username}' is already taken.");

        if (await _context.Users.AnyAsync(u => u.Email == request.Email))
            throw new InvalidOperationException($"Email '{request.Email}' is already in use.");

        var user = new User
        {
            Username = request.Username,
            Email = request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName
        };

        if (request.RoleIds.Any())
        {
            var roles = await _context.Roles
                .Where(r => request.RoleIds.Contains(r.Id))
                .ToListAsync();

            foreach (var role in roles)
            {
                user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id });
            }
        }

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync("Create", "User", user.Id.ToString(),
            null,
            JsonSerializer.Serialize(new { user.Username, user.Email, user.FirstName, user.LastName }),
            GetCurrentUserId());

        return await GetByIdAsync(user.Id);
    }

    public async Task<UserResponse> UpdateAsync(Guid id, UpdateUserRequest request)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == id)
            ?? throw new KeyNotFoundException($"User with ID {id} not found.");

        var oldValues = JsonSerializer.Serialize(new { user.Email, user.FirstName, user.LastName, user.IsActive });

        if (request.Email != null)
        {
            if (await _context.Users.AnyAsync(u => u.Email == request.Email && u.Id != id))
                throw new InvalidOperationException($"Email '{request.Email}' is already in use.");
            user.Email = request.Email;
        }

        if (request.FirstName != null) user.FirstName = request.FirstName;
        if (request.LastName != null) user.LastName = request.LastName;
        if (request.IsActive.HasValue) user.IsActive = request.IsActive.Value;

        await _context.SaveChangesAsync();

        var newValues = JsonSerializer.Serialize(new { user.Email, user.FirstName, user.LastName, user.IsActive });
        await _auditLog.LogAsync("Update", "User", id.ToString(), oldValues, newValues, GetCurrentUserId());

        return MapToResponse(user);
    }

    public async Task DeleteAsync(Guid id)
    {
        var user = await _context.Users.FindAsync(id)
            ?? throw new KeyNotFoundException($"User with ID {id} not found.");

        var oldValues = JsonSerializer.Serialize(new { user.Username, user.Email, user.FirstName, user.LastName });

        user.IsDeleted = true;
        await _context.SaveChangesAsync();

        await _auditLog.LogAsync("Delete", "User", id.ToString(), oldValues, null, GetCurrentUserId());
    }

    public async Task<UserResponse> AssignRolesAsync(Guid userId, AssignRolesRequest request)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == userId)
            ?? throw new KeyNotFoundException($"User with ID {userId} not found.");

        // Validate all roles first
        foreach (var roleId in request.RoleIds)
        {
            if (!await _context.Roles.AnyAsync(r => r.Id == roleId))
                throw new KeyNotFoundException($"Role with ID {roleId} not found.");
        }

        // Remove existing roles
        var existingRoles = user.UserRoles.ToList();
        _context.UserRoles.RemoveRange(existingRoles);
        await _context.SaveChangesAsync();

        // Add new roles
        foreach (var roleId in request.RoleIds)
        {
            _context.UserRoles.Add(new UserRole { UserId = userId, RoleId = roleId });
        }

        await _context.SaveChangesAsync();
        return await GetByIdAsync(userId);
    }

    private static UserResponse MapToResponse(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        FirstName = user.FirstName,
        LastName = user.LastName,
        IsActive = user.IsActive,
        LastLoginAt = user.LastLoginAt,
        Roles = user.UserRoles.Select(ur => ur.Role.Name).ToList(),
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt
    };
}
