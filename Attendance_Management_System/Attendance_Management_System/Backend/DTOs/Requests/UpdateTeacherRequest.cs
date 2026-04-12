namespace Attendance_Management_System.Backend.DTOs.Requests;

public class UpdateTeacherRequest
{
    public string? EmployeeNumber { get; set; }

    public string? FirstName { get; set; }

    public string? LastName { get; set; }

    public string? MiddleName { get; set; }

    public string? Department { get; set; }

    public string? Specialization { get; set; }
}