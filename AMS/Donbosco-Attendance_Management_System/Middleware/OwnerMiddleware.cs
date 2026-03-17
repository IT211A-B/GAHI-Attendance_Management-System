using Microsoft.EntityFrameworkCore;
using Donbosco_Attendance_Management_System.Data;
using Donbosco_Attendance_Management_System.DTOs.Responses;

namespace Donbosco_Attendance_Management_System.Middleware;

/// <summary>
/// Attribute to require schedule ownership (teacher_id must match current user)
/// Admins can optionally bypass this check
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireOwnerAttribute : Attribute
{
    /// <summary>
    /// If true, admin can bypass ownership check (e.g., for DELETE any schedule)
    /// If false, admin must also be the owner (rare case)
    /// </summary>
    public bool AdminBypass { get; set; } = true;

    public RequireOwnerAttribute() { }
}

/// <summary>
/// Middleware that verifies schedule ownership for mutating operations
/// Checks if the authenticated user owns the schedule (teacher_id == current user id)
/// Admin can bypass for certain operations like DELETE
/// </summary>
public class OwnerMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<OwnerMiddleware> _logger;

    public OwnerMiddleware(RequestDelegate next, ILogger<OwnerMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext dbContext)
    {
        // Get the endpoint metadata to check for RequireOwner attribute
        var endpoint = context.GetEndpoint();

        if (endpoint == null)
        {
            await _next(context);
            return;
        }

        // Check for RequireOwner attribute
        var requireOwnerAttribute = endpoint.Metadata
            .OfType<RequireOwnerAttribute>()
            .FirstOrDefault();

        if (requireOwnerAttribute == null)
        {
            // No ownership requirement, proceed
            await _next(context);
            return;
        }

        // Get current user info from HttpContext (set by JwtMiddleware)
        var userId = context.Items["UserId"] as Guid?;
        var userRole = context.Items["UserRole"] as string;

        if (userId == null)
        {
            // No user attached - return 401 Unauthorized
            _logger.LogWarning("Unauthenticated access attempt to owner-protected endpoint");
            await WriteUnauthorizedResponse(context, "Authentication required");
            return;
        }

        // Admin bypass check
        if (requireOwnerAttribute.AdminBypass && string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Admin user {UserId} bypassing ownership check", userId);
            await _next(context);
            return;
        }

        // Extract schedule ID from route
        var scheduleId = ExtractScheduleId(context);

        if (scheduleId == null)
        {
            // No schedule ID in route - let the controller handle it (likely 400 or 404)
            await _next(context);
            return;
        }

        // Check ownership in database
        var schedule = await dbContext.Schedules
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == scheduleId.Value);

        if (schedule == null)
        {
            // Schedule doesn't exist - let the controller return 404
            await _next(context);
            return;
        }

        // Verify ownership
        if (schedule.TeacherId != userId.Value)
        {
            _logger.LogWarning(
                "User {UserId} attempted to access schedule {ScheduleId} owned by {OwnerId}",
                userId,
                scheduleId,
                schedule.TeacherId
            );

            await WriteForbiddenResponse(context, "You do not have permission to access this schedule");
            return;
        }

        // User is the owner, proceed
        _logger.LogDebug("Ownership verified for user {UserId} on schedule {ScheduleId}", userId, scheduleId);
        await _next(context);
    }

    private Guid? ExtractScheduleId(HttpContext context)
    {
        // Try to get schedule ID from route values
        if (context.Request.RouteValues.TryGetValue("id", out var idValue))
        {
            if (idValue is Guid guidId)
            {
                return guidId;
            }

            if (idValue is string stringId && Guid.TryParse(stringId, out var parsedId))
            {
                return parsedId;
            }
        }

        // Try to get from route parameter with different name
        if (context.Request.RouteValues.TryGetValue("scheduleId", out var scheduleIdValue))
        {
            if (scheduleIdValue is Guid guidScheduleId)
            {
                return guidScheduleId;
            }

            if (scheduleIdValue is string stringScheduleId && Guid.TryParse(stringScheduleId, out var parsedScheduleId))
            {
                return parsedScheduleId;
            }
        }

        return null;
    }

    private static async Task WriteUnauthorizedResponse(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        context.Response.ContentType = "application/json";

        var response = ApiResponse.FailureResponse(ErrorCodes.UNAUTHORIZED, message);
        await context.Response.WriteAsJsonAsync(response);
    }

    private static async Task WriteForbiddenResponse(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json";

        var response = ApiResponse.FailureResponse(ErrorCodes.FORBIDDEN, message);
        await context.Response.WriteAsJsonAsync(response);
    }
}

/// <summary>
/// Extension methods for registering OwnerMiddleware
/// </summary>
public static class OwnerMiddlewareExtensions
{
    public static IApplicationBuilder UseOwnerMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<OwnerMiddleware>();
    }
}