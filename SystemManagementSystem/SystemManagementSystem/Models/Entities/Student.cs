using SystemManagementSystem.Models.Enums;

namespace SystemManagementSystem.Models.Entities;

/// <summary>
/// A student master record. Imported from CSV via the Registrar's masterlist.
/// QrCodeData holds the value encoded in the physical QR code on the student's ID card.
/// </summary>
public class Student : BaseEntity
{
    public string StudentIdNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? ContactNumber { get; set; }

    /// <summary>
    /// The data encoded in the QR code on the student's physical ID card.
    /// Defaults to StudentIdNumber but can be regenerated/revoked independently.
    /// </summary>
    public string? QrCodeData { get; set; }

    public EnrollmentStatus EnrollmentStatus { get; set; } = EnrollmentStatus.Active;

    // Foreign Keys
    public Guid SectionId { get; set; }
    public Section Section { get; set; } = null!;

    // Navigation
    public ICollection<AttendanceLog> AttendanceLogs { get; set; } = new List<AttendanceLog>();
}
