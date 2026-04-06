using System.ComponentModel.DataAnnotations.Schema;

namespace Attendance_Management_System.Backend.Entities;

// Represents a class schedule time slot for a section and subject
public class Schedule : EntityBase
{
    // The section that has this schedule
    public int SectionId { get; set; }

    // Teacher who owns this schedule slot
    public int? TeacherId { get; set; }

    // The subject being taught in this time slot
    public int SubjectId { get; set; }

    // Day of week: 0=Sunday, 1=Monday, ..., 6=Saturday
    public int DayOfWeek { get; set; }

    // Start time of the class
    public TimeOnly StartTime { get; set; }

    // End time of the class
    public TimeOnly EndTime { get; set; }

    // Date when this schedule becomes effective
    public DateOnly EffectiveFrom { get; set; }

    // Optional end date for schedule changes (null if currently active)
    public DateOnly? EffectiveTo { get; set; }

    // Navigation property to the section
    [ForeignKey(nameof(SectionId))]
    public Section? Section { get; set; }

    // Navigation property to the owner teacher
    [ForeignKey(nameof(TeacherId))]
    public Teacher? Teacher { get; set; }

    // Navigation property to the subject
    [ForeignKey(nameof(SubjectId))]
    public Subject? Subject { get; set; }
}