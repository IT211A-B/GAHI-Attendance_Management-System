using WebApplication1.Models;

namespace WebApplication1.Models.Entities;

public class ClassAttendance
{
    public int Id { get; set; }
    public int ScheduleId { get; set; }
    public int StudentId { get; set; }
    public DateTime Date { get; set; }
    public AttendanceStatus Status { get; set; }
    public string Remarks { get; set; } = string.Empty;
    public string RecordedBy { get; set; } = string.Empty;

    public Schedule Schedule { get; set; } = null!;
    public Student Student { get; set; } = null!;
}
