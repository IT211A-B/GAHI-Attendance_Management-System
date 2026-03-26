using System.ComponentModel.DataAnnotations;

namespace Attendance_Management_System.Backend.DTOs.Requests;

public class BulkAttendanceRequest
{
    [Required]
    public int ScheduleId { get; set; }

    [Required]
    public int SectionId { get; set; }

    [Required]
    public DateOnly Date { get; set; }

    [Required]
    public List<SingleAttendanceEntry> Entries { get; set; } = new();
}

public class SingleAttendanceEntry
{
    [Required]
    public int StudentId { get; set; }

    public TimeOnly? TimeIn { get; set; }  // Null = absent

    public string? Remarks { get; set; }
}