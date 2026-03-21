using Attendance_Management_System.Backend.Entities;
using System.Security.Claims;

namespace Attendance_Management_System.Backend.Interfaces.Services;

public interface ITokenService
{
    string GenerateToken(User user);
    ClaimsPrincipal? ValidateToken(string token);
    int? GetUserIdFromToken(ClaimsPrincipal principal);
}