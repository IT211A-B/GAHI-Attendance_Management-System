using System.ComponentModel.DataAnnotations;

namespace Attendance_Management_System.Backend.DTOs.Requests;

// Request DTO for student QR attendance submission.
public class SubmitAttendanceQrCheckinRequest
{
    [Required]
    [StringLength(4096, MinimumLength = 20)]
    public string Token { get; set; } = string.Empty;
}
