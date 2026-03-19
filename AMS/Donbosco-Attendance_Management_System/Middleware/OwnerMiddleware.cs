using Microsoft.EntityFrameworkCore;
using Donbosco_Attendance_Management_System.Data;
using Donbosco_Attendance_Management_System.DTOs.Responses;

namespace Donbosco_Attendance_Management_System.Middleware;

// require schedule ownership
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireOwnerAttribute : Attribute
{
    // if true, admin can bypass ownership check
    public bool AdminBypass { get; set; } = true;

    public RequireOwnerAttribute() { }
}

// verifies schedule ownership for mutating operations
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
        // get endpoint metadata
        var endpoint = context.GetEndpoint();

        if (endpoint == null)
        {
            await _next(context);
            return;
        }

        // check for requireowner attribute
        var requireOwnerAttribute = endpoint.Metadata
            .OfType<RequireOwnerAttribute>()
            .FirstOrDefault();

        if (requireOwnerAttribute == null)
        {
            // no ownership requirement
            await _next(context);
            return;
        }

        // get current user info
        var userId = context.Items["UserId"] as Guid?;
        var userRole = context.Items["UserRole"] as string;

        if (userId == null)
        {
            // no user attached
            _logger.LogWarning("Unauthenticated access attempt to owner-protected endpoint");
            await WriteUnauthorizedResponse(context, "Authentication required");
            return;
        }

        // admin bypass
        if (requireOwnerAttribute.AdminBypass && string.Equals(userRole, "admin", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogDebug("Admin user {UserId} bypassing ownership check", userId);
            await _next(context);
            return;
        }

        // get schedule id from route
        var scheduleId = ExtractScheduleId(context);

        if (scheduleId == null)
        {
            // no schedule id in route
            await _next(context);
            return;
        }

        // check ownership in database
        var schedule = await dbContext.Schedules
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == scheduleId.Value);

        if (schedule == null)
        {
            // schedule not found
            await _next(context);
            return;
        }

        // verify ownership
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

        // user is the owner
        _logger.LogDebug("Ownership verified for user {UserId} on schedule {ScheduleId}", userId, scheduleId);
        await _next(context);
    }

    private Guid? ExtractScheduleId(HttpContext context)
    {
        // try to get schedule id from route values
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

        // try scheduleid parameter
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

// extension methods for registering OwnerMiddleware
public static class OwnerMiddlewareExtensions
{
    public static IApplicationBuilder UseOwnerMiddleware(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<OwnerMiddleware>();
    }
}