using SystemManagementSystem.Models.Enums;

namespace SystemManagementSystem.Models.Entities;

/// <summary>
/// A staff/faculty master record.
/// QrCodeData holds the value encoded in the physical QR code on the staff's ID card.
/// </summary>
public class Staff : BaseEntity
{
    public string EmployeeIdNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? ContactNumber { get; set; }

    /// <summary>
    /// The data encoded in the QR code on the staff's physical ID card.
    /// Defaults to EmployeeIdNumber but can be regenerated/revoked independently.
    /// </summary>
    public string? QrCodeData { get; set; }

    public StaffType StaffType { get; set; } = StaffType.Teaching;

    // Foreign Keys
    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    // Navigation
    public ICollection<AttendanceLog> AttendanceLogs { get; set; } = new List<AttendanceLog>();
}
