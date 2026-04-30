using System.ComponentModel.DataAnnotations;

namespace Attendance_Management_System.Backend.DTOs.Requests;

// Request DTO for creating a teacher QR attendance session.
public class CreateAttendanceQrSessionRequest
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Section is required.")]
    public int SectionId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Subject is required.")]
    public int SubjectId { get; set; }

    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Period is required.")]
    public int ScheduleId { get; set; }
}
