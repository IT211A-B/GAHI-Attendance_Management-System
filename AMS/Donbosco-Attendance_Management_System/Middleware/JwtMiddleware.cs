using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using Donbosco_Attendance_Management_System.Models;
using Donbosco_Attendance_Management_System.Data;
using Microsoft.EntityFrameworkCore;

namespace Donbosco_Attendance_Management_System.Middleware;

// validates jwt tokens and attaches user to HttpContext.Items
public class JwtMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<JwtMiddleware> _logger;

    public JwtMiddleware(RequestDelegate next, ILogger<JwtMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext, IConfiguration configuration)
    {
        var token = ExtractToken(context);

        if (!string.IsNullOrEmpty(token))
        {
            var user = await ValidateTokenAndGetUser(token, dbContext, configuration);

            if (user != null)
            {
                // attach user info to context
                context.Items["User"] = user;
                context.Items["UserId"] = user.Id;
                context.Items["UserRole"] = user.Role;
                context.Items["UserEmail"] = user.Email;

                _logger.LogDebug("JWT validated for user: {Email} with role: {Role}", user.Email, user.Role);
            }
        }

        await _next(context);
    }

    private string? ExtractToken(HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();

        if (string.IsNullOrEmpty(authHeader))
        {
            return null;
        }

        // expected format: Bearer {token}
        if (authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authHeader.Substring("Bearer ".Length).Trim();
        }

        return null;
    }

    private async Task<User?> ValidateTokenAndGetUser(string token, AppDbContext dbContext, IConfiguration configuration)
    {
        try
        {
            var secretKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY")
                ?? configuration["JwtSettings:SecretKey"];

            if (string.IsNullOrEmpty(secretKey))
            {
                _logger.LogError("JWT Secret Key is not configured");
                return null;
            }

            var key = Encoding.UTF8.GetBytes(secretKey);
            var issuer = configuration["JwtSettings:Issuer"] ?? "DonboscoAttendanceSystem";
            var audience = configuration["JwtSettings:Audience"] ?? "DonboscoAttendanceSystem";

            var tokenHandler = new JwtSecurityTokenHandler();

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = issuer,
                ValidateAudience = true,
                ValidAudience = audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);

            var userIdClaim = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                _logger.LogWarning("Token does not contain user ID claim");
                return null;
            }

            var userId = Guid.Parse(userIdClaim);

            var user = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning("User not found for ID: {UserId}", userId);
                return null;
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Inactive user attempted access: {UserId}", userId);
                return null;
            }

            return user;
        }
        catch (SecurityTokenExpiredException)
        {
            _logger.LogDebug("Token has expired");
            return null;
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            _logger.LogWarning("Token has invalid signature");
            return null;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning("Token validation failed: {Message}", ex.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during token validation");
            return null;
        }
    }
}

// extension methods for registering JwtMiddleware
public static class JwtMiddlewareExtensions
{
    public static IApplicationBuilder UseJwtMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<JwtMiddleware>();
    }
}