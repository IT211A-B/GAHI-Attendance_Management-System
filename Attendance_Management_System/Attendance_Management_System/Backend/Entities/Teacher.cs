using System.ComponentModel.DataAnnotations.Schema;

namespace Attendance_Management_System.Backend.Entities;

// Represents a teacher entity with personal and professional information
public class Teacher : EntityBase
{
    // Foreign key to the User table (one-to-one relationship)
    public int UserId { get; set; }

    // Unique employee identifier assigned by the institution
    public string EmployeeNumber { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }

    // Academic department the teacher belongs to
    public string Department { get; set; } = string.Empty;

    // Optional field for teacher's area of expertise
    public string? Specialization { get; set; }

    // Soft delete flag - inactive teachers cannot be assigned to new sections
    public bool IsActive { get; set; } = true;

    // Navigation property to the associated User account
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    // Collection of section assignments (many-to-many relationship via bridge table)
    public ICollection<SectionTeacher> SectionTeachers { get; set; } = new List<SectionTeacher>();
}
