using System.ComponentModel.DataAnnotations;

namespace Attendance_Management_System.Backend.DTOs.Requests;

public class CreateSectionRequest
{
    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Year Level is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Year Level must be at least 1")]
    public int YearLevel { get; set; }

    [Required(ErrorMessage = "Academic period is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid academic period")]
    public int AcademicYearId { get; set; }

    [Required(ErrorMessage = "Course is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid course")]
    public int CourseId { get; set; }

    [Required(ErrorMessage = "Subject is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid subject")]
    public int SubjectId { get; set; }

    [Required(ErrorMessage = "Classroom is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid classroom")]
    public int ClassroomId { get; set; }
}
