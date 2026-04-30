using System.ComponentModel.DataAnnotations.Schema;

namespace Attendance_Management_System.Backend.Entities;

// Represents a subject or course unit (e.g., "Data Structures", "Database Systems")
public class Subject : EntityBase
{
    // Full name of the subject
    public string Name { get; set; } = string.Empty;

    // Short code identifier (e.g., "CS201", "DB101")
    public string Code { get; set; } = string.Empty;

    // The course/program this subject belongs to
    public int CourseId { get; set; }

    // Navigation property to the course
    [ForeignKey(nameof(CourseId))]
    public Course? Course { get; set; }

    // Number of credit units for this subject
    public int Units { get; set; }
}