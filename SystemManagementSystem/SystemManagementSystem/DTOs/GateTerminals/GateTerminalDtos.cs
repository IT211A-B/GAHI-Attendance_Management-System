using System.ComponentModel.DataAnnotations;
using SystemManagementSystem.Models.Enums;

namespace SystemManagementSystem.DTOs.GateTerminals;

public class CreateGateTerminalRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string Location { get; set; } = string.Empty;

    [Required]
    public TerminalType TerminalType { get; set; }
}

public class UpdateGateTerminalRequest
{
    [MaxLength(100)]
    public string? Name { get; set; }

    [MaxLength(200)]
    public string? Location { get; set; }

    public TerminalType? TerminalType { get; set; }
    public bool? IsActive { get; set; }
}

public class GateTerminalResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string TerminalType { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
