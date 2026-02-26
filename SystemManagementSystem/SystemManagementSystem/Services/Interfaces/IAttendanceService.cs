using SystemManagementSystem.DTOs.Attendance;
using SystemManagementSystem.DTOs.Common;

namespace SystemManagementSystem.Services.Interfaces;

public interface IAttendanceService
{
    Task<ScanResponse> ProcessScanAsync(ScanRequest request);
    Task<PagedResult<AttendanceLogResponse>> GetLogsAsync(AttendanceFilterRequest filter);
    Task<AttendanceLogResponse> GetByIdAsync(Guid id);
}
