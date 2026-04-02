namespace Attendance_Management_System.Backend.DTOs.Responses;

// Full profile response DTO for students - includes sensitive information
// Used for self-view (student viewing own profile) and admin access
public class StudentProfileDto
{
    // Student entity ID
    public int Id { get; set; }

    // Associated user account ID
    public int UserId { get; set; }

    // Unique student identifier assigned by the institution
    public string StudentNumber { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }

    // Student's date of birth (sensitive)
    public DateOnly Birthdate { get; set; }

    // Gender: "M" for male, "F" for female, or "Other"
    public string Gender { get; set; } = string.Empty;

    // Student's address (sensitive)
    public string Address { get; set; } = string.Empty;

    // Parent or guardian name (sensitive)
    public string GuardianName { get; set; } = string.Empty;

    // Parent or guardian contact number (sensitive)
    public string GuardianContact { get; set; } = string.Empty;

    // Current year level in the academic program (1-4)
    public int YearLevel { get; set; }

    // Course enrollment details
    public int CourseId { get; set; }
    public string? CourseName { get; set; }
    public string? CourseCode { get; set; }

    // Section assignment (null if not yet enrolled in a section)
    public int? SectionId { get; set; }
    public string? SectionName { get; set; }

    // Active status
    public bool IsActive { get; set; }
}