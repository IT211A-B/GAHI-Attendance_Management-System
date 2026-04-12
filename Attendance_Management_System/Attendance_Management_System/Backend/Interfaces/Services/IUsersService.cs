using Attendance_Management_System.Backend.DTOs.Requests;
using Attendance_Management_System.Backend.DTOs.Responses;

namespace Attendance_Management_System.Backend.Interfaces.Services;

public interface IUsersService
{
    Task<ApiResponse<List<UserDto>>> GetAllUsersAsync();
    Task<ApiResponse<UserDto>> GetUserByIdAsync(int id);
    Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserRequest request);
    Task<ApiResponse<UserDto>> UpdateUserAsync(int id, UpdateUserRequest request);
    Task<ApiResponse<UserDto>> UpdateProfileAsync(int userId, UpdateProfileRequest request);
    Task<ApiResponse<bool>> DeleteUserAsync(int id);
}
