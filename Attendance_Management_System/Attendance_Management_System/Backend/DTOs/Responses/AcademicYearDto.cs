namespace Attendance_Management_System.Backend.DTOs.Responses;

public class AcademicYearDto
{
    public int Id { get; set; }
    public string YearLabel { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}