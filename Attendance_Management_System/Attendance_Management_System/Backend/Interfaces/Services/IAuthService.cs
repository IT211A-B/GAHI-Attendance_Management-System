using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;

namespace Attendance_Management_System.Backend.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponse> RegisterStudentAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> RegisterTeacherAsync(TeacherRegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> ConfirmEmailAsync(int userId, string token, CancellationToken cancellationToken = default);
    Task<AuthResponse> ResendVerificationAsync(string email, CancellationToken cancellationToken = default);
    Task<AuthResponse> ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task<UserDto?> GetUserProfileAsync(int userId, CancellationToken cancellationToken = default);
}