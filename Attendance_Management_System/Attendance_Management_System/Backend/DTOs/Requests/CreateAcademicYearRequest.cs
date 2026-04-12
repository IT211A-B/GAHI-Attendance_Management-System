using System.ComponentModel.DataAnnotations;

namespace Attendance_Management_System.Backend.DTOs.Requests;

public class CreateAcademicYearRequest
{
    [Required(ErrorMessage = "Year label is required")]
    public string YearLabel { get; set; } = string.Empty;

    [Required(ErrorMessage = "Start date is required")]
    public DateOnly StartDate { get; set; }

    [Required(ErrorMessage = "End date is required")]
    public DateOnly EndDate { get; set; }
}