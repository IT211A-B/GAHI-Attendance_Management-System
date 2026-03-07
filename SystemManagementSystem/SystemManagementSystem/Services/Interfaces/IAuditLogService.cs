using SystemManagementSystem.DTOs.AuditLogs;
using SystemManagementSystem.DTOs.Common;

namespace SystemManagementSystem.Services.Interfaces;

public interface IAuditLogService
{
    Task<PagedResult<AuditLogResponse>> GetLogsAsync(AuditLogFilterRequest filter);
    Task LogAsync(string action, string entityName, string? entityId, string? oldValues, string? newValues, Guid? userId);
}
