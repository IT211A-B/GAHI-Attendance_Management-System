namespace Attendance_Management_System.Backend.Entities;

// Represents a physical classroom or venue where classes are held
public class Classroom : EntityBase
{
    // Room name or identifier (e.g., "Room 101", "Computer Lab A")
    public string Name { get; set; } = string.Empty;

    // Optional description with additional details (e.g., capacity, equipment)
    public string? Description { get; set; }
}