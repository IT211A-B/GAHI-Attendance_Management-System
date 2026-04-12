namespace Attendance_Management_System.Backend.DTOs.Responses;

// Response shape for teachers assigned to a section
public class SectionTeacherDto
{
    public int TeacherId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string Department { get; set; } = string.Empty;
    public DateTimeOffset AssignedAt { get; set; }
}