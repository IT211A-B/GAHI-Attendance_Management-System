namespace Attendance_Management_System.Backend.DTOs.Responses;

public class EnrollmentListDto
{
    public int TotalCount { get; set; }
    public int PendingCount { get; set; }
    public int ApprovedCount { get; set; }
    public int RejectedCount { get; set; }
    public List<EnrollmentDto> Enrollments { get; set; } = new();
}