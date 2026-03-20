using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Donbosco_Attendance_Management_System.Data;
using Donbosco_Attendance_Management_System.DTOs.Requests;
using Donbosco_Attendance_Management_System.DTOs.Responses;
using Donbosco_Attendance_Management_System.Models;

namespace Donbosco_Attendance_Management_System.Services;

public interface IAuthService
{
    Task<(AuthResponse? Response, string? ErrorCode, string? ErrorMessage)> LoginAsync(LoginRequest request);
    Task<UserProfileResponse?> GetCurrentUserAsync(Guid userId);
    Task<User?> ValidateTokenAsync(string token);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext context, IConfiguration configuration, ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<(AuthResponse? Response, string? ErrorCode, string? ErrorMessage)> LoginAsync(LoginRequest request)
    {
        // Find user by email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

        if (user == null)
        {
            _logger.LogWarning("Login attempt with non-existent email: {Email}", request.Email);
            return (null, ErrorCodes.INVALID_CREDENTIALS, "Invalid email or password");
        }

        // Check if user is active
        if (!user.IsActive)
        {
            _logger.LogWarning("Login attempt for inactive user: {Email}", request.Email);
            return (null, ErrorCodes.USER_INACTIVE, "User account is deactivated");
        }

        // Verify password
        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for user: {Email}", request.Email);
            return (null, ErrorCodes.INVALID_CREDENTIALS, "Invalid email or password");
        }

        // Generate JWT token
        var token = GenerateJwtToken(user);
        var expiresAt = DateTime.UtcNow.AddHours(GetExpirationHours());

        var response = new AuthResponse
        {
            Token = token,
            TokenType = "Bearer",
            ExpiresAt = expiresAt,
            User = MapToUserProfile(user)
        };

        _logger.LogInformation("User logged in successfully: {Email}", request.Email);
        return (response, null, null);
    }

    public async Task<UserProfileResponse?> GetCurrentUserAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        return user != null ? MapToUserProfile(user) : null;
    }

    public async Task<User?> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(GetSecretKey());

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = GetIssuer(),
                ValidateAudience = true,
                ValidAudience = GetAudience(),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero // No clock skew tolerance
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                return null;
            }

            var userId = Guid.Parse(userIdClaim);
            var user = await _context.Users.FindAsync(userId);

            return user?.IsActive == true ? user : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Token validation failed");
            return null;
        }
    }

    private string GenerateJwtToken(User user)
    {
        var key = Encoding.UTF8.GetBytes(GetSecretKey());
        var expirationHours = GetExpirationHours();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.Name),
            new(ClaimTypes.Role, user.Role),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // Unique token ID
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddHours(expirationHours),
            Issuer = GetIssuer(),
            Audience = GetAudience(),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);

        return tokenHandler.WriteToken(token);
    }

    private string GetSecretKey()
    {
        // Prefer environment variable for production
        var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
            ?? _configuration["JwtSettings:SecretKey"];

        if (string.IsNullOrEmpty(secretKey))
        {
            throw new InvalidOperationException("JWT Secret Key is not configured");
        }

        return secretKey;
    }

    private string GetIssuer()
    {
        return _configuration["JwtSettings:Issuer"] ?? "DonboscoAttendanceSystem";
    }

    private string GetAudience()
    {
        return _configuration["JwtSettings:Audience"] ?? "DonboscoAttendanceSystem";
    }

    private int GetExpirationHours()
    {
        var expirationStr = _configuration["JwtSettings:ExpirationHours"];
        return int.TryParse(expirationStr, out var hours) ? hours : 8;
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