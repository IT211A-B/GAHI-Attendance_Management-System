namespace Attendance_Management_System.Backend.DTOs.Requests;

public class UpdateAcademicYearRequest
{
    public string? YearLabel { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }
}