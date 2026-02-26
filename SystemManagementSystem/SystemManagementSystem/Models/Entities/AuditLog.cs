namespace SystemManagementSystem.Models.Entities;

/// <summary>
/// System-wide audit log. Tracks who changed what, when, and the before/after values (as JSON).
/// </summary>
public class AuditLog : BaseEntity
{
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }

    // Who performed the action
    public Guid? PerformedByUserId { get; set; }
    public User? PerformedByUser { get; set; }

    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
}
