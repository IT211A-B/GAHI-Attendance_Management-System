namespace Attendance_Management_System.Backend.Entities;

public class Classroom : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}