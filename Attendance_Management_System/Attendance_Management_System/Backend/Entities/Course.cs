namespace Attendance_Management_System.Backend.Entities;

// Represents an academic course or program (e.g., "Bachelor of Science in Computer Science")
public class Course : EntityBase
{
    // Full name of the course
    public string Name { get; set; } = string.Empty;

    // Short code identifier (e.g., "BSCS", "BSIT")
    public string Code { get; set; } = string.Empty;

    // Optional description of the course program
    public string? Description { get; set; }
}