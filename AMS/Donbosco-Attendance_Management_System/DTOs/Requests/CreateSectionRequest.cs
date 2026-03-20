using System.ComponentModel.DataAnnotations;

namespace Donbosco_Attendance_Management_System.DTOs.Requests;

// request model for creating a new section
public class CreateSectionRequest
{
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(255, ErrorMessage = "Name cannot exceed 255 characters")]
    public string Name { get; set; } = string.Empty;
}