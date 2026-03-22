using System.ComponentModel.DataAnnotations.Schema;

namespace Attendance_Management_System.Backend.Entities;

// Records attendance for a student in a specific schedule and date
public class Attendance : EntityBase
{
    // The class schedule this attendance record belongs to
    public int ScheduleId { get; set; }

    // The student whose attendance is being recorded
    public int StudentId { get; set; }

    // Academic year for reporting purposes
    public int AcademicYearId { get; set; }

    // Section for quick filtering and reporting
    public int SectionId { get; set; }

    // The date of attendance
    public DateOnly Date { get; set; }

    // Time the student checked in (null if absent)
    public TimeOnly? TimeIn { get; set; }

    // Time the student checked out (null if still in class or absent)
    public TimeOnly? TimeOut { get; set; }

    // Optional notes about the attendance (e.g., "Late - traffic")
    public string? Remarks { get; set; }

    // Timestamp when the attendance was recorded
    public DateTimeOffset MarkedAt { get; set; } = DateTimeOffset.UtcNow;

    // User ID of the teacher who marked the attendance
    public int MarkedBy { get; set; }

    // Navigation property to the schedule
    [ForeignKey(nameof(ScheduleId))]
    public Schedule? Schedule { get; set; }

    // Navigation property to the student
    [ForeignKey(nameof(StudentId))]
    public Student? Student { get; set; }

    // Navigation property to the academic year
    [ForeignKey(nameof(AcademicYearId))]
    public AcademicYear? AcademicYear { get; set; }

    // Navigation property to the section
    [ForeignKey(nameof(SectionId))]
    public Section? Section { get; set; }

    // Navigation property to the user who marked attendance
    [ForeignKey(nameof(MarkedBy))]
    public User? Marker { get; set; }
}