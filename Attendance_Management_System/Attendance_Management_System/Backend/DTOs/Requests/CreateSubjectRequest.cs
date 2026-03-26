using System.ComponentModel.DataAnnotations;

namespace Attendance_Management_System.Backend.DTOs.Requests;

public class CreateSubjectRequest
{
    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Code is required")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Course ID is required")]
    public int CourseId { get; set; }

    [Required(ErrorMessage = "Units is required")]
    [Range(1, int.MaxValue, ErrorMessage = "Units must be at least 1")]
    public int Units { get; set; }
}