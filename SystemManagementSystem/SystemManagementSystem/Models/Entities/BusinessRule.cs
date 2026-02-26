namespace SystemManagementSystem.Models.Entities;

/// <summary>
/// A configurable business rule (e.g., grace periods, cutoff times).
/// If DepartmentId is null, the rule applies institution-wide.
/// </summary>
public class BusinessRule : BaseEntity
{
    public string RuleKey { get; set; } = string.Empty;
    public string RuleValue { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Nullable FK — null means institution-wide, otherwise department-specific
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }
}
