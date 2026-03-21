using System.ComponentModel.DataAnnotations.Schema;

namespace Attendance_Management_System.Backend.Entities;

public class Student : EntityBase
{
    public int UserId { get; set; }
    public int CourseId { get; set; }
    public int? SectionId { get; set; }
    public string StudentNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public DateOnly Birthdate { get; set; }
    public string Gender { get; set; } = "M";
    public string Address { get; set; } = string.Empty;
    public string GuardianName { get; set; } = string.Empty;
    public string GuardianContact { get; set; } = string.Empty;
    public int YearLevel { get; set; }
    public bool IsActive { get; set; } = true;

    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    [ForeignKey(nameof(CourseId))]
    public Course? Course { get; set; }

    [ForeignKey(nameof(SectionId))]
    public Section? Section { get; set; }
}