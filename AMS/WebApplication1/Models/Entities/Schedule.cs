using WebApplication1.Models;

namespace WebApplication1.Models.Entities;

public class Schedule
{
    public int Id { get; set; }
    public int SubjectId { get; set; }
    public int RoomId { get; set; }
    public DayOfWeekEnum DayOfWeek { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public Subject Subject { get; set; } = null!;
    public Room Room { get; set; } = null!;
    public ICollection<ClassAttendance> Attendances { get; set; } = new List<ClassAttendance>();
}
