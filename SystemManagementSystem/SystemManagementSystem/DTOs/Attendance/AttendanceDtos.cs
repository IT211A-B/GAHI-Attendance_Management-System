using System.ComponentModel.DataAnnotations;

namespace SystemManagementSystem.DTOs.Attendance;

public class ScanRequest
{
    [Required]
    public Guid GateTerminalId { get; set; }

    [Required]
    public string RawScanData { get; set; } = string.Empty;

    public string? Remarks { get; set; }
}

public class ScanResponse
{
    public Guid AttendanceLogId { get; set; }
    public string PersonName { get; set; } = string.Empty;
    public string PersonType { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;
    public string ScanType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string VerificationStatus { get; set; } = string.Empty;
    public DateTime ScannedAt { get; set; }
    public string TerminalName { get; set; } = string.Empty;
}

public class AttendanceLogResponse
{
    public Guid Id { get; set; }
    public string PersonType { get; set; } = string.Empty;
    public Guid? StudentId { get; set; }
    public string? StudentName { get; set; }
    public string? StudentIdNumber { get; set; }
    public Guid? StaffId { get; set; }
    public string? StaffName { get; set; }
    public string? EmployeeIdNumber { get; set; }
    public Guid GateTerminalId { get; set; }
    public string GateTerminalName { get; set; } = string.Empty;
    public DateTime ScannedAt { get; set; }
    public string ScanType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string VerificationStatus { get; set; } = string.Empty;
    public string? RawScanData { get; set; }
    public string? Remarks { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AttendanceFilterRequest
{
    public DateTime? Date { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public Guid? SectionId { get; set; }
    public Guid? DepartmentId { get; set; }
    public string? Status { get; set; }
    public string? PersonType { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
