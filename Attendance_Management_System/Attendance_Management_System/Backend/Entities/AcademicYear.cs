namespace Attendance_Management_System.Backend.Entities;

// Represents an academic year/semester period in the institution
public class AcademicYear : EntityBase
{
    // Display label for the academic year (e.g., "2024-2025 First Semester")
    public string YearLabel { get; set; } = string.Empty;

    // Start date of the academic year
    public DateOnly StartDate { get; set; }

    // End date of the academic year
    public DateOnly EndDate { get; set; }

    // Only one academic year can be active at a time
    public bool IsActive { get; set; } = false;
}