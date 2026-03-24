using System.ComponentModel.DataAnnotations;

namespace Attendance_Management_System.Backend.DTOs.Requests;

public class UpdateSectionRequest
{
    public string? Name { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Year Level must be at least 1")]
    public int? YearLevel { get; set; }

    public int? AcademicYearId { get; set; }

    public int? CourseId { get; set; }

    public int? SubjectId { get; set; }

    public int? ClassroomId { get; set; }
}
