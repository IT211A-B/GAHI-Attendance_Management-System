using System.ComponentModel.DataAnnotations;

namespace Donbosco_Attendance_Management_System.DTOs.Requests;

// request model for updating a classroom
public class UpdateClassroomRequest
{
    [MaxLength(255, ErrorMessage = "Name cannot exceed 255 characters")]
    public string? Name { get; set; }

    [MaxLength(50, ErrorMessage = "Room number cannot exceed 50 characters")]
    public string? RoomNumber { get; set; }
}