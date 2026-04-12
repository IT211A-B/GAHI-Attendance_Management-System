using System.ComponentModel.DataAnnotations;

namespace Attendance_Management_System.Backend.DTOs.Requests;

// Request DTO for admin to reassign a student to a different section
public class ReassignSectionRequest
{
    [Required(ErrorMessage = "New Section ID is required")]
    public int NewSectionId { get; set; }

    // Optional reason for the reassignment
    public string? Reason { get; set; }
}