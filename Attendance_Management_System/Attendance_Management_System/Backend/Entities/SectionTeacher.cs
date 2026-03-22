using System.ComponentModel.DataAnnotations.Schema;

namespace Attendance_Management_System.Backend.Entities;

// Bridge table for many-to-many relationship between Section and Teacher
// Allows multiple teachers to be assigned to a single section
public class SectionTeacher
{
    // The section being taught
    public int SectionId { get; set; }

    // The teacher assigned to the section
    public int TeacherId { get; set; }

    // Timestamp when the teacher was assigned to this section
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;

    // Navigation property to the section
    [ForeignKey(nameof(SectionId))]
    public Section? Section { get; set; }

    // Navigation property to the teacher
    [ForeignKey(nameof(TeacherId))]
    public Teacher? Teacher { get; set; }
}