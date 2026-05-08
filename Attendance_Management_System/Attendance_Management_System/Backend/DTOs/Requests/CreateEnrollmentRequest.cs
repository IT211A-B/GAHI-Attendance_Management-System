using System.ComponentModel.DataAnnotations;

namespace Attendance_Management_System.Backend.DTOs.Requests;

// Request DTO for student self-enrollment
public class CreateEnrollmentRequest
{
    [Required(ErrorMessage = "Course ID is required")]
    public int CourseId { get; set; }

    [Required(ErrorMessage = "Year level is required")]
    [Range(1, 12, ErrorMessage = "Year level must be between 1 and 12")]
    public int YearLevel { get; set; }

    [Required(ErrorMessage = "Academic Year ID is required")]
    public int AcademicYearId { get; set; }
}