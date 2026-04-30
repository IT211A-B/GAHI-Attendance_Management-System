using System.ComponentModel.DataAnnotations.Schema;

namespace Attendance_Management_System.Backend.Entities;

// Represents a student in the system with academic and personal information
public class Student : EntityBase
{
    // Foreign key to the User table for authentication
    public int UserId { get; set; }

    // The course/program the student is enrolled in
    public int CourseId { get; set; }

    // Optional section assignment - null until enrollment is approved
    public int? SectionId { get; set; }

    // Unique student identifier assigned by the institution
    public string StudentNumber { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }

    // Student's date of birth
    public DateOnly Birthdate { get; set; }

    // Gender: "M" for male, "F" for female
    public string Gender { get; set; } = "M";

    public string Address { get; set; } = string.Empty;

    // Parent or guardian information for emergency contact
    public string GuardianName { get; set; } = string.Empty;
    public string GuardianContact { get; set; } = string.Empty;

    // Current year level in the academic program (1-4 typically)
    public int YearLevel { get; set; }

    // Soft delete flag - inactive students cannot access the system
    public bool IsActive { get; set; } = true;

    // Navigation property to the associated User account
    [ForeignKey(nameof(UserId))]
    public User? User { get; set; }

    // Navigation property to the student's course
    [ForeignKey(nameof(CourseId))]
    public Course? Course { get; set; }

    // Navigation property to the student's assigned section
    [ForeignKey(nameof(SectionId))]
    public Section? Section { get; set; }
}