using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;

namespace Attendance_Management_System.Backend.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RegisterStudentAsync(RegisterRequest request);
    Task<AuthResponse> RegisterTeacherAsync(TeacherRegisterRequest request);
    Task<AuthResponse> ConfirmEmailAsync(int userId, string token);
    Task<AuthResponse> ResendVerificationAsync(string email);
    Task<AuthResponse> ForgotPasswordAsync(ForgotPasswordRequest request);
    Task<AuthResponse> ResetPasswordAsync(ResetPasswordRequest request);
    Task<UserDto?> GetUserProfileAsync(int userId);
}