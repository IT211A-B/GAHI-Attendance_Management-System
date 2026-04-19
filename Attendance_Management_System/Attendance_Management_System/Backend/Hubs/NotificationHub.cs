using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Attendance_Management_System.Backend.Hubs;

[Authorize]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var context = ResolveConnectionContext();
        if (context.UserId.HasValue)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{context.UserId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(context.Role))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"role:{context.Role}");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var context = ResolveConnectionContext();
        if (context.UserId.HasValue)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user:{context.UserId.Value}");
        }

        if (!string.IsNullOrWhiteSpace(context.Role))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"role:{context.Role}");
        }

        await base.OnDisconnectedAsync(exception);
    }

    private (int? UserId, string Role) ResolveConnectionContext()
    {
        var userIdClaim = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = (Context.User?.FindFirstValue(ClaimTypes.Role) ?? string.Empty).Trim().ToLowerInvariant();

        if (!int.TryParse(userIdClaim, out var userId))
        {
            return (null, role);
        }

        return (userId, role);
    }
}
