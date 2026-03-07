using SystemManagementSystem.Models.Enums;

namespace SystemManagementSystem.Models.Entities;

/// <summary>
/// A single attendance scan record. One row per QR scan at a gate terminal.
/// Uses polymorphic FK: either StudentId or StaffId is set (based on PersonType).
/// </summary>
public class AttendanceLog : BaseEntity
{
    public PersonType PersonType { get; set; }

    // Polymorphic FK — one of these is set depending on PersonType
    public Guid? StudentId { get; set; }
    public Student? Student { get; set; }

    public Guid? StaffId { get; set; }
    public Staff? Staff { get; set; }

    // Gate terminal where the scan occurred
    public Guid GateTerminalId { get; set; }
    public GateTerminal GateTerminal { get; set; } = null!;

    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
    public ScanType ScanType { get; set; }
    public AttendanceStatus Status { get; set; }
    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Verified;

    /// <summary>
    /// The raw string data received from the QR scanner for audit purposes.
    /// </summary>
    public string? RawScanData { get; set; }

    /// <summary>
    /// Optional notes, e.g., reason for manual override.
    /// </summary>
    public string? Remarks { get; set; }
}
