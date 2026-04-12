namespace Attendance_Management_System.Backend.DTOs.Responses;

// Limited profile response DTO for students - excludes sensitive information
// Used for teacher access to students in their sections
public class StudentBasicProfileDto
{
    // Student entity ID
    public int Id { get; set; }

    // Unique student identifier assigned by the institution
    public string StudentNumber { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }

    // Current year level in the academic program (1-4)
    public int YearLevel { get; set; }

    // Course enrollment details
    public int CourseId { get; set; }
    public string? CourseName { get; set; }
    public string? CourseCode { get; set; }

    // Section assignment (null if not yet enrolled in a section)
    public int? SectionId { get; set; }
    public string? SectionName { get; set; }
}