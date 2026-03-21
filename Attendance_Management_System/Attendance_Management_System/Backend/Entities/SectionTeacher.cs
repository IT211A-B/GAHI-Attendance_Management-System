using System.ComponentModel.DataAnnotations.Schema;

namespace Attendance_Management_System.Backend.Entities;

public class SectionTeacher
{
    public int SectionId { get; set; }
    public int TeacherId { get; set; }
    public DateTimeOffset AssignedAt { get; set; } = DateTimeOffset.UtcNow;

    [ForeignKey(nameof(SectionId))]
    public Section? Section { get; set; }

    [ForeignKey(nameof(TeacherId))]
    public Teacher? Teacher { get; set; }
}