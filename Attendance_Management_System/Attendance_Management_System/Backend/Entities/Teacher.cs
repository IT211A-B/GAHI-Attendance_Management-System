using System.ComponentModel.DataAnnotations.Schema;

namespace Attendance_Management_System.Backend.Entities;

public class Teacher : EntityBase
{
    public int UserId { get; set; }
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string Department { get; set; } = string.Empty;
    public string? Specialization { get; set; }

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }
}