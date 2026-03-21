namespace Attendance_Management_System.Backend.Entities;

public class AcademicYear : EntityBase
{
    public string YearLabel { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsActive { get; set; } = false;
}