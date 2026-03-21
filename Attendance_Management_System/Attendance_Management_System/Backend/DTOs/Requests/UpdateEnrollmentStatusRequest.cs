using System.ComponentModel.DataAnnotations;

namespace Attendance_Management_System.Backend.DTOs.Requests;

public class UpdateEnrollmentStatusRequest
{
    [Required]
    public string Status { get; set; } = string.Empty;  // "approved" or "rejected"

    public string? RejectionReason { get; set; }  // Required if rejected
}