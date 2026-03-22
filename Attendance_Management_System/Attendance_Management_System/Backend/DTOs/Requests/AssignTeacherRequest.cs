using System.ComponentModel.DataAnnotations;

namespace Attendance_Management_System.Backend.DTOs.Requests;

// Request body for assigning a teacher to a section
public class AssignTeacherRequest
{
    // ID of the teacher to assign to the section
    [Required]
    public int TeacherId { get; set; }
}