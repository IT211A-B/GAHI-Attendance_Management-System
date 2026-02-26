using Microsoft.EntityFrameworkCore;
using SystemManagementSystem.Data;
using SystemManagementSystem.DTOs.Auth;
using SystemManagementSystem.Helpers;
using SystemManagementSystem.Services.Interfaces;

namespace SystemManagementSystem.Services.Implementations;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _context;
    private readonly JwtTokenHelper _jwtHelper;
    private readonly IConfiguration _configuration;

    public AuthService(ApplicationDbContext context, JwtTokenHelper jwtHelper, IConfiguration configuration)
    {
        _context = context;
        _jwtHelper = jwtHelper;
        _configuration = configuration;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Username == request.Username && u.IsActive);

        if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid username or password.");

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var token = _jwtHelper.GenerateToken(user, roles);

        var expirationMinutes = int.Parse(_configuration["JwtSettings:ExpirationInMinutes"]!);

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new LoginResponse
        {
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
            Username = user.Username,
            Email = user.Email,
            FullName = $"{user.FirstName} {user.LastName}",
            Roles = roles
        };
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new KeyNotFoundException("User not found.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new UnauthorizedAccessException("Current password is incorrect.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _context.SaveChangesAsync();
    }
}
