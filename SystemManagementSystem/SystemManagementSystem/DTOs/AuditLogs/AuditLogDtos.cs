namespace SystemManagementSystem.DTOs.AuditLogs;

public class AuditLogResponse
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public Guid? PerformedByUserId { get; set; }
    public string? PerformedByUsername { get; set; }
    public DateTime PerformedAt { get; set; }
}

public class AuditLogFilterRequest
{
    public string? Action { get; set; }
    public string? EntityName { get; set; }
    public Guid? UserId { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
