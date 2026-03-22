using System.ComponentModel.DataAnnotations;

namespace Attendance_Management_System.Backend.DTOs.Requests;

public class CreateSectionRequest
{
    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Academic Year ID is required")]
    public int AcademicYearId { get; set; }

    [Required(ErrorMessage = "Course ID is required")]
    public int CourseId { get; set; }

    [Required(ErrorMessage = "Subject ID is required")]
    public int SubjectId { get; set; }

    [Required(ErrorMessage = "Classroom ID is required")]
    public int ClassroomId { get; set; }
}