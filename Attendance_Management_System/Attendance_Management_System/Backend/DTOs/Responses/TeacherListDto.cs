namespace Attendance_Management_System.Backend.DTOs.Responses;

// Enhanced teacher list item with assigned sections
public class TeacherListDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string EmployeeNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string Department { get; set; } = string.Empty;
    public string? Specialization { get; set; }
    public bool IsActive { get; set; }
    public List<SectionSummaryDto> Sections { get; set; } = new();
}

// Summary of a section for inclusion in teacher lists
public class SectionSummaryDto
{
    public int SectionId { get; set; }
    public string SectionName { get; set; } = string.Empty;
}