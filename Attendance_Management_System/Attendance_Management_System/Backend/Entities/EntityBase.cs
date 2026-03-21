namespace Attendance_Management_System.Backend.Entities;

public abstract class EntityBase
{
    public int Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}