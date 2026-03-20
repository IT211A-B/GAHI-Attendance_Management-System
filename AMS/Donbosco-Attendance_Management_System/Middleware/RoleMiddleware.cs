using Microsoft.AspNetCore.Mvc;
using Donbosco_Attendance_Management_System.DTOs.Responses;

namespace Donbosco_Attendance_Management_System.Middleware;

// attribute to restrict access to specific roles
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireRoleAttribute : Attribute
{
    public string[] Roles { get; }

    public RequireRoleAttribute(params string[] roles)
    {
        Roles = roles;
    }
}

// checks if authenticated user has required role(s)
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
        // get endpoint metadata
        var endpoint = context.GetEndpoint();

        if (endpoint == null)
        {
            await _next(context);
            return;
        }

        // check for requirerole attribute
        var requireRoleAttribute = endpoint.Metadata
            .OfType<RequireRoleAttribute>()
            .FirstOrDefault();

        if (requireRoleAttribute == null)
        {
            // no role requirement
            await _next(context);
            return;
        }

        // get user role
        var userRole = context.Items["UserRole"] as string;

        if (string.IsNullOrEmpty(userRole))
        {
            // no user attached
            _logger.LogWarning("Unauthenticated access attempt to protected endpoint");
            await WriteUnauthorizedResponse(context, "Authentication required");
            return;
        }

        // check if user has required role
        if (!requireRoleAttribute.Roles.Contains(userRole, StringComparer.OrdinalIgnoreCase))
        {
            // user doesn't have required role
            _logger.LogWarning(
                "User with role '{UserRole}' attempted to access endpoint requiring roles: {RequiredRoles}",
                userRole,
                string.Join(", ", requireRoleAttribute.Roles)
            );

            await WriteForbiddenResponse(context, userRole, requireRoleAttribute.Roles);
            return;
        }

        // user has required role
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

// extension methods for registering RoleMiddleware
public static class RoleMiddlewareExtensions
{
    public static IApplicationBuilder UseRoleMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<RoleMiddleware>();
    }
}