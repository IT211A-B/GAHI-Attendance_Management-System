using SystemManagementSystem.DTOs.Common;
using SystemManagementSystem.DTOs.Users;

namespace SystemManagementSystem.Services.Interfaces;

public interface IUserService
{
    Task<PagedResult<UserResponse>> GetAllAsync(int page, int pageSize);
    Task<UserResponse> GetByIdAsync(Guid id);
    Task<UserResponse> CreateAsync(CreateUserRequest request);
    Task<UserResponse> UpdateAsync(Guid id, UpdateUserRequest request);
    Task DeleteAsync(Guid id);
    Task<UserResponse> AssignRolesAsync(Guid userId, AssignRolesRequest request);
}
