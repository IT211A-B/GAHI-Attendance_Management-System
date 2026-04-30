using System.ComponentModel.DataAnnotations;

namespace Attendance_Management_System.Backend.DTOs.Requests;

public class UpdateSubjectRequest
{
    public string? Name { get; set; }

    public string? Code { get; set; }

    public int? CourseId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Units must be at least 1")]
    public int? Units { get; set; }
}