namespace Attendance_Management_System.Backend.DTOs.Responses;

public class TeacherDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string Department { get; set; } = string.Empty;
    public string? Specialization { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}