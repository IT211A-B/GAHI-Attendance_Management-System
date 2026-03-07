using Microsoft.EntityFrameworkCore;
using SystemManagementSystem.Data;
using SystemManagementSystem.DTOs.AuditLogs;
using SystemManagementSystem.DTOs.Common;
using SystemManagementSystem.Models.Entities;
using SystemManagementSystem.Services.Interfaces;

namespace SystemManagementSystem.Services.Implementations;

public class AuditLogService : IAuditLogService
{
    private readonly ApplicationDbContext _context;

    public AuditLogService(ApplicationDbContext context) => _context = context;

    public async Task<PagedResult<AuditLogResponse>> GetLogsAsync(AuditLogFilterRequest filter)
    {
        var query = _context.AuditLogs
            .Include(a => a.PerformedByUser)
            .AsQueryable();

        if (!string.IsNullOrEmpty(filter.Action))
            query = query.Where(a => a.Action == filter.Action);
        if (!string.IsNullOrEmpty(filter.EntityName))
            query = query.Where(a => a.EntityName == filter.EntityName);
        if (filter.UserId.HasValue)
            query = query.Where(a => a.PerformedByUserId == filter.UserId.Value);
        if (filter.StartDate.HasValue)
            query = query.Where(a => a.PerformedAt >= filter.StartDate.Value);
        if (filter.EndDate.HasValue)
            query = query.Where(a => a.PerformedAt <= filter.EndDate.Value);

        query = query.OrderByDescending(a => a.PerformedAt);

        var totalCount = await query.CountAsync();
        var data = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<AuditLogResponse>
        {
            Items = data.Select(a => new AuditLogResponse
            {
                Id = a.Id,
                Action = a.Action,
                EntityName = a.EntityName,
                EntityId = a.EntityId,
                OldValues = a.OldValues,
                NewValues = a.NewValues,
                PerformedByUserId = a.PerformedByUserId,
                PerformedByUsername = a.PerformedByUser?.Username,
                PerformedAt = a.PerformedAt
            }).ToList(),
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalCount = totalCount
        };
    }

    public async Task LogAsync(string action, string entityName, string? entityId,
        string? oldValues, string? newValues, Guid? userId)
    {
        var log = new AuditLog
        {
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            PerformedByUserId = userId,
            PerformedAt = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}
