using SystemManagementSystem.DTOs.Auth;

namespace SystemManagementSystem.Services.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
}
