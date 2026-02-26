using System.ComponentModel.DataAnnotations;

namespace SystemManagementSystem.DTOs.BusinessRules;

public class CreateBusinessRuleRequest
{
    [Required, MaxLength(100)]
    public string RuleKey { get; set; } = string.Empty;

    [Required, MaxLength(500)]
    public string RuleValue { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public Guid? DepartmentId { get; set; }
}

public class UpdateBusinessRuleRequest
{
    [MaxLength(500)]
    public string? RuleValue { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }
}

public class BusinessRuleResponse
{
    public Guid Id { get; set; }
    public string RuleKey { get; set; } = string.Empty;
    public string RuleValue { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
