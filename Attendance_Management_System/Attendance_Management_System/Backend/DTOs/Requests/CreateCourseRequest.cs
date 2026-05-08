using System.ComponentModel.DataAnnotations;
using Attendance_Management_System.Backend.Enums;

namespace Attendance_Management_System.Backend.DTOs.Requests;

public class CreateCourseRequest
{
    [Required(ErrorMessage = "Name is required")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Code is required")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Education level is required")]
    [EnumDataType(typeof(EducationLevel), ErrorMessage = "Please select a valid education level")]
    public EducationLevel EducationLevel { get; set; }

    public string? Description { get; set; }
}