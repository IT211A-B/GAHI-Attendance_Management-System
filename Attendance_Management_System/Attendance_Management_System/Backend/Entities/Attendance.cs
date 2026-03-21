using System.ComponentModel.DataAnnotations.Schema;

namespace Attendance_Management_System.Backend.Entities;

public class Attendance : EntityBase
{
    public int ScheduleId { get; set; }
    public int StudentId { get; set; }
    public int AcademicYearId { get; set; }
    public int SectionId { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly? TimeIn { get; set; }
    public TimeOnly? TimeOut { get; set; }
    public string? Remarks { get; set; }
    public DateTimeOffset MarkedAt { get; set; } = DateTimeOffset.UtcNow;
    public int MarkedBy { get; set; }

    [ForeignKey(nameof(ScheduleId))]
    public Schedule? Schedule { get; set; }

    [ForeignKey(nameof(StudentId))]
    public Student? Student { get; set; }

    [ForeignKey(nameof(AcademicYearId))]
    public AcademicYear? AcademicYear { get; set; }

    [ForeignKey(nameof(SectionId))]
    public Section? Section { get; set; }

    [ForeignKey(nameof(MarkedBy))]
    public User? Marker { get; set; }
}