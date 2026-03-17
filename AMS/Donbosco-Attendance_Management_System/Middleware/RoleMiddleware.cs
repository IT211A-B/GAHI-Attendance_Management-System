using Microsoft.AspNetCore.Mvc;
using Donbosco_Attendance_Management_System.DTOs.Responses;

namespace Donbosco_Attendance_Management_System.Middleware;

/// <summary>
/// Attribute to restrict access to specific roles
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireRoleAttribute : Attribute
{
    public string[] Roles { get; }

    public RequireRoleAttribute(params string[] roles)
    {
        Roles = roles;
    }
}

/// <summary>
/// Middleware that checks if the authenticated user has the required role(s)
/// Should be used after JwtMiddleware
/// </summary>
public class RoleMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RoleMiddleware> _logger;

    public RoleMiddleware(RequestDelegate next, ILogger<RoleMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Get the endpoint metadata to check for RequireRole attribute
        var endpoint = context.GetEndpoint();

        if (endpoint == null)
        {
            await _next(context);
            return;
        }

        // Check for RequireRole attribute on the endpoint or controller
        var requireRoleAttribute = endpoint.Metadata
            .OfType<RequireRoleAttribute>()
            .FirstOrDefault();

        if (requireRoleAttribute == null)
        {
            // No role requirement, proceed
            await _next(context);
            return;
        }

        // Check if user is authenticated
        var userRole = context.Items["UserRole"] as string;

        if (string.IsNullOrEmpty(userRole))
        {
            // No user attached - return 401 Unauthorized
            _logger.LogWarning("Unauthenticated access attempt to protected endpoint");
            await WriteUnauthorizedResponse(context, "Authentication required");
            return;
        }

        // Check if user has one of the required roles
        if (!requireRoleAttribute.Roles.Contains(userRole, StringComparer.OrdinalIgnoreCase))
        {
            // User doesn't have required role - return 403 Forbidden
            _logger.LogWarning(
                "User with role '{UserRole}' attempted to access endpoint requiring roles: {RequiredRoles}",
                userRole,
                string.Join(", ", requireRoleAttribute.Roles)
            );

            await WriteForbiddenResponse(context, userRole, requireRoleAttribute.Roles);
            return;
        }

        // User has required role, proceed
        await _next(context);
    }

    private static async Task WriteUnauthorizedResponse(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        var response = ApiResponse.FailureResponse(ErrorCodes.UNAUTHORIZED, message);
        await context.Response.WriteAsJsonAsync(response);
    }

    private static async Task WriteForbiddenResponse(HttpContext context, string userRole, string[] requiredRoles)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";

        var response = ApiResponse.FailureResponse(
            ErrorCodes.FORBIDDEN,
            $"Access denied. Required role(s): {string.Join(", ", requiredRoles)}. Your role: {userRole}"
        );

        await context.Response.WriteAsJsonAsync(response);
    }
}

/// <summary>
/// Extension methods for registering RoleMiddleware
/// </summary>
public static class RoleMiddlewareExtensions
{
    public static IApplicationBuilder UseRoleMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RoleMiddleware>();
    }
}