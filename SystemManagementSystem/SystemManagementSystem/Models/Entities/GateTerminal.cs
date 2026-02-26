using SystemManagementSystem.Models.Enums;

namespace SystemManagementSystem.Models.Entities;

/// <summary>
/// A physical scanning terminal installed at a gate.
/// </summary>
public class GateTerminal : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public TerminalType TerminalType { get; set; } = TerminalType.QRScanner;
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<AttendanceLog> AttendanceLogs { get; set; } = new List<AttendanceLog>();
}
