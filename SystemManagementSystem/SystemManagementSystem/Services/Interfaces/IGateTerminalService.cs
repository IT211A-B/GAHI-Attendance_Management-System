using SystemManagementSystem.DTOs.Common;
using SystemManagementSystem.DTOs.GateTerminals;

namespace SystemManagementSystem.Services.Interfaces;

public interface IGateTerminalService
{
    Task<PagedResult<GateTerminalResponse>> GetAllAsync(int page, int pageSize);
    Task<GateTerminalResponse> GetByIdAsync(Guid id);
    Task<GateTerminalResponse> CreateAsync(CreateGateTerminalRequest request);
    Task<GateTerminalResponse> UpdateAsync(Guid id, UpdateGateTerminalRequest request);
    Task DeleteAsync(Guid id);
}
