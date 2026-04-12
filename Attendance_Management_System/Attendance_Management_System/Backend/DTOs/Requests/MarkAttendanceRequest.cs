using System.ComponentModel.DataAnnotations;

namespace Attendance_Management_System.Backend.DTOs.Requests;

// Request DTO for marking a single student's attendance
public class MarkAttendanceRequest
{
    // The schedule (class session) identifier
    [Required]
    public int ScheduleId { get; set; }

    // The student identifier
    [Required]
    public int StudentId { get; set; }

    // The section (class) identifier
    [Required]
    public int SectionId { get; set; }

    // The date of attendance
    [Required]
    public DateOnly Date { get; set; }

    // Time the student checked in - null means student is absent
    public TimeOnly? TimeIn { get; set; }

    // Optional notes about the attendance
    public string? Remarks { get; set; }
}
