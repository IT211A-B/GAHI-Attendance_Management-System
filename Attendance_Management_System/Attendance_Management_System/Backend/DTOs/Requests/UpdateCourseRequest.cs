using Attendance_Management_System.Backend.Enums;

namespace Attendance_Management_System.Backend.DTOs.Requests;

public class UpdateCourseRequest
{
    public string? Name { get; set; }

    public string? Code { get; set; }

    public EducationLevel? EducationLevel { get; set; }

    public string? Description { get; set; }
}