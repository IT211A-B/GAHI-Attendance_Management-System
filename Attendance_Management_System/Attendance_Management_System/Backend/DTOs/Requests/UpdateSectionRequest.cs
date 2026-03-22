namespace Attendance_Management_System.Backend.DTOs.Requests;

public class UpdateSectionRequest
{
    public string? Name { get; set; }

    public int? AcademicYearId { get; set; }

    public int? CourseId { get; set; }

    public int? SubjectId { get; set; }

    public int? ClassroomId { get; set; }
}