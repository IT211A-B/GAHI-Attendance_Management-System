using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using SystemManagementSystem.Data;
using SystemManagementSystem.DTOs.Auth;
using SystemManagementSystem.Helpers;
using SystemManagementSystem.Models.Entities;
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

        // Generate refresh token
        var refreshToken = await CreateRefreshTokenAsync(user.Id);

        // Update last login
        user.LastLoginAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new LoginResponse
        {
            Token = token,
            RefreshToken = refreshToken.Token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
            Username = user.Username,
            Email = user.Email,
            FullName = $"{user.FirstName} {user.LastName}",
            Roles = roles
        };
    }

    public async Task<LoginResponse> RefreshAsync(string refreshToken)
    {
        var stored = await _context.RefreshTokens
            .Include(rt => rt.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

        if (stored == null || !stored.IsActive)
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        // Revoke the old token
        stored.RevokedAt = DateTime.UtcNow;

        var user = stored.User;
        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
        var token = _jwtHelper.GenerateToken(user, roles);
        var expirationMinutes = int.Parse(_configuration["JwtSettings:ExpirationInMinutes"]!);

        // Issue a new refresh token
        var newRefreshToken = await CreateRefreshTokenAsync(user.Id);
        await _context.SaveChangesAsync();

        return new LoginResponse
        {
            Token = token,
            RefreshToken = newRefreshToken.Token,
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

    private async Task<RefreshToken> CreateRefreshTokenAsync(Guid userId)
    {
        var tokenBytes = RandomNumberGenerator.GetBytes(64);
        var refreshToken = new RefreshToken
        {
            Token = Convert.ToBase64String(tokenBytes),
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(7) // 7-day refresh token
        };

        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();
        return refreshToken;
    }
}
