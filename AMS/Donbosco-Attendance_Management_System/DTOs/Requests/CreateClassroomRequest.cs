using System.ComponentModel.DataAnnotations;

namespace Donbosco_Attendance_Management_System.DTOs.Requests;

// request model for creating a new classroom
public class CreateClassroomRequest
{
    [Required(ErrorMessage = "Name is required")]
    [MaxLength(255, ErrorMessage = "Name cannot exceed 255 characters")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Room number is required")]
    [MaxLength(50, ErrorMessage = "Room number cannot exceed 50 characters")]
    public string RoomNumber { get; set; } = string.Empty;
}