using Attendance_Management_System.Backend.ValueObjects;

namespace Attendance_Management_System.Backend.DTOs.Responses;

// Response DTO for enrollment operations with warnings
public class EnrollmentResultDto
{
    // Indicates if the enrollment was successful
    public bool Success { get; set; }

    // The enrollment details if successful
    public EnrollmentDto? Enrollment { get; set; }

    // List of warnings generated during enrollment (e.g., capacity warnings)
    public List<EnrollmentWarning> Warnings { get; set; } = new();

    // General message about the enrollment result
    public string? Message { get; set; }
}